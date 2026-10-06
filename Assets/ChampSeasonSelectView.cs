using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using RVP;

public class ChampSeasonSelectView : MainMenuView
{
	public Text sponsorDescrText;
	public Text startingMoneyText;
	public RectTransform content;
	public Scrollbar scrollx;
	public bool d_co;

	sealed class PartRow
	{
		public OriginalVehiclePartType type;
		public RectTransform transform;
		public List<OriginalVehiclePartDefinition> parts;
		public readonly List<RectTransform> images = new();
		public int selectedIndex;
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

	void OnEnable() => Reload();

	void OnDisable()
	{
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
		loading = false;
		loadRoutine = null;
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
		RectTransform template = null;// sponsorImageTemplate.GetComponent<RectTransform>();
		float imageWidth = template.rect.width * Mathf.Abs(template.localScale.x);
		float imageHeight = template.rect.height * Mathf.Abs(template.localScale.y);
		rowSpacing = Mathf.Max(imageHeight + 24, viewport.rect.height);
		content.anchorMin = content.anchorMax = new Vector2(0.5f, 1);
		content.pivot = new Vector2(0.5f, 1);
		content.anchoredPosition = Vector2.zero;

		foreach (OriginalVehiclePartSlot slot in allowedSetup?.slots ?? System.Array.Empty<OriginalVehiclePartSlot>())
		{
			List<OriginalVehiclePartDefinition> parts = catalog.GetAvailableParts(allowedSetup, slot.type);
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
			var row = new PartRow { type = slot.type, transform = rowTransform, parts = parts };
			string equippedId = carConfig.originalParts?.GetSlot(slot.type)?.SelectedPartId ?? slot.defaultPartId;
			row.selectedIndex = Mathf.Max(0, parts.FindIndex(part => part.id == equippedId));
			for (int i = 0; i < parts.Count; i++)
			{
				GameObject icon = null;// Instantiate(sponsorImageTemplate, rowTransform, false);
				icon.name = parts[i].id;
				RectTransform rect = (RectTransform)icon.transform;
				rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = new Vector2(i * (imageWidth + 24), 0);
				Image image = icon.GetComponent<Image>();
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
		
	}

	static void DisableAutomaticLayout(RectTransform target)
	{
		if (target.TryGetComponent(out LayoutGroup layout)) layout.enabled = false;
		if (target.TryGetComponent(out ContentSizeFitter fitter)) fitter.enabled = false;
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
		selectedRow = nextRow;
		row.selectedIndex = nextPart;
		PlaySFX("fe-bitmapscroll");
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
			"Additive Mass" => "additiveMass",
			"Base Mass" => "mass",
			"Com A" => "comA",
			"Com B" => "comB",
			"Com H" => "comHeight",
			"Ride Height" => "rideHeight",
			"Cm" => column > 40 ? "turboDecay" : "engineDecay",
			"Rpm Idle" => "rpmIdle",
			"Rpm Limit" => "rpmLimit",
			"Rpm Max" => column > 40 ? "turboMax" : "rpmMax",
			"Max Torque" => "maxTorque",
			"Torque Env" => "torqueCurveIndex",
			"Drive Mode" => "driveMode",
			"Final Drive" => "finalDrive",
			"Gears" => "forwardGears",
			"Shift" => "shiftTime",
			"Efficiency" => "efficiency",
			"4WD Split" => "powerSplit",
			"GearCog1" => "gearRatio1",
			"GearCog2" => "gearRatio2",
			"GearCog3" => "gearRatio3",
			"GearCog4" => "gearRatio4",
			"GearCog5" => "gearRatio5",
			"GearCog6" => "gearRatio6",
			"GearCog7" => "gearRatio7",
			"GearCog8" => "gearRatio8",
			"Brake Bias" => "brakeBias",
			"Brake Accel." => "brakeAcceleration",
			"Travel in" => "travelIn",
			"Damping in" => "dampingIn",
			"Stiffness in" => "stiffnessIn",
			"Max Travel" => "travelOut",
			"Max Damp." => "dampingOut",
			"Max Stiff." => "stiffnessOut",
			"Cs" => "staticFriction",
			"Ck" => "kineticFriction",
			"Cd" => "dragCoefficient",
			"Cl" => "liftCoefficient",
			"Steering angle" => "steeringMax",
			"Sensitivity" => "steeringSensitivity",
			"Acceleration" => "steeringAcceleration",
			"Max Fuel" => "fuelCapacity",
			"Fuel Consumpt" => "fuelConsumption",
			"Refuel Rate" => "refuelRate",
			"Rpm Acc" => "turboAcceleration",
			"Power Mult" => "turboScale",
			"Max Consump" => "turboConsumption",
			"Fuel Cut Off" => "turboEnergyThreshold",
			"Launchtime" => "launchTime",
			"Launch Tolerance" => "launchTolerance",
			"Launch Speed" => "launchSpeed",
			"Mat Rot Speed X" => "maxPitchSpeed",
			"Mat Rot Speed Y" => "maxYawSpeed",
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
		Vector2 initialRow = row.transform.anchoredPosition;
		Vector2 targetRow = new Vector2(-row.images[row.selectedIndex].anchoredPosition.x, initialRow.y);
		Vector2 initialScroll = new Vector2(scrollx ? scrollx.value : 0, 0);
		Vector2 initialSize = new Vector2(scrollx ? scrollx.size : 1, 0);
		float horizontalProgress = row.parts.Count == 1 ? 0 : (float)row.selectedIndex / (row.parts.Count - 1);
		float verticalProgress = rows.Count == 1 ? 0 : (float)selectedRow / (rows.Count - 1);
		Vector2 targetScroll = new Vector2(
			scrollx && scrollx.direction == Scrollbar.Direction.RightToLeft ? 1 - horizontalProgress : horizontalProgress,
			0);
		Vector2 targetSize = new Vector2(1f / row.parts.Count, 1f / rows.Count);
		for (float timer = 0; timer < 1; timer += Time.unscaledDeltaTime)
		{
			float step = F.EasingOutQuint(timer);
			backgroundPosition = Mathf.Lerp(initialBackground, targetBackground, step);
			content.anchoredPosition = Vector2.Lerp(initialContent, targetContent, step);
			row.transform.anchoredPosition = Vector2.Lerp(initialRow, targetRow, step);
			if (scrollx) { scrollx.SetValueWithoutNotify(Mathf.Lerp(initialScroll.x, targetScroll.x, step)); scrollx.size = Mathf.Lerp(initialSize.x, targetSize.x, step); }
			yield return null;
		}
		backgroundPosition = targetBackground;
		content.anchoredPosition = targetContent;
		row.transform.anchoredPosition = targetRow;
		if (scrollx) { scrollx.SetValueWithoutNotify(targetScroll.x); scrollx.size = targetSize.x; }
		containerCo = null;
		d_co = true;
	}

	IEnumerator SetPerformanceBarsAndRadial()
	{
		if (barWidths.Length == 0) yield break;
		//float[] targetBars = carConfig.SGP;
		//float[] initialBars = new float[barWidths.Length];
		//for (int i = 0; i < initialBars.Length; i++) initialBars[i] = bars[i] ? bars[i].sizeDelta.x : 0;
		//for (float timer = 0; timer < 1; timer += Time.unscaledDeltaTime)
		//{
		//	float step = Easing.OutCubic(timer);
		//	for (int i = 0; i < initialBars.Length && i < targetBars.Length; i++)
		//		if (bars[i]) bars[i].sizeDelta = new Vector2(Mathf.Lerp(initialBars[i], targetBars[i] * barWidths[i], step), bars[i].sizeDelta.y);
		//	yield return null;
		//}
		//barsAndRadialCo = null;
	}
}
