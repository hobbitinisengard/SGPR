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
	Launch,
	None
}

public class ComponentPanel : MonoBehaviour
{
	VehicleParent vp;
	public GameObject settersPanel;
	public GameObject mainMenu;
	public AudioMixerSnapshot paused;
	public AudioMixerSnapshot unPaused;
	public TextMeshProUGUI bottomNameText;
	public GameObject YouSurePanel;
	PartType selectedPart;
	readonly List<PhysicsValueBinding> activeBindings = new();

	static readonly string[] GroupNames =
	{
		"Suspension", "Brakes", "Fuel", "Gearbox", "Chassis", "Engine", "Turbo", "Tyres", "Steering", "Launch"
	};

	void OnEnable()
	{
		F.I.escRef.action.performed += OnEscPressed;
		Cursor.visible = true;
		paused.TransitionTo(0);
		Time.timeScale = 0;
		F.I.gamePaused = true;
		if (vp == null)
			NewSetupButton();
	}

	void OnEscPressed(UnityEngine.InputSystem.InputAction.CallbackContext obj) => BackToComponentMenu();

	void OnDisable()
	{
		F.I.escRef.action.performed -= OnEscPressed;
		Cursor.visible = false;
		unPaused.TransitionTo(0);
		Time.timeScale = 1;
		F.I.gamePaused = false;
	}

	public void NewSetupButton()
	{
		vp = RaceManager.I.playerCar;
		YouSurePanel.SetActive(false);
		mainMenu.SetActive(true);
		settersPanel.SetActive(false);
		bottomNameText.text = vp.carConfig.name;
		PopulateCarConfigTable();
	}

	public void OpenComponentConfigMenu(ConfigEnumSelector type)
	{
		selectedPart = type.componentType;
		mainMenu.SetActive(false);
		settersPanel.SetActive(true);
		bottomNameText.text = GroupName(selectedPart);
		PopulatePropertyTable();
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
			new FileBrowser.Filter("Original vehicle physics configuration", "." + CarConfig.extension)
		});
		StartCoroutine(ShowLoadDialogCoroutine());
	}

	public void LoadFromFile(string[] filepaths)
	{
		if (filepaths == null || filepaths.Length == 0 || string.IsNullOrWhiteSpace(filepaths[0]))
			return;

		string filepath = filepaths[0];
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

		vp.carConfig = loadedConfig;
		vp.carConfig.Apply(vp);
		mainMenu.SetActive(true);
		settersPanel.SetActive(false);
		bottomNameText.text = vp.carConfig.name;
		PopulateCarConfigTable();
	}

	void PopulateCarConfigTable()
	{
		if (!mainMenu)
			return;

		int groupCount = Enum.GetValues(typeof(PartType)).Length - 1;
		for (int i = 0; i < mainMenu.transform.childCount && i < groupCount; i++)
		{
			ComponentSetter setter = mainMenu.transform.GetChild(i).GetComponent<ComponentSetter>();
			if (setter)
				setter.InitializeOriginalPhysics(GroupName((PartType)i));
		}
	}

	public void PopulatePropertyTable()
	{
		for (int i = settersPanel.transform.childCount - 1; i >= 0; i--)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
		activeBindings.Clear();

		OriginalVehiclePhysicsConfig config = vp.carConfig?.originalPhysics;
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
				AddRootArray(config, "frictionCurve");
				break;
			case PartType.Fuel:
				AddRootField(config, "fuelCapacity", "fuelUnitMass", "fuelConsumption", "refuelRate");
				AddRootArray(config, "consumptionCurve");
				break;
			case PartType.Gearbox:
				AddRootField(config, "driveMode", "gearCount", "finalDrive", "shiftTime", "efficiency", "powerSplit");
				AddRootArray(config, "ratios");
				break;
			case PartType.Chassis:
				AddRootField(config, "mass", "comA", "comB", "comHeight", "wheelbase", "trackFront", "trackRear",
					"length", "width", "height", "dragCoefficient", "liftCoefficient", "frontalArea");
				break;
			case PartType.Engine:
				AddRootField(config, "rpmIdle", "rpmLimit", "rpmMax", "engineDecay", "maxTorque");
				AddRootMatrix(config, "torqueCurves");
				break;
			case PartType.Turbo:
				AddRootField(config, "turboAcceleration", "turboMax", "turboDecay", "turboScale",
					"turboConsumption", "turboEnergyThreshold");
				AddRootArray(config, "turboCurve");
				break;
			case PartType.Tyres:
				AddTyreFields(config, "radius", "pressure", "staticFriction", "kineticFriction");
				break;
			case PartType.Steering:
				AddRootField(config, "steeringMax", "steeringSensitivity", "steeringAcceleration");
				AddRootArray(config, "steeringCurve", "digitalSteering", "digitalBrake", "analogSteering",
					"analogBrake", "velocitySteering");
				break;
			case PartType.Launch:
				AddRootField(config, "launchTime", "launchTolerance", "launchSpeed", "maxPitchSpeed", "maxYawSpeed");
				break;
		}
	}

	void AddRootField(OriginalVehiclePhysicsConfig config, params string[] fieldNames)
	{
		foreach (string fieldName in fieldNames)
			AddNumericField(config, typeof(OriginalVehiclePhysicsConfig).GetField(fieldName), Humanize(fieldName));
	}

	void AddRootArray(OriginalVehiclePhysicsConfig config, params string[] fieldNames)
	{
		foreach (string fieldName in fieldNames)
		{
			FieldInfo field = typeof(OriginalVehiclePhysicsConfig).GetField(fieldName);
			if (field?.GetValue(config) is not float[] values)
				continue;

			for (int i = 0; i < values.Length; i++)
			{
				int index = i;
				string label = ArrayValueName(fieldName, index);
				activeBindings.Add(new PhysicsValueBinding(label, () => values[index], value => values[index] = value));
			}
		}
	}

	void AddRootMatrix(OriginalVehiclePhysicsConfig config, string fieldName)
	{
		FieldInfo field = typeof(OriginalVehiclePhysicsConfig).GetField(fieldName);
		if (field?.GetValue(config) is not float[][] values)
			return;

		for (int curve = 0; curve < values.Length; curve++)
		{
			float[] samples = values[curve];
			if (samples == null)
				continue;
			for (int sample = 0; sample < samples.Length; sample++)
			{
				int curveIndex = curve;
				int sampleIndex = sample;
				activeBindings.Add(new PhysicsValueBinding(
					$"Torque curve {curveIndex} [{sampleIndex}]",
					() => values[curveIndex][sampleIndex],
					value => values[curveIndex][sampleIndex] = value));
			}
		}
	}

	void AddTyreFields(OriginalVehiclePhysicsConfig config, params string[] fieldNames)
	{
		if (config.tyres == null)
			return;

		string[] wheelNames = { "Rear left", "Rear right", "Front left", "Front right" };
		for (int wheel = 0; wheel < config.tyres.Length && wheel < wheelNames.Length; wheel++)
		{
			OriginalTyrePhysicsConfig tyre = config.tyres[wheel];
			if (tyre == null)
				continue;
			foreach (string fieldName in fieldNames)
				AddNumericField(tyre, typeof(OriginalTyrePhysicsConfig).GetField(fieldName),
					$"{wheelNames[wheel]} {Humanize(fieldName)}");
		}
	}

	void AddNumericField(object target, FieldInfo field, string label)
	{
		if (target == null || field == null)
			return;

		if (field.FieldType == typeof(float))
		{
			activeBindings.Add(new PhysicsValueBinding(label,
				() => (float)field.GetValue(target), value => field.SetValue(target, value)));
		}
		else if (field.FieldType == typeof(int))
		{
			activeBindings.Add(new PhysicsValueBinding(label,
				() => (int)field.GetValue(target), value => field.SetValue(target, Mathf.RoundToInt(value))));
		}
	}

	void EditPhysicsValue(PhysicsValueBinding binding, float value)
	{
		binding.Write(value);
		vp.carConfig.MarkModified();
		vp.RefreshOriginalPhysicsParameters();
		bottomNameText.text = "*" + vp.carConfig.name;
	}

	public void BackToComponentMenu()
	{
		if (mainMenu.activeSelf)
			return;

		PopulateCarConfigTable();
		bottomNameText.text = (vp.carConfig.Modified ? "*" : "") + vp.carConfig.name;
		settersPanel.SetActive(false);
		mainMenu.SetActive(true);
		activeBindings.Clear();
		for (int i = settersPanel.transform.childCount - 1; i >= 0; i--)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
	}

	public void SaveConfig() => StartCoroutine(SaveConfigCo());

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

	static string ArrayValueName(string fieldName, int index)
	{
		if (fieldName == "ratios")
		{
			if (index == 0) return "Reverse ratio";
			if (index == 1) return "Neutral ratio";
			return $"Gear {index - 1} ratio";
		}
		return $"{Humanize(fieldName)} [{index}]";
	}

	sealed class PhysicsValueBinding
	{
		public readonly string Name;
		readonly Func<float> read;
		public readonly Action<float> Write;

		public PhysicsValueBinding(string name, Func<float> read, Action<float> write)
		{
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
			// Temporary diagnostic: run Formula 17 with car 17's fully assembled
			// original-physics setup while retaining Formula 17's source identity and
			// model dimensions. Remove this block after the suspension comparison.
			if (vehicle.carNumber == 18 && F.I?.cars != null && F.I.cars.Length > 17)
			{
				CarConfig donor = F.I.cars[17].config;
				OriginalVehicleCarSetup donorParts = donor?.originalParts ??
					F.I.GetDefaultOriginalVehicleSetup(17);
				OriginalVehiclePhysicsConfig donorPhysics = donor == null ? null :
					CreateRuntimePhysicsConfig(donor.originalPhysics, donorParts);
				if (OriginalVehiclePhysics.IsUsable(runtimePhysics) &&
					OriginalVehiclePhysics.IsUsable(donorPhysics))
				{
					donorPhysics.sourceConfig = runtimePhysics.sourceConfig;
					donorPhysics.wheelbase = runtimePhysics.wheelbase;
					donorPhysics.trackFront = runtimePhysics.trackFront;
					donorPhysics.trackRear = runtimePhysics.trackRear;
					donorPhysics.length = runtimePhysics.length;
					donorPhysics.width = runtimePhysics.width;
					donorPhysics.height = runtimePhysics.height;
					if (runtimePhysics.tyres != null && donorPhysics.tyres != null)
						for (int i = 0; i < Mathf.Min(runtimePhysics.tyres.Length, donorPhysics.tyres.Length); i++)
							donorPhysics.tyres[i].radius = runtimePhysics.tyres[i].radius;

					runtimePhysics = donorPhysics;
					Debug.LogWarning($"[OriginalVehicleParts] Formula 17 diagnostic: using car17 physics " +
						$"with Formula 17 dimensions. mass={runtimePhysics.mass:F1}, " +
						$"comHeight={runtimePhysics.comHeight:F1}, " +
						$"springIn={runtimePhysics.tyres[0].stiffnessIn:F3}/" +
						$"{runtimePhysics.tyres[2].stiffnessIn:F3}, " +
						$"springOut={runtimePhysics.tyres[0].stiffnessOut:F3}/" +
						$"{runtimePhysics.tyres[2].stiffnessOut:F3}, " +
						$"travelOut={runtimePhysics.tyres[0].travelOut:F1}/" +
						$"{runtimePhysics.tyres[2].travelOut:F1}.", vehicle);
				}
				else
					Debug.LogError("[OriginalVehicleParts] Formula 17 diagnostic could not load valid car17 physics; " +
						"Formula 17 is using its own configuration.", vehicle);
			}
			vehicle.UseOriginalPhysics(runtimePhysics);
			vehicle.originalPartsSetup = originalParts?.Clone();

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
