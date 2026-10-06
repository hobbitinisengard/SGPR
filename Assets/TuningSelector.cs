using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using RVP;

public class TuningSelector : Sfxable
{
	// Keep the existing Inspector bindings and animation controls.
	public RectTransform[] bars;
	public Text partDescrText;
	public Text partTypeText;
	public Text partNameText;
	public GameObject partImageTemplate;
	public GameObject TraitValuePrefab;
	public RectTransform content;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	public RectTransform traitsContent;
	public BackgroundTiles tuningBackground;
	public GameObject PriceNew;
	public GameObject CashBalance;
	public GameObject TradeIn;
	public bool d_co;

	const float AnimationSpeed = 3f;
	static readonly string[] ImageCategories =
		{ "engines", "batteries", "tyres", "brakes", "shocks", "chassis", "drives", "horns", "boost", "bms", "gears" };
	static readonly string[] CategoryNames =
		{ "Engine", "Battery", "Tyres", "Brakes", "Suspension", "Chassis", "Drive", "Horn", "Stunt Boost", "BMS", "Gears" };

	sealed class PartRow
	{
		public OriginalVehiclePartType type;
		public RectTransform transform;
		public List<OriginalVehiclePartDefinition> parts;
		public readonly List<RectTransform> images = new();
		public int selectedIndex;
		public string defaultPartId;
		public float imageSpacing;
	}

	readonly List<PartRow> rows = new();
	readonly List<GameObject> traitCells = new();
	OriginalVehiclePartCatalog catalog;
	CarConfig carConfig;
	InputAction navigationAction;
	int loadedCarIndex = -1;
	int loadedPartsVersion = -1;
	int selectedRow;
	OriginalVehiclePartType rememberedCategory;
	float rowSpacing;
	float backgroundPosition;
	float[] barWidths;
	Coroutine loadRoutine;
	Coroutine containerCo;
	Coroutine barsAndRadialCo;
	bool loading;

	public OriginalVehiclePartDefinition SelectedPart => rows.Count == 0 ? null :
		rows[selectedRow].parts[rows[selectedRow].selectedIndex];

	protected override void Awake()
	{
		base.Awake();
		barWidths = new float[bars?.Length ?? 0];
		for (int i = 0; i < barWidths.Length; i++)
			barWidths[i] = bars[i] ? bars[i].sizeDelta.x : 0;
	}

	void OnEnable()
	{
		F.I.enterRef.action.performed += ConfirmPurchase;
		UpdatePurchaseUI(); Reload();
	}

	void OnDisable()
	{
		F.I.enterRef.action.performed -= ConfirmPurchase;
		if (navigationAction != null)
			navigationAction.performed -= CalculateTargetToSelect;
		navigationAction = null;
		StopAllCoroutines();
		loadRoutine = containerCo = barsAndRadialCo = null;
		loading = false;
	}

	void Update()
	{
		if (!loading && F.I && (loadedCarIndex != F.I.s_playerCarIdx || (catalog != null && loadedPartsVersion != catalog.UserPartsVersion)))
			Reload();
	}

	public void Reload()
	{
		if (!isActiveAndEnabled)
			return;
		if (loadRoutine != null) StopCoroutine(loadRoutine);
		if (containerCo != null) StopCoroutine(containerCo);
		if (barsAndRadialCo != null) StopCoroutine(barsAndRadialCo);
		containerCo = barsAndRadialCo = null;
		loading = true;
		loadRoutine = StartCoroutine(Load());
	}

	IEnumerator Load()
	{
		while (!F.I || F.I.originalVehiclePartCatalog == null || F.I.cars == null || F.I.cars.Length == 0)
			yield return null;
		loadedCarIndex = Mathf.Clamp(F.I.s_playerCarIdx, 0, F.I.cars.Length - 1);
		while (F.I.cars[loadedCarIndex].config == null)
			yield return null;
		if (navigationAction == null && F.I.move2Ref != null)
		{
			navigationAction = F.I.move2Ref.action;
			navigationAction.performed += CalculateTargetToSelect;
		}
		ResolveViewBindings();
		if (!content || !partImageTemplate || !TraitValuePrefab || !traitsContent)
		{
			Debug.LogError("TuningSelector needs part content, both prefabs and a trait grid.", this);
			loading = false;
			yield break;
		}
		F.I.ReloadUserParts();
		catalog = F.I.originalVehiclePartCatalog;
		loadedPartsVersion = catalog.UserPartsVersion;
		carConfig = F.I.cars[loadedCarIndex].config;
		OriginalVehicleCarSetup allowedSetup = F.I.GetDefaultOriginalVehicleSetup(loadedCarIndex);
		carConfig.EnsureOriginalParts(allowedSetup);
		BuildRows(allowedSetup);
		selectedRow = Mathf.Max(0, rows.FindIndex(row => row.type == rememberedCategory));
		yield return null;
		Canvas.ForceUpdateCanvases();
		RefreshSelectedPart(false);
		loading = false;
		loadRoutine = null;
	}

	void ResolveViewBindings()
	{
		Transform view = transform;
		while (view.parent && view.name != "TuningView") view = view.parent;
		if (!tuningBackground)
		{
			foreach (BackgroundTiles background in view.GetComponentsInChildren<BackgroundTiles>(true))
			{
				if (background.name == "CVBckBrown")
				{
					tuningBackground = background;
					break;
				}
				if (background.TryGetComponent(out Image image) && image.sprite == background.tileBrown)
					tuningBackground = background;
			}
		}
		if (!content)
		{
			ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
			if (scroll) content = scroll.content;
		}
		if (!partImageTemplate) partImageTemplate = Resources.Load<GameObject>("prefabs/partContent");
		if (!TraitValuePrefab) TraitValuePrefab = Resources.Load<GameObject>("prefabs/ElementValue");
		if (!traitsContent)
		{
			GridLayoutGroup grid = view.GetComponentInChildren<GridLayoutGroup>(true);
			if (grid) traitsContent = (RectTransform)grid.transform;
		}
		foreach (Text text in view.GetComponentsInChildren<Text>(true))
		{
			if (!partTypeText && text.name == "Type") partTypeText = text;
			if (!partNameText && text.name == "Name") partNameText = text;
			if (!partDescrText && text.name == "Description") partDescrText = text;
		}
		DisableLocalization(partTypeText ? partTypeText.gameObject : null);
		DisableLocalization(partNameText ? partNameText.gameObject : null);
		DisableLocalization(partDescrText ? partDescrText.gameObject : null);
	}

	void BuildRows(OriginalVehicleCarSetup allowedSetup)
	{
		rows.Clear();
		DisableAutomaticLayout(content);
		ScrollRect scroll = content.GetComponentInParent<ScrollRect>();
		if (scroll)
		{
			scroll.StopMovement();
			// The selector owns centering; ScrollRect's bounds must not clamp it.
			scroll.enabled = false;
		}
		RectTransform viewport = content.parent as RectTransform;
		RectTransform template = partImageTemplate.GetComponent<RectTransform>();
		float imageWidth = template.rect.width * Mathf.Abs(template.localScale.x);
		float imageHeight = template.rect.height * Mathf.Abs(template.localScale.y);
		rowSpacing = Mathf.Max(imageHeight + 24, viewport.rect.height);
		content.anchorMin = content.anchorMax = new Vector2(0.5f, 1);
		content.pivot = new Vector2(0.5f, 1);
		content.anchoredPosition = Vector2.zero;

		foreach (OriginalVehiclePartSlot slot in allowedSetup?.slots ?? System.Array.Empty<OriginalVehiclePartSlot>())
		{
			List<OriginalVehiclePartDefinition> parts = catalog.GetAvailableParts(allowedSetup, slot.type);
			if (!Championships.Active)
			{
				var equipped = catalog.GetSelectedPart(carConfig.originalParts, slot.type);
				parts.RemoveAll(part => part.id != equipped?.id && !F.I.IsOriginalVehiclePartUnlocked(part));
				// A custom car setup can equip a stock part outside its usual catalog range.
				if (equipped != null && !parts.Exists(part => part.id == equipped.id))
				{
					parts.Add(equipped);
					parts.Sort((a, b) => a.IsUserPart != b.IsUserPart ? (a.IsUserPart ? 1 : -1) : a.index.CompareTo(b.index));
				}
			}
			if (parts.Count == 0) continue;
			int rowIndex = rows.Count;
			RectTransform rowTransform = rowIndex < content.childCount
				? content.GetChild(rowIndex) as RectTransform
				: (RectTransform)new GameObject(slot.type.ToString(), typeof(RectTransform)).transform;
			rowTransform.SetParent(content, false);
			rowTransform.gameObject.SetActive(true);
			DisableAutomaticLayout(rowTransform);
			for (int i = rowTransform.childCount - 1; i >= 0; i--)
			{
				GameObject previous = rowTransform.GetChild(i).gameObject;
				previous.SetActive(false);
				Destroy(previous);
			}
			rowTransform.name = slot.type.ToString();
			rowTransform.localScale = Vector3.one;
			rowTransform.anchorMin = rowTransform.anchorMax = new Vector2(0.5f, 1);
			rowTransform.pivot = new Vector2(0.5f, 0.5f);
			rowTransform.sizeDelta = new Vector2(viewport.rect.width, rowSpacing);
			var row = new PartRow { type = slot.type, transform = rowTransform, parts = parts, defaultPartId = slot.defaultPartId, imageSpacing = imageWidth + 24 };
			row.selectedIndex = EquippedIndex(row);
			for (int i = 0; i < parts.Count; i++)
			{
				GameObject icon = Instantiate(partImageTemplate, rowTransform, false);
				icon.name = parts[i].id;
				RectTransform rect = (RectTransform)icon.transform;
				rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = new Vector2(i * (imageWidth + 24), 0);
				Image image = icon.GetComponent<Image>();
				image.sprite = parts[i].IsUserPart ? Resources.Load<Sprite>("catalog/locked") : Resources.Load<Sprite>("catalog/upgrades/frontend_pages_catalog_upgrades_" +
					ImageCategories[(int)slot.type] + "_" + parts[i].index.ToString("D2"));
				image.preserveAspect = true;
				icon.SetActive(true);
				row.images.Add(rect);
			}
			rowTransform.anchoredPosition = new Vector2(-row.images[row.selectedIndex].anchoredPosition.x,
				-viewport.rect.height * 0.5f - rowIndex * rowSpacing);
			rows.Add(row);
		}
		for (int i = rows.Count; i < content.childCount; i++) content.GetChild(i).gameObject.SetActive(false);
		content.sizeDelta = new Vector2(viewport.rect.width,
			viewport.rect.height + Mathf.Max(0, rows.Count - 1) * rowSpacing);
		if (traitCells.Count == 0)
			foreach (Transform child in traitsContent) child.gameObject.SetActive(false);
	}

	static void DisableAutomaticLayout(RectTransform target)
	{
		if (target.TryGetComponent(out LayoutGroup layout)) layout.enabled = false;
		if (target.TryGetComponent(out ContentSizeFitter fitter)) fitter.enabled = false;
	}

	int EquippedIndex(PartRow row)
	{
		string equippedId = carConfig.originalParts?.GetSlot(row.type)?.SelectedPartId ?? row.defaultPartId;
		// Some source setups use index 0 (including horns). Keep the same visual
		// fallback when entering a row and when discarding an unpurchased preview.
		return Mathf.Max(0, row.parts.FindIndex(part => part.id == equippedId));
	}

	void CalculateTargetToSelect(InputAction.CallbackContext ctx)
	{
		if (loading || rows.Count == 0) return;
		Vector2 move = ctx.ReadValue<Vector2>();
		int x = Mathf.RoundToInt(move.x);
		int y = Mathf.RoundToInt(-move.y);
		if (x == 0 && y == 0) return;
		int nextRow = Mathf.Clamp(selectedRow + y, 0, rows.Count - 1);
		PartRow row = rows[nextRow];
		int nextPart = Mathf.Clamp(row.selectedIndex + (y == 0 ? x : 0), 0, row.parts.Count - 1);
		if (nextRow == selectedRow && nextPart == row.selectedIndex) return;
		if (Championships.Active && nextRow != selectedRow)
		{
			PartRow previous = rows[selectedRow];
			previous.selectedIndex = EquippedIndex(previous);
		}
		selectedRow = nextRow;
		row.selectedIndex = nextPart;
		PlaySFX("fe-bitmapscroll");
		RefreshSelectedPart(true);
	}

	void RefreshSelectedPart(bool equip)
	{
		OriginalVehiclePartDefinition part = SelectedPart;
		if (part == null)
		{
			if (partTypeText) partTypeText.text = "";
			if (partNameText) partNameText.text = "";
			if (partDescrText) partDescrText.text = F.I.LocStr("No parts available");
			foreach (GameObject cell in traitCells) cell.SetActive(false);
			return;
		}
		rememberedCategory = part.type;
		if (equip && !Championships.Active)
		{
			EquipPart(part);
			RemoveUnequippedLockedParts(rows[selectedRow]);
		}
		if (partTypeText) partTypeText.text = F.I.LocStr(CategoryNames[(int)part.type]) + ":";
		if (partNameText) partNameText.text = F.I.LocStr(part.IsUserPart ? part.GetName() : "Tuning." + part.id + ".Name");
		if (partDescrText) partDescrText.text = F.I.LocStr(part.IsUserPart ? part.GetDescription() : "Tuning." + part.id + ".Description");
		RefreshTraits(part);
		UpdatePurchaseUI();
		if (containerCo != null) StopCoroutine(containerCo);
		containerCo = StartCoroutine(MoveToPart());
		if (barsAndRadialCo != null) StopCoroutine(barsAndRadialCo);
		barsAndRadialCo = StartCoroutine(SetPerformanceBarsAndRadial());
	}

	void RemoveUnequippedLockedParts(PartRow row)
	{
		string equippedId = carConfig.originalParts.GetSlot(row.type)?.SelectedPartId;
		for (int i = row.parts.Count - 1; i >= 0; i--)
		{
			if (row.parts[i].id == equippedId || F.I.IsOriginalVehiclePartUnlocked(row.parts[i])) continue;
			row.images[i].gameObject.SetActive(false);
			Destroy(row.images[i].gameObject);
			row.images.RemoveAt(i);
			row.parts.RemoveAt(i);
			if (i < row.selectedIndex) row.selectedIndex--;
		}
		for (int i = 0; i < row.images.Count; i++)
			row.images[i].anchoredPosition = new Vector2(i * row.imageSpacing, 0);
	}

	void ConfirmPurchase(InputAction.CallbackContext ctx) { if (Championships.Active && !loading) BuySelectedPart(); }
	public void BuySelectedPart()
	{
		if (!Championships.Active || loading || SelectedPart == null) return;
		var installed = F.I.originalVehiclePartCatalog.GetSelectedPart(carConfig.originalParts, SelectedPart.type);
		if (installed?.id == SelectedPart.id) return;
		if (!Championships.BuyPart(SelectedPart)) { PlaySFX("fe-warning"); return; }
		PlaySFX("fe-dialogconfirm"); UpdatePurchaseUI();
	}
	void UpdatePurchaseUI()
	{
		bool active = Championships.Active;
		if (PriceNew) PriceNew.SetActive(active);
		if (CashBalance) CashBalance.SetActive(active);
		if (TradeIn) TradeIn.SetActive(active);
		if (!active || SelectedPart == null) return;
		var part = SelectedPart;
		var installed = F.I.originalVehiclePartCatalog.GetSelectedPart(F.I.cars[Championships.Current.carIndex].config.originalParts, part.type);
		SetMoneyValue(PriceNew, Championships.PartPrice(part));
		SetMoneyValue(CashBalance, Championships.Current.cash);
		int exchangeBalance = installed?.id == part.id ? 0 : -Championships.PartCost(part);
		SetMoneyValue(TradeIn, exchangeBalance);

	}
	static void SetMoneyValue(GameObject root, int amount)
	{
		if (!root) return;
		// Update only the numeric child, leaving the labels and their localization intact.
		Transform value = root.transform.Find("Price") ?? root.transform.Find("Name");
		if (value) ChampionshipUI.SetText(value.gameObject, $"${amount:N0}");
	}

	void EquipPart(OriginalVehiclePartDefinition part)
	{
		if (carConfig.originalParts == null) return;
		OriginalVehiclePartSlot slot = carConfig.originalParts.GetSlot(part.type);
		if (slot == null || slot.SelectedPartId == part.id ||
			!carConfig.originalParts.TrySelectPart(part)) return;
		carConfig.MarkModified();
		//PlaySFX("fe-dialogconfirm");
		VehicleParent player = RaceManager.I ? RaceManager.I.playerCar : null;
		if (player && player.Owner && player.carNumber == loadedCarIndex)
		{
			player.carConfig.originalParts = carConfig.originalParts.Clone();
			player.carConfig.MarkModified();
			player.carConfig.Apply(player);
		}
	}

	void RefreshTraits(OriginalVehiclePartDefinition part)
	{
		int count = 0;
		if (part.IsUserPart)
		{
			foreach (var entry in part.physicsOverrides)
				SetTraitCell(count++, entry.Key, entry.Value);
			for (int i = count; i < traitCells.Count; i++) traitCells[i].SetActive(false);
			LayoutRebuilder.ForceRebuildLayoutImmediate(traitsContent);
			return;
		}
		for (int i = 0; i < part.parameterNames.Length && i < part.parameters.Length; i++)
		{
			float value = part.parameters[i];
			if (value == -99999 || float.IsNaN(value) || float.IsInfinity(value)) continue;
			string label = PhysicsParameterName(part.parameterNames[i], i);
			if (label == null) continue;
			SetTraitCell(count++, label, value);
		}
		for (int i = count; i < traitCells.Count; i++) traitCells[i].SetActive(false);
		LayoutRebuilder.ForceRebuildLayoutImmediate(traitsContent);
	}

	void SetTraitCell(int index, string label, float value)
	{
		if (index == traitCells.Count)
		{
			GameObject cell = Instantiate(TraitValuePrefab, traitsContent, false);
			DisableLocalization(cell);
			traitCells.Add(cell);
		}
		GameObject trait = traitCells[index];
		trait.SetActive(true);
		trait.transform.Find("Element").GetComponent<Text>().text = label + ":";
		trait.transform.Find("Value").GetComponent<Text>().text = value.ToString("0.#####", CultureInfo.InvariantCulture);
	}
	static void DisableLocalization(GameObject target)
	{
		if (!target) return;
		foreach (LocalizeStringEvent localizer in target.GetComponentsInChildren<LocalizeStringEvent>(true))
			localizer.enabled = false;
	}

	static string PhysicsParameterName(string source, int column)
	{
		return source.Trim() switch
		{
			"Additive Mass" => "additiveMass", "Base Mass" => "mass",
			"Com A" => "comA", "Com B" => "comB", "Com H" => "comHeight",
			"Ride Height" => "rideHeight", "Cm" => column > 40 ? "turboDecay" : "engineDecay",
			"Rpm Idle" => "rpmIdle", "Rpm Limit" => "rpmLimit",
			"Rpm Max" => column > 40 ? "turboMax" : "rpmMax",
			"Max Torque" => "maxTorque", "Torque Env" => "torqueCurveIndex",
			"Drive Mode" => "driveMode", "Final Drive" => "finalDrive", "Gears" => "forwardGears",
			"Shift" => "shiftTime", "Efficiency" => "efficiency", "4WD Split" => "powerSplit",
			"GearCog1" => "gearRatio1", "GearCog2" => "gearRatio2", "GearCog3" => "gearRatio3",
			"GearCog4" => "gearRatio4", "GearCog5" => "gearRatio5", "GearCog6" => "gearRatio6",
			"GearCog7" => "gearRatio7", "GearCog8" => "gearRatio8",
			"Brake Bias" => "brakeBias", "Brake Accel." => "brakeAcceleration",
			"Travel in" => "travelIn", "Damping in" => "dampingIn", "Stiffness in" => "stiffnessIn",
			"Max Travel" => "travelOut", "Max Damp." => "dampingOut", "Max Stiff." => "stiffnessOut",
			"Cs" => "staticFriction", "Ck" => "kineticFriction", "Cd" => "dragCoefficient",
			"Cl" => "liftCoefficient", "Steering angle" => "steeringMax",
			"Sensitivity" => "steeringSensitivity", "Acceleration" => "steeringAcceleration",
			"Max Fuel" => "fuelCapacity", "Fuel Consumpt" => "fuelConsumption", "Refuel Rate" => "refuelRate",
			"Rpm Acc" => "turboAcceleration", "Power Mult" => "turboScale",
			"Max Consump" => "turboConsumption", "Fuel Cut Off" => "turboEnergyThreshold",
			"Launchtime" => "launchTime", "Launch Tolerance" => "launchTolerance", "Launch Speed" => "launchSpeed",
			"Mat Rot Speed X" => "maxPitchSpeed", "Mat Rot Speed Y" => "maxYawSpeed",
			_ => null
		};
	}

	IEnumerator MoveToPart()
	{
		d_co = false;
		PartRow row = rows[selectedRow];
		float initialBackground = backgroundPosition;
		float targetBackground = row.parts.Count == 1 ? 0 :
			2f * row.selectedIndex / (row.parts.Count - 1) - 1;
		Vector2 initialContent = content.anchoredPosition;
		Vector2 targetContent = new Vector2(initialContent.x, selectedRow * rowSpacing);
		// Restore outgoing previews and center the destination in the same animation.
		// Capturing every row also allows a new input to redirect an unfinished movement.
		Vector2[] initialRows = new Vector2[rows.Count];
		Vector2[] targetRows = new Vector2[rows.Count];
		for (int i = 0; i < rows.Count; i++)
		{
			initialRows[i] = rows[i].transform.anchoredPosition;
			targetRows[i] = new Vector2(-rows[i].images[rows[i].selectedIndex].anchoredPosition.x, initialRows[i].y);
		}
		Vector2 initialScroll = new Vector2(scrollx ? scrollx.value : 0, scrolly ? scrolly.value : 1);
		Vector2 initialSize = new Vector2(scrollx ? scrollx.size : 1, scrolly ? scrolly.size : 1);
		float horizontalProgress = row.parts.Count == 1 ? 0 : (float)row.selectedIndex / (row.parts.Count - 1);
		float verticalProgress = rows.Count == 1 ? 0 : (float)selectedRow / (rows.Count - 1);
		Vector2 targetScroll = new Vector2(
			scrollx && scrollx.direction == Scrollbar.Direction.RightToLeft ? 1 - horizontalProgress : horizontalProgress,
			scrolly && scrolly.direction == Scrollbar.Direction.BottomToTop ? 1 - verticalProgress : verticalProgress);
		Vector2 targetSize = new Vector2(1f / row.parts.Count, 1f / rows.Count);
		for (float timer = 0; timer < 1; timer += Time.unscaledDeltaTime * AnimationSpeed)
		{
			float step = F.EasingOutQuint(timer);
			backgroundPosition = Mathf.Lerp(initialBackground, targetBackground, step);
			if (tuningBackground) tuningBackground.SetTuningBlend(backgroundPosition);
			content.anchoredPosition = Vector2.Lerp(initialContent, targetContent, step);
			for (int i = 0; i < rows.Count; i++)
				rows[i].transform.anchoredPosition = Vector2.Lerp(initialRows[i], targetRows[i], step);
			if (scrollx) { scrollx.SetValueWithoutNotify(Mathf.Lerp(initialScroll.x, targetScroll.x, step)); scrollx.size = Mathf.Lerp(initialSize.x, targetSize.x, step); }
			if (scrolly) { scrolly.SetValueWithoutNotify(Mathf.Lerp(initialScroll.y, targetScroll.y, step)); scrolly.size = Mathf.Lerp(initialSize.y, targetSize.y, step); }
			yield return null;
		}
		backgroundPosition = targetBackground;
		if (tuningBackground) tuningBackground.SetTuningBlend(backgroundPosition);
		content.anchoredPosition = targetContent;
		for (int i = 0; i < rows.Count; i++) rows[i].transform.anchoredPosition = targetRows[i];
		if (scrollx) { scrollx.SetValueWithoutNotify(targetScroll.x); scrollx.size = targetSize.x; }
		if (scrolly) { scrolly.SetValueWithoutNotify(targetScroll.y); scrolly.size = targetSize.y; }
		containerCo = null;
		d_co = true;
	}

	IEnumerator SetPerformanceBarsAndRadial()
	{
		if (barWidths.Length == 0) yield break;
		float[] targetBars = carConfig.SGP;
		float[] initialBars = new float[barWidths.Length];
		for (int i = 0; i < initialBars.Length; i++) initialBars[i] = bars[i] ? bars[i].sizeDelta.x : 0;
		for (float timer = 0; timer < 1; timer += Time.unscaledDeltaTime * AnimationSpeed)
		{
			float step = Easing.OutCubic(timer);
			for (int i = 0; i < initialBars.Length && i < targetBars.Length; i++)
				if (bars[i]) bars[i].sizeDelta = new Vector2(Mathf.Lerp(initialBars[i], targetBars[i] * barWidths[i], step), bars[i].sizeDelta.y);
			yield return null;
		}
		barsAndRadialCo = null;
	}
}
