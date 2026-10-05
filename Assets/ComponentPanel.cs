using Newtonsoft.Json;
using RVP;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

// Keep these values aligned with ConfigEnumSelector values serialized in Demo.unity.
public enum PartType
{
	Suspension,
	Brakes,
	Fuel,
	Gearbox,
	Chassis,
	Engine,
	Turbo,
	Tyres,
	Steering,
	None
}

public class ComponentPanel : MonoBehaviour
{
	VehicleParent vp;
	public GameObject settersPanel;
	public GameObject mainMenu;
	public GameObject removeSetupBtn;
	public AudioMixerSnapshot paused;
	public AudioMixerSnapshot unPaused;
	public TextMeshProUGUI bottomNameText;
	public GameObject YouSurePanel;
	PartType selectedPart;
	OriginalVehiclePartDefinition openedUserPart;
	OriginalVehiclePhysicsConfig editingConfig;
	readonly List<PhysicsValueBinding> activeBindings = new();

	static readonly string[] GroupNames =
	{
		"Suspension", "Brakes", "Fuel", "Gearbox", "Chassis", "Engine", "Turbo", "Tyres", "Steering"
	};

	void OnEnable()
	{
		F.I.escRef.action.performed += OnEscPressed;
		Cursor.visible = true;
		paused.TransitionTo(0);
		Time.timeScale = 0;
		F.I.gamePaused = true;
		if (!vp || vp != RaceManager.I.playerCar)
			NewSetupButton();
	}

	void OnEscPressed(UnityEngine.InputSystem.InputAction.CallbackContext obj) => BackToComponentMenu();

	void OnDisable()
	{
		// F3 hides the panel without leaving the current part editor.
		if (vp && editingConfig != null)
			foreach (PhysicsValueBinding binding in activeBindings)
				if (binding.Setter) binding.Setter.CommitInput();
		F.I.escRef.action.performed -= OnEscPressed;
		Cursor.visible = false;
		unPaused.TransitionTo(0);
		Time.timeScale = 1;
		F.I.gamePaused = false;
	}

	public void NewSetupButton()
	{
		if (vp && editingConfig != null) vp.carConfig.Apply(vp);
		vp = RaceManager.I.playerCar;
		openedUserPart = null;
		editingConfig = null;
		if (removeSetupBtn) removeSetupBtn.SetActive(false);
		YouSurePanel.SetActive(false);
		mainMenu.SetActive(true);
		settersPanel.SetActive(false);
		bottomNameText.text = vp.carConfig.name;
		PopulateCarConfigTable();
	}

	public void RemoveSetupButton()
	{
		if (mainMenu.activeSelf || openedUserPart == null || !openedUserPart.IsUserPart) return;
		string id = openedUserPart.id;
		var affected = new List<VehicleParent>();
		foreach (VehicleParent vehicle in F.I.s_cars)
			if (vehicle && Array.Exists(vehicle.carConfig?.originalParts?.slots ?? Array.Empty<OriginalVehiclePartSlot>(),
				slot => slot != null && slot.selectedUserPartId == id)) affected.Add(vehicle);
		File.Delete(openedUserPart.sourcePath);
		F.I.ReloadUserParts();
		foreach (VehicleParent vehicle in affected) vehicle.carConfig.Apply(vehicle);
		openedUserPart = null;
		BackToComponentMenu();
	}
	void LateUpdate()
	{
		if (removeSetupBtn) removeSetupBtn.SetActive(!mainMenu.activeSelf && openedUserPart != null && openedUserPart.IsUserPart);
	}
	public void OpenComponentConfigMenu(ConfigEnumSelector type)
	{
		if (!type) return;
		selectedPart = type.componentType;
		if (selectedPart == PartType.None) return;
		OriginalVehiclePartDefinition part = F.I.originalVehiclePartCatalog.GetSelectedPart(vp.carConfig.originalParts, CatalogType(selectedPart));
		openedUserPart = part != null && part.IsUserPart ? part : null;
		editingConfig = vp.carConfig.BuildRuntimePhysicsConfig();
		mainMenu.SetActive(false);
		settersPanel.SetActive(true);
		bottomNameText.text = openedUserPart?.GetName() ?? GroupName(selectedPart);
		PopulatePropertyTable();
		vp.UseOriginalPhysics(editingConfig);
	}
	// Existing Unity button events pass the selected parameter group to LoadConfig.
	public void LoadConfig(ConfigEnumSelector type) => OpenComponentConfigMenu(type);

	IEnumerator ShowLoadDialogCoroutine()
	{
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false,
			F.I.partsPath, null, F.I.LocStr("Select car physics configuration.."), F.I.LocStr("LOAD"));
		if (FileBrowser.Success)
			LoadFromFile(FileBrowser.Result);
	}

	public void LoadFromFile()
	{
		if (FileBrowser.IsOpen)
			return;

		FileBrowser.SetFilters(true, new[]
		{
			new FileBrowser.Filter("Vehicle parts and physics", "." + CarConfig.extension, "." + OriginalVehiclePartCatalog.UserPartExtension)
		});
		StartCoroutine(ShowLoadDialogCoroutine());
	}

	public void LoadFromFile(string[] filepaths)
	{
		if (filepaths == null || filepaths.Length == 0 || string.IsNullOrWhiteSpace(filepaths[0]))
			return;

		string filepath = filepaths[0];
		if (filepath.EndsWith("." + OriginalVehiclePartCatalog.UserPartExtension, StringComparison.OrdinalIgnoreCase))
		{
			LoadUserPart(filepath);
			return;
		}
		if (!filepath.EndsWith("." + CarConfig.extension, StringComparison.OrdinalIgnoreCase))
		{
			Debug.LogWarning("Only original-physics .carcfg files can be loaded.", this);
			return;
		}

		CarConfig loadedConfig = new(Path.GetFileNameWithoutExtension(filepath), File.ReadAllText(filepath));
		if (!OriginalVehiclePhysics.IsUsable(loadedConfig.originalPhysics))
		{
			Debug.LogWarning("The selected .carcfg does not contain a complete original-physics configuration.", this);
			return;
		}

		openedUserPart = null;
		editingConfig = null;
		if (removeSetupBtn) removeSetupBtn.SetActive(false);
		vp.carConfig = loadedConfig;
		vp.carConfig.Apply(vp);
		mainMenu.SetActive(true);
		settersPanel.SetActive(false);
		bottomNameText.text = vp.carConfig.name;
		PopulateCarConfigTable();
	}

	void PopulateCarConfigTable()
	{
		if (!mainMenu) return;
		var catalog = F.I.originalVehiclePartCatalog;
		var setup = F.I.GetDefaultOriginalVehicleSetup(vp.carNumber);
		for (int i = 0; i < mainMenu.transform.childCount; i++)
		{
			ComponentSetter setter = mainMenu.transform.GetChild(i).GetComponent<ComponentSetter>();
			if (!setter) continue;
			ConfigEnumSelector selector = setter.GetComponentInChildren<ConfigEnumSelector>(true);
			PartType group = selector ? selector.componentType : (PartType)i;
			if (group == PartType.None) { setter.gameObject.SetActive(false); continue; }
			OriginalVehiclePartType category = CatalogType(group);
			setter.InitializeOriginalPhysics(GroupName(group), catalog.GetAvailableParts(setup, category),
				vp.carConfig.originalParts?.GetSlot(category)?.SelectedPartId, part =>
				{
					if (vp.carConfig.originalParts.TrySelectPart(part))
					{
						vp.carConfig.MarkModified();
						vp.carConfig.Apply(vp);
					}
				});
		}
	}
	public void PopulatePropertyTable()
	{
		for (int i = settersPanel.transform.childCount - 1; i >= 0; i--)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
		activeBindings.Clear();

		OriginalVehiclePhysicsConfig config = editingConfig ?? vp.carConfig?.BuildRuntimePhysicsConfig();
		if (!OriginalVehiclePhysics.IsUsable(config))
		{
			Debug.LogError("The active car has no valid original-physics configuration.", this);
			return;
		}
		BuildBindings(config, selectedPart);
		GameObject propertySetter = Resources.Load<GameObject>("prefabs/SimpleSetter");
		foreach (PhysicsValueBinding binding in activeBindings)
		{
			PropertySetter setter = Instantiate(propertySetter, settersPanel.transform).GetComponent<PropertySetter>();
			binding.Setter = setter;
			setter.Initialize(binding.Name, binding.Read(), value => EditPhysicsValue(binding, value));
		}
	}

	void BuildBindings(OriginalVehiclePhysicsConfig config, PartType group)
	{
		switch (group)
		{
			case PartType.Suspension:
				AddRootField(config, "rideHeight");
				AddTyreFields(config, "travelIn", "dampingIn", "stiffnessIn", "travelOut", "dampingOut", "stiffnessOut");
				break;
			case PartType.Brakes:
				AddRootField(config, "brakeBias", "brakeAcceleration");
				break;
			case PartType.Fuel:
				AddRootField(config, "fuelCapacity", "fuelUnitMass", "fuelConsumption", "refuelRate");
				break;
			case PartType.Gearbox:
				AddRootField(config, "driveMode", "gearCount", "finalDrive", "shiftTime", "efficiency", "powerSplit");
				AddGearRatios(config);
				break;
			case PartType.Chassis:
				AddRootField(config, "mass", "comA", "comB", "comHeight", "wheelbase", "trackFront", "trackRear",
					"length", "width", "height", "dragCoefficient", "liftCoefficient", "frontalArea",
					"launchTime", "launchTolerance", "launchSpeed", "maxPitchSpeed", "maxYawSpeed");
				break;
			case PartType.Engine:
				AddRootField(config, "rpmIdle", "rpmLimit", "rpmMax", "engineDecay", "maxTorque");
				break;
			case PartType.Turbo:
				AddRootField(config, "turboAcceleration", "turboMax", "turboDecay", "turboScale",
					"turboConsumption", "turboEnergyThreshold");
				break;
			case PartType.Tyres:
				AddTyreFields(config, "radius", "pressure", "staticFriction", "kineticFriction");
				break;
			case PartType.Steering:
				AddRootField(config, "steeringMax", "steeringSensitivity", "steeringAcceleration");
				break;
		}
	}

	void AddRootField(OriginalVehiclePhysicsConfig config, params string[] fieldNames)
	{
		foreach (string fieldName in fieldNames)
			AddNumericField(config, typeof(OriginalVehiclePhysicsConfig).GetField(fieldName), Humanize(fieldName), fieldName);
	}

	void AddGearRatios(OriginalVehiclePhysicsConfig config)
	{
		if (config.ratios == null) return;
		for (int i = 0; i < config.ratios.Length; i++)
		{
			int index = i;
			string gear = i == 0 ? "R" : i == 1 ? "N" : (i - 1).ToString();
			activeBindings.Add(new PhysicsValueBinding($"ratios.{index}", $"Ratio {gear}",
				() => config.ratios[index], value => config.ratios[index] = value));
		}
	}

	void AddTyreFields(OriginalVehiclePhysicsConfig config, params string[] fieldNames)
	{
		if (config.tyres == null) return;
		// Source ordering: rear left/right, then front left/right.
		for (int axle = 0; axle < 2; axle++)
		{
			int left = axle * 2;
			int right = left + 1;
			if (right >= config.tyres.Length || config.tyres[left] == null || config.tyres[right] == null) continue;
			string axleKey = axle == 0 ? "rear" : "front";
			string axleName = axle == 0 ? "Rear" : "Front";
			foreach (string fieldName in fieldNames)
			{
				FieldInfo field = typeof(OriginalTyrePhysicsConfig).GetField(fieldName);
				if (field == null || field.FieldType != typeof(float)) continue;
				activeBindings.Add(new PhysicsValueBinding($"tyres.{axleKey}.{fieldName}", $"{axleName} {Humanize(fieldName)}",
					() => ((float)field.GetValue(config.tyres[left]) + (float)field.GetValue(config.tyres[right])) * 0.5f,
					value =>
					{
						field.SetValue(config.tyres[left], value);
						field.SetValue(config.tyres[right], value);
					}));
			}
		}
	}

	void AddNumericField(object target, FieldInfo field, string label, string key)
	{
		if (target == null || field == null)
			return;

		if (field.FieldType == typeof(float))
		{
			activeBindings.Add(new PhysicsValueBinding(key, label,
				() => (float)field.GetValue(target), value => field.SetValue(target, value)));
		}
		else if (field.FieldType == typeof(int))
		{
			activeBindings.Add(new PhysicsValueBinding(key, label,
				() => (int)field.GetValue(target), value => field.SetValue(target, Mathf.RoundToInt(value))));
		}
	}

	void EditPhysicsValue(PhysicsValueBinding binding, float value)
	{
		binding.Write(value);
		vp.RefreshOriginalPhysicsParameters();
		bottomNameText.text = "*" + (openedUserPart?.GetName() ?? GroupName(selectedPart));
	}

	public void BackToComponentMenu()
	{
		if (mainMenu.activeSelf)
			return;

		vp.carConfig.Apply(vp);
		editingConfig = null;
		openedUserPart = null;
		if (removeSetupBtn) removeSetupBtn.SetActive(false);
		PopulateCarConfigTable();
		bottomNameText.text = (vp.carConfig.Modified ? "*" : "") + vp.carConfig.name;
		settersPanel.SetActive(false);
		mainMenu.SetActive(true);
		activeBindings.Clear();
		for (int i = settersPanel.transform.childCount - 1; i >= 0; i--)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
	}

	public void SaveConfig() => StartCoroutine(mainMenu.activeSelf ? SaveConfigCo() : SavePartCo());

	IEnumerator SaveConfigCo()
	{
		if (FileBrowser.IsOpen)
			yield break;

		FileBrowser.SetFilters(false, new[]
		{
			new FileBrowser.Filter("Original vehicle physics configuration", "." + CarConfig.extension)
		});
		yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false,
			F.I.partsPath, vp.carConfig.name, F.I.LocStr("Save car config file.."), F.I.LocStr("SAVE"));

		if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
			yield break;

		string filepath = FileBrowser.Result[0];
		if (string.IsNullOrWhiteSpace(filepath))
			yield break;
		if (!filepath.EndsWith("." + CarConfig.extension, StringComparison.OrdinalIgnoreCase))
			filepath += "." + CarConfig.extension;

		vp.carConfig.name = Path.GetFileNameWithoutExtension(filepath);
		vp.carConfig.PrepareForSave();
		File.WriteAllText(filepath, JsonConvert.SerializeObject(vp.carConfig, Formatting.Indented));
		bottomNameText.text = vp.carConfig.name;
		if (bottomNameText.text.StartsWith("car", StringComparison.OrdinalIgnoreCase))
			F.I.ReloadCarConfigs();
	}

	static OriginalVehiclePartType CatalogType(PartType group) => group switch
	{
		PartType.Suspension => OriginalVehiclePartType.Suspension,
		PartType.Brakes => OriginalVehiclePartType.Brakes,
		PartType.Fuel => OriginalVehiclePartType.Battery,
		PartType.Gearbox => OriginalVehiclePartType.Gears,
		PartType.Chassis => OriginalVehiclePartType.Chassis,
		PartType.Engine => OriginalVehiclePartType.Engine,
		PartType.Turbo => OriginalVehiclePartType.StuntBoost,
		PartType.Tyres => OriginalVehiclePartType.Tyres,
		_ => OriginalVehiclePartType.Bms
	};
	static PartType EditorType(OriginalVehiclePartType type) => type switch
	{
		OriginalVehiclePartType.Suspension => PartType.Suspension,
		OriginalVehiclePartType.Brakes => PartType.Brakes,
		OriginalVehiclePartType.Battery => PartType.Fuel,
		OriginalVehiclePartType.Gears => PartType.Gearbox,
		OriginalVehiclePartType.Drive => PartType.Gearbox,
		OriginalVehiclePartType.Chassis => PartType.Chassis,
		OriginalVehiclePartType.Engine => PartType.Engine,
		OriginalVehiclePartType.StuntBoost => PartType.Turbo,
		OriginalVehiclePartType.Tyres => PartType.Tyres,
		_ => PartType.Steering
	};
	void LoadUserPart(string filepath)
	{
		try
		{
			var file = JsonConvert.DeserializeObject<OriginalVehicleUserPartFile>(File.ReadAllText(filepath));
			if (file == null || file.formatVersion != 1 || file.id == null || !file.id.StartsWith("user-", StringComparison.Ordinal))
				throw new InvalidDataException("Not a supported user part.");
			Directory.CreateDirectory(F.I.userPartsPath);
			string target = F.I.originalVehiclePartCatalog.GetPart(file.id)?.sourcePath ??
				Path.Combine(F.I.userPartsPath, file.id + "." + OriginalVehiclePartCatalog.UserPartExtension);
			if (!string.Equals(Path.GetFullPath(filepath), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
				File.Copy(filepath, target, true);
			F.I.ReloadUserParts();
			openedUserPart = F.I.originalVehiclePartCatalog.GetPart(file.id);
			if (openedUserPart == null) throw new InvalidDataException("Invalid part parameters.");
			selectedPart = EditorType(openedUserPart.type);
			editingConfig = vp.carConfig.BuildRuntimePhysicsConfig();
			OriginalVehicleUserPartFile.Apply(editingConfig, openedUserPart.physicsOverrides);
			mainMenu.SetActive(false);
			settersPanel.SetActive(true);
			bottomNameText.text = openedUserPart.GetName();
			PopulatePropertyTable();
			vp.UseOriginalPhysics(editingConfig);
		}
		catch (Exception exception) { Debug.LogError("Could not open user part: " + exception.Message, this); }
	}
	IEnumerator SavePartCo()
	{
		if (FileBrowser.IsOpen || activeBindings.Count == 0) yield break;
		foreach (PhysicsValueBinding binding in activeBindings) binding.Setter.CommitInput();
		Directory.CreateDirectory(F.I.userPartsPath);
		FileBrowser.SetFilters(false, new FileBrowser.Filter("User vehicle part", "." + OriginalVehiclePartCatalog.UserPartExtension));
		yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, F.I.userPartsPath,
			openedUserPart?.GetName() ?? GroupName(selectedPart), F.I.LocStr("SAVE"), F.I.LocStr("SAVE"));
		if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0) yield break;
		string filepath = FileBrowser.Result[0];
		if (string.IsNullOrWhiteSpace(filepath)) yield break;
		if (!filepath.EndsWith("." + OriginalVehiclePartCatalog.UserPartExtension, StringComparison.OrdinalIgnoreCase))
			filepath += "." + OriginalVehiclePartCatalog.UserPartExtension;
		bool overwrite = openedUserPart != null && string.Equals(Path.GetFullPath(filepath), openedUserPart.sourcePath, StringComparison.OrdinalIgnoreCase);
		OriginalVehiclePartType type = openedUserPart?.type ?? CatalogType(selectedPart);
		var basePart = F.I.originalVehiclePartCatalog.GetSelectedPart(vp.carConfig.originalParts, type);
		var file = new OriginalVehicleUserPartFile
		{
			id = overwrite ? openedUserPart.id : "user-" + Guid.NewGuid().ToString("N"),
			name = Path.GetFileNameWithoutExtension(filepath), type = type,
			description = openedUserPart?.GetDescription() ?? "User-created part",
			presentationIndex = openedUserPart?.presentationIndex ??
				(basePart != null && basePart.IsUserPart ? basePart.presentationIndex : basePart?.index ?? 1)
		};
		foreach (PhysicsValueBinding binding in activeBindings) file.physics[binding.Key] = binding.Read();
		string json = JsonConvert.SerializeObject(file, Formatting.Indented);
		File.WriteAllText(filepath, json);
		// Keep an installed copy in the discovered directory when exporting elsewhere.
		if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(filepath)), Path.GetFullPath(F.I.userPartsPath), StringComparison.OrdinalIgnoreCase))
			File.WriteAllText(Path.Combine(F.I.userPartsPath, file.id + "." + OriginalVehiclePartCatalog.UserPartExtension), json);
		F.I.ReloadUserParts();
		openedUserPart = F.I.originalVehiclePartCatalog.GetPart(file.id);
		if (openedUserPart != null)
		{
			vp.carConfig.originalParts.TrySelectPart(openedUserPart);
			vp.carConfig.MarkModified();
			F.I.cars[vp.carNumber].config.originalParts.TrySelectPart(openedUserPart);
			F.I.cars[vp.carNumber].config.MarkModified();
			vp.carConfig.Apply(vp);
		}
		bottomNameText.text = file.name;
		editingConfig = vp.carConfig.BuildRuntimePhysicsConfig();
		PopulatePropertyTable();
		vp.UseOriginalPhysics(editingConfig);
	}
	static string GroupName(PartType group)
	{
		int index = (int)group;
		return index >= 0 && index < GroupNames.Length ? GroupNames[index] : "Original physics";
	}

	static string Humanize(string fieldName)
	{
		if (string.IsNullOrEmpty(fieldName))
			return fieldName;
		System.Text.StringBuilder result = new();
		result.Append(char.ToUpperInvariant(fieldName[0]));
		for (int i = 1; i < fieldName.Length; i++)
		{
			if (char.IsUpper(fieldName[i]) && !char.IsUpper(fieldName[i - 1]))
				result.Append(' ');
			result.Append(fieldName[i]);
		}
		return result.ToString();
	}

	sealed class PhysicsValueBinding
	{
		public readonly string Name;
		public readonly string Key;
		public PropertySetter Setter;
		readonly Func<float> read;
		public readonly Action<float> Write;

		public PhysicsValueBinding(string key, string name, Func<float> read, Action<float> write)
		{
			Key = key;
			Name = name;
			this.read = read;
			Write = write;
		}

		public float Read() => read();
	}
}

[Serializable]
public class CarConfig
{
	[NonSerialized]
	public string name;
	[JsonIgnore]
	bool modified;
	[JsonIgnore]
	public bool Modified => modified;
	[JsonIgnore]
	public float[] SGP
	{
		get
		{
			float C01(float min, float max, float val) => Mathf.Clamp(Mathf.InverseLerp(min, max, val), .15f, 1);
			return new[] { C01(1, 10, F.I.Car(name).stunt), C01(1, 10, F.I.Car(name).grip), C01(1, 10, F.I.Car(name).power) };
		}
	}

	[NonSerialized]
	public static readonly string extension = "carcfg";
	public OriginalVehiclePhysicsConfig originalPhysics;
	public OriginalVehicleCarSetup originalParts;

	public CarConfig() { }

	public CarConfig(CarConfig source)
	{
		name = source.name;
		originalPhysics = source.originalPhysics == null ? null :
			JsonConvert.DeserializeObject<OriginalVehiclePhysicsConfig>(
				JsonConvert.SerializeObject(source.originalPhysics));
		originalParts = source.originalParts?.Clone();
	}

	public CarConfig(string name, string jsonText)
	{
		this.name = name;
		CarConfig data = JsonConvert.DeserializeObject<CarConfig>(jsonText);
		originalPhysics = data?.originalPhysics;
		originalParts = data?.originalParts?.Clone();
	}

	public void EnsureOriginalParts(OriginalVehicleCarSetup defaultSetup)
	{
		if (originalParts == null)
			originalParts = defaultSetup?.Clone();
	}

	public void Apply(VehicleParent vehicle = null)
	{
		if (vehicle)
		{
			if (originalParts == null)
				originalParts = F.I?.GetDefaultOriginalVehicleSetup(vehicle.carNumber);
			OriginalVehiclePhysicsConfig runtimePhysics = CreateRuntimePhysicsConfig(originalPhysics, originalParts);
			vehicle.UseOriginalPhysics(runtimePhysics);
			vehicle.originalPartsSetup = originalParts?.Clone();
			vehicle.ApplyOriginalPartPresentation();

			OriginalVehiclePartSlot gearsSlot = originalParts?.GetSlot(OriginalVehiclePartType.Gears);
			OriginalVehiclePartDefinition gearsPart = gearsSlot == null
				? null
				: F.I?.GetOriginalVehiclePart(OriginalVehiclePartType.Gears, gearsSlot.selectedIndex);
		if (runtimePhysics != null)
		{
			string selectedParts = string.Empty;
			for (int i = 0; i < (originalParts?.slots?.Length ?? 0); i++)
			{
				string partId = originalParts.slots[i]?.SelectedPartId;
				if (!string.IsNullOrEmpty(partId))
					selectedParts += (selectedParts.Length == 0 ? string.Empty : ",") + partId;
			}
			int topGearIndex = runtimePhysics.ratios == null || runtimePhysics.ratios.Length == 0
				? -1
				: Mathf.Clamp(runtimePhysics.gearCount, 0, runtimePhysics.ratios.Length - 1);
			float topGearRatio = topGearIndex >= 0 ? runtimePhysics.ratios[topGearIndex] : 0;
			float theoreticalTopSpeed = topGearRatio > 0 && runtimePhysics.tyres != null && runtimePhysics.tyres.Length > 0
				? runtimePhysics.rpmLimit * (runtimePhysics.tyres[0].radius * 0.01f * 2 * Mathf.PI) * 60 /
					(topGearRatio * runtimePhysics.finalDrive * 1000)
				: 0;
			//Debug.Log($"[OriginalVehicleParts] car={name}, source={originalParts?.sourceCarId ?? "<none>"}, " +
			//	$"parts=[{selectedParts}], mass={runtimePhysics.mass:F1}, rpmLimit={runtimePhysics.rpmLimit:F0}, " +
			//	$"maxTorque={runtimePhysics.maxTorque:F1}, gears={Mathf.Max(0, runtimePhysics.gearCount - 1)}, " +
			//	$"topRatio={topGearRatio:F3}, theoreticalTop={theoreticalTopSpeed:F1}km/h.", vehicle);
		}
		}
	}

	public OriginalVehiclePhysicsConfig BuildRuntimePhysicsConfig() => CreateRuntimePhysicsConfig(originalPhysics, originalParts);

	static OriginalVehiclePhysicsConfig CreateRuntimePhysicsConfig(OriginalVehiclePhysicsConfig source, OriginalVehicleCarSetup parts)
	{
		if (source == null)
			return null;

		var runtime = JsonConvert.DeserializeObject<OriginalVehiclePhysicsConfig>(JsonConvert.SerializeObject(source));
		OriginalVehiclePartCatalog catalog = F.I?.originalVehiclePartCatalog;
		if (catalog == null || parts == null)
			return runtime;

		bool Try(string parameter, out float value, int occurrence = 0) =>
			catalog.TryGetEffectiveParameter(parts, parameter, occurrence, out value);
		void Set(string parameter, System.Action<float> assign, int occurrence = 0)
		{
			if (Try(parameter, out float value, occurrence))
				assign(value);
		}

		// The retail setup block starts from the car's dynamics.csv row and overlays
		// every selected part. Additive Mass is summed by the catalog; other fields
		// use the last selected part that provides a value.
		if (Try("Base Mass", out float baseMass) && Try("Additive Mass", out float additiveMass))
			runtime.mass = baseMass + additiveMass;
		Set("Com A", value => runtime.comA = value);
		Set("Com B", value => runtime.comB = value);
		Set("Com H", value => runtime.comHeight = value);
		Set("Ride Height", value => runtime.rideHeight = value);
		Set("Cm", value => runtime.engineDecay = value);
		Set("Rpm Idle", value => runtime.rpmIdle = value);
		Set("Rpm Limit", value => runtime.rpmLimit = value);
		Set("Rpm Max", value => runtime.rpmMax = value);
		Set("Max Torque", value => runtime.maxTorque = value);
		Set("Torque Env", value => runtime.torqueCurveIndex = Mathf.RoundToInt(value));
		Set("Drive Mode", value => runtime.driveMode = Mathf.RoundToInt(value));
		Set("Final Drive", value => runtime.finalDrive = value);
		Set("Gears", value => runtime.gearCount = Mathf.RoundToInt(value) + 1);
		Set("Shift", value => runtime.shiftTime = value);
		Set("Efficiency", value => runtime.efficiency = value);
		Set("4WD Split", value => runtime.powerSplit = value);

		for (int cog = 1; cog <= 8 && runtime.ratios != null; cog++)
		{
			int ratioIndex = cog + 1;
			if (ratioIndex >= runtime.ratios.Length)
				break;
			int selectedCog = cog;
			Set("GearCog" + selectedCog, value => runtime.ratios[ratioIndex] = value);
		}

		Set("Brake Bias", value => runtime.brakeBias = value);
		Set("Brake Accel.", value => runtime.brakeAcceleration = value);
		// The retail tuning block stores one grip/suspension value per part and
		// copies it into all four tyre states. Keep radius and pressure from the
		// per-wheel vehicle cfg, as the original tuner does.
		Set("Cs", value =>
		{
			for (int i = 0; runtime.tyres != null && i < runtime.tyres.Length; i++)
				runtime.tyres[i].staticFriction = value;
		});
		Set("Ck", value =>
		{
			for (int i = 0; runtime.tyres != null && i < runtime.tyres.Length; i++)
				runtime.tyres[i].kineticFriction = value;
		});
		Set("Travel in", value => SetTyreValues(runtime, tyre => tyre.travelIn = value));
		Set("Damping in", value => SetTyreValues(runtime, tyre => tyre.dampingIn = value));
		Set("Stiffness in", value => SetTyreValues(runtime, tyre => tyre.stiffnessIn = value));
		Set("Max Travel", value => SetTyreValues(runtime, tyre => tyre.travelOut = value));
		Set("Max Damp.", value => SetTyreValues(runtime, tyre => tyre.dampingOut = value));
		Set("Max Stiff.", value => SetTyreValues(runtime, tyre => tyre.stiffnessOut = value));
		Set("Cd", value => runtime.dragCoefficient = value);
		Set("Cl", value => runtime.liftCoefficient = value);
		Set("Steering angle", value => runtime.steeringMax = value);
		Set("Sensitivity", value => runtime.steeringSensitivity = value);
		Set("Acceleration", value => runtime.steeringAcceleration = value);
		Set("Max Fuel", value => runtime.fuelCapacity = value);
		Set("Fuel Consumpt", value => runtime.fuelConsumption = value);
		Set("Refuel Rate", value => runtime.refuelRate = value);
		Set("Rpm Acc", value => runtime.turboAcceleration = value);
		Set("Rpm Max", value => runtime.turboMax = value, 1);
		Set("Cm", value => runtime.turboDecay = value, 1);
		Set("Power Mult", value => runtime.turboScale = value);
		Set("Max Consump", value => runtime.turboConsumption = value);
		Set("Fuel Cut Off", value => runtime.turboEnergyThreshold = value);
		Set("Launchtime", value => runtime.launchTime = value);
		Set("Launch Tolerance", value => runtime.launchTolerance = value);
		Set("Launch Speed", value => runtime.launchSpeed = value);
		Set("Mat Rot Speed X", value => runtime.maxPitchSpeed = value);
		Set("Mat Rot Speed Y", value => runtime.maxYawSpeed = value);

		foreach (OriginalVehiclePartSlot slot in parts.slots ?? Array.Empty<OriginalVehiclePartSlot>())
			OriginalVehicleUserPartFile.Apply(runtime, catalog.GetPart(slot?.SelectedPartId)?.physicsOverrides);
		return runtime;
	}

	static void SetTyreValues(OriginalVehiclePhysicsConfig config, System.Action<OriginalTyrePhysicsConfig> assign)
	{
		if (config?.tyres == null)
			return;
		for (int i = 0; i < config.tyres.Length; i++)
			if (config.tyres[i] != null)
				assign(config.tyres[i]);
	}

	public void MarkModified() => modified = true;

	internal void PrepareForSave() => modified = false;
}
