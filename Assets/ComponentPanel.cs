using Newtonsoft.Json;
using RVP;
using SFB;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
//public struct TuningProperty
//{
//	public string name;
//	public float value;
//	public Action applyingFunc;
//	public TuningProperty(string name, float value, Action applyingFunc)
//	{
//		this.name = name;
//		this.value = value;
//		this.applyingFunc = applyingFunc;
//	}
//}
public enum PartType
{
	Suspension,
	Bms,
	Battery,
	Engine,
	Chassis,
	Gears,
	Boost,
	Tyre,
	Drive,
	None
}
public class ComponentPanel : MonoBehaviour
{
	public GameObject settersPanel;
	public GameObject mainMenu;
	/// <summary>
	/// shows component name or carConfig name if no component is selected
	/// </summary>
	public TextMeshProUGUI bottomNameText;
	public SGP_HUD hud;
	public GameObject YouSurePanel;
	PartType selectedPart;
	CarConfig carConfig;
	private void OnEnable()
	{
		Time.timeScale = 0;
		Info.gamePaused = true;
	}
	void OnDisable()
	{
		Time.timeScale = 1;
		Info.gamePaused = false;
	}
	private void Start()
	{
		NewSetupButton();
	}
	public void NewSetupButton()
	{
		carConfig = new CarConfig(hud.vp);
		YouSurePanel.gameObject.SetActive(false);
		mainMenu.SetActive(true);
		settersPanel.SetActive(false);
		if (mainMenu.activeSelf)
		{
			bottomNameText.text = carConfig.name;
		}
		PopulateCarConfigTable();
	}
	public static void AddPart(string filepath)
	{
		string jsonText = File.ReadAllText(filepath);
		string partName = Path.GetFileNameWithoutExtension(filepath);
		PartSavable part;
		if (filepath.EndsWith("suscfg"))
			part = JsonConvert.DeserializeObject<SuspensionSavable>(jsonText);
		else if (filepath.EndsWith("bmscfg"))
			part = JsonConvert.DeserializeObject<BmsSavable>(jsonText);
		else if (filepath.EndsWith("batcfg"))
			part = JsonConvert.DeserializeObject<BatterySavable>(jsonText);
		else if (filepath.EndsWith("engcfg"))
			part = JsonConvert.DeserializeObject<EngineSavable>(jsonText);
		else if (filepath.EndsWith("chacfg"))
			part = JsonConvert.DeserializeObject<ChassisSavable>(jsonText);
		else if (filepath.EndsWith("grscfg"))
			part = JsonConvert.DeserializeObject<GearboxSavable>(jsonText);
		else if (filepath.EndsWith("jetcfg"))
			part = JsonConvert.DeserializeObject<BoostSavable>(jsonText);
		else if (filepath.EndsWith("tyrcfg"))
			part = JsonConvert.DeserializeObject<TyreSavable>(jsonText);
		else if (filepath.EndsWith("drvcfg"))
			part = JsonConvert.DeserializeObject<DriveSavable>(jsonText);
		else
			return;
		Info.carParts.Add(partName, part);
	}
	public void OpenComponentConfigMenu(ConfigEnumSelector type)
	{
		mainMenu.SetActive(false);
		settersPanel.SetActive(true);

		// part can be custom or external
		selectedPart = type.componentType;
		bottomNameText.text = (carConfig.IsPartModified(selectedPart) ? "*" : "") 
			+ carConfig.GetPartName(selectedPart);

		PopulatePropertyTable();
	}

	public void LoadFromFile()
	{
		string[] extensions = Info.partInfos.Select(i => i.fileExtension).ToArray();
		var extensionFilter = new[] {
			 new ExtensionFilter("SGPR car parts configuration files", extensions)};
		string filepath = StandaloneFileBrowser.OpenFilePanel("Select configuration file..",
				Info.partsPath, extensionFilter, false)[0];

		if(filepath.Length > 0)
		{
			if (filepath.EndsWith("carcfg"))
			{
				// Load carcfg menu
				if (filepath.Length > 0)
				{
					string jsonText = File.ReadAllText(filepath);
					bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
					carConfig = new CarConfig(hud.vp, bottomNameText.text, jsonText);

					mainMenu.SetActive(true);
					settersPanel.SetActive(false);

					PopulateCarConfigTable();
				}
			}
			else
			{ // Load component menu
				if (filepath.Length > 0)
				{
					selectedPart = PartType.None;
					for (int i = 0; i < Info.partInfos.Length - 1; ++i)
					{
						if (filepath.EndsWith(Info.partInfos[i].fileExtension))
						{
							selectedPart = (PartType)i;
							break;
						}
					}
					if (selectedPart == PartType.None)
					{
						Debug.LogError("Didn't find componentType");
						return;
					}
					bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
					carConfig.SetPartTo(selectedPart, bottomNameText.text);
					mainMenu.SetActive(false);
					settersPanel.SetActive(true);

					PopulatePropertyTable();
				}
			}
		}
	}
	private void PopulateCarConfigTable()
	{
		for (int i = 0; i < mainMenu.transform.childCount; ++i)
		{ // dropdown has only external parts. You can tick toggle to make it custom (saved in carConfig)
			string[] files = Directory.GetFiles(
				Info.partsPath, "*." + Info.partInfos[i].fileExtension);

			int choiceIdx = -1;
			string selectedPartName = carConfig.GetPartName((PartType)i);
			for (int j = 0; j < files.Length; ++j)
			{
				files[j] = Path.GetFileNameWithoutExtension(files[j]);
				if (choiceIdx == -1 && files[j] == selectedPartName)
					choiceIdx = j;
			}
			mainMenu.transform.GetChild(i).GetComponent<ComponentSetter>()
				.Initialize((PartType)i, files.ToList(), choiceIdx, EditCarConfigCallback);
		}
	}
	public void PopulatePropertyTable()
	{
		for (int i = 0; i < settersPanel.transform.childCount; ++i)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
		PartSavable curPart = carConfig.GetPart(selectedPart);
		var fields = curPart.GetType().GetFields();
		GameObject propertySetter = Resources.Load<GameObject>("prefabs/SimpleSetter");
		foreach (var field in fields)
		{
			PropertySetter instantiatedSetter = Instantiate(propertySetter, settersPanel.transform).GetComponent<PropertySetter>();
			float value = (float)field.GetValue(curPart);
			instantiatedSetter.Initialize(field.Name, value, EditPartCallback);
		}
	}

	public void BackToComponentMenu()
	{
		if (carConfig.IsPartModified(selectedPart))
		{
			carConfig.PreserveUnsavedComponentAsCustom(selectedPart);
		}
		Info.ReloadCarPartsData();
		PopulateCarConfigTable();
		bottomNameText.text = (carConfig.Modified ? "*" : "") + carConfig.name;
		settersPanel.SetActive(false);
		mainMenu.SetActive(true);

		for (int i = 0; i < settersPanel.transform.childCount; ++i)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
	}
	public void SaveConfig()
	{
		if (mainMenu.activeSelf)
		{ // saving car configuration
			string filepath = StandaloneFileBrowser.SaveFilePanel("Save car config file..",
				Info.partsPath, carConfig.name, CarConfig.extension);
			if (filepath != null && filepath.Length > 3)
			{
				carConfig.PrepareForSave();
				string serializedJson = JsonConvert.SerializeObject(carConfig, Formatting.Indented);
				bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
				carConfig.name = bottomNameText.text;
				File.WriteAllText(filepath, serializedJson);
				if(bottomNameText.text.Contains("car"))
					Info.ReloadCarConfigs();
			}
		}
		else
		{ // saving part
			string filepath = StandaloneFileBrowser.SaveFilePanel("Save part file..",
				Info.partsPath, carConfig.GetPartName(selectedPart), Info.partInfos[(int)selectedPart].fileExtension);
			if(filepath != null && filepath.Length > 3)
			{
				var curPart = carConfig.GetPart(selectedPart);
				string serializedJson = JsonConvert.SerializeObject(curPart, Formatting.Indented);
				bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
				if(!File.Exists(filepath))
					Info.carParts.Add(bottomNameText.text, curPart);
				File.WriteAllText(filepath, serializedJson);
				carConfig.SetPartTo(selectedPart, bottomNameText.text);
			}
		}
	}
	void EditCarConfigCallback(PartType newPartType, string partName)
	{
		if(carConfig != null)
		{
			if (!carConfig.Modified)
			{
				carConfig.Modified = true;
				bottomNameText.text = "*" + bottomNameText.text;
			}
			selectedPart = newPartType;
			carConfig.SetPartTo(newPartType, partName);
		}
	}
	void EditPartCallback()
	{
		if (!carConfig.IsPartModified(selectedPart))
		{
			carConfig.MarkPartModified(selectedPart, true);
			bottomNameText.text = "*" + bottomNameText.text;
		}
		var curPart = carConfig.GetPart(selectedPart);
		var fields = curPart.GetType().GetFields();
		int j = 0;
		foreach (FieldInfo field in fields)
		{
			field.SetValue(curPart, settersPanel.transform.GetChild(j).GetComponent<PropertySetter>().value);
			j++;
		}
		curPart.Apply(hud.vp);
	}
}
[Serializable]
public class PartsStruct
{
	public SuspensionSavable sus;
	public BmsSavable bms;
	public BatterySavable battery;
	public EngineSavable engine;
	public ChassisSavable chassis;
	public GearboxSavable gearbox;
	public BoostSavable boost;
	public TyreSavable tyre;
	public DriveSavable drive;
	public PartSavable this[PartType key]
	{
		get {
			return key switch
			{
				PartType.Suspension => sus,
				PartType.Bms => bms,
				PartType.Battery => battery,
				PartType.Engine => engine,
				PartType.Chassis => chassis,
				PartType.Gears => gearbox,
				PartType.Boost => boost,
				PartType.Tyre => tyre,
				PartType.Drive => drive,
				_ => null,
			};
		}
		set
		{
			switch (key)
			{
				case PartType.Suspension://G & S
					sus = (SuspensionSavable)value;
					break;
				case PartType.Bms://G & S
					bms = (BmsSavable)value;
					break;
				case PartType.Battery://P
					battery = (BatterySavable)value;
					break;
				case PartType.Engine://P
					engine = (EngineSavable)value;
					break;
				case PartType.Chassis://S
					chassis = (ChassisSavable)value;
					break;
				case PartType.Gears://P
					gearbox = (GearboxSavable)value;
					break;
				case PartType.Boost://P
					boost = (BoostSavable)value;
					break;
				case PartType.Tyre://G
					tyre = (TyreSavable)value;
					break;
				case PartType.Drive://G
					drive = (DriveSavable)value;
					break;
				case PartType.None:
					break;
				default:
					break;
			}
		}
	}
}

[Serializable]
public class CarConfig
{	
	[NonSerialized]
	public string name;
	[NonSerialized]
	public VehicleParent vp;
	[NonSerialized]
	bool modified;
	/// <summary>
	/// Returns Stunt, Grip, Power coefficients in range <0;1>
	/// </summary>
	/// 
	[JsonIgnore]
	public float[] SGP
	{
		get
		{
			float Tier(PartType type)
			{
				float tier;
				if (externalParts[(int)type] != null)
					tier = 1+Info.carParts[externalParts[(int)type]].tier;
				else
					tier = 1+customParts[type].tier;
				return Mathf.Clamp(tier, 1, 5);
			}
			int maxTier = 5;
			float S = (Tier(PartType.Suspension) + Tier(PartType.Bms) + Tier(PartType.Chassis)) / (3*maxTier);
			// PartType.Drive has only 3 tiers -> scale it to 5
			float G = (Tier(PartType.Suspension) + Tier(PartType.Bms) + 3*Tier(PartType.Tyre) + 2*5/3f*Tier(PartType.Drive)) / (7*maxTier);
			float P = (Tier(PartType.Battery) + 2*Tier(PartType.Engine) + 2*Tier(PartType.Gears) + Tier(PartType.Boost)) / (6*maxTier);
			return new float[] { S, G, P };
		}
	}
	[JsonIgnore]
	public bool Modified
	{
		get
		{
			return modified || modifiedParts.FirstOrDefault(p => p);
		}
		set
		{
			modified = value;
		}
	}
	[NonSerialized]
	public static readonly string extension = "carcfg";
	[NonSerialized]
	bool[] modifiedParts;
	[NonSerialized]
	PartSavable curPart;
	[NonSerialized]
	PartType curPartType;
	[SerializeField]
	string[] externalParts;
	[SerializeField]
	PartsStruct customParts;
	/// <summary>
	/// Initialize from Json (automatic)
	/// </summary>
	public CarConfig()
	{}
	/// <summary>
	/// Initialize from car
	/// </summary>
	/// <param name="vp"></param>
	public CarConfig(VehicleParent vp)
	{
		this.vp = vp;
		this.name = "Untitled";
		modifiedParts = new bool[9];
		externalParts = new string[9];//Enumerable.Repeat("", 9).ToArray();
		customParts = new PartsStruct();
		customParts[(PartType)0] = new SuspensionSavable(vp);
		customParts[(PartType)1] = new BmsSavable(vp);
		customParts[(PartType)2] = new BatterySavable(vp);
		customParts[(PartType)3] = new EngineSavable(vp);
		customParts[(PartType)4] = new ChassisSavable(vp);
		customParts[(PartType)5] = new GearboxSavable(vp);
		customParts[(PartType)6] = new BoostSavable(vp);
		customParts[(PartType)7] = new TyreSavable(vp);
		customParts[(PartType)8] = new DriveSavable(vp);
	}
	public CarConfig(VehicleParent vp, string name, string jsonText)
	{
		this.vp = vp;
		this.name = name;
		var data = JsonConvert.DeserializeObject<CarConfig>(jsonText);
		modifiedParts = new bool[9];
		externalParts = data.externalParts;
		customParts = data.customParts;
		Apply();
	}
	public void Apply(VehicleParent vp = null)
	{
		if (vp == null)
			vp = this.vp;

		if(vp)
		{
			for (int i = 0; i < 9; ++i)
			{
				GetPart((PartType)i).Apply(vp);
			}
		}
	}
	public PartSavable GetPart(PartType type)
	{
		if(curPart == null || curPartType != type)
		{
			if (externalParts[(int)type] != null)
			{
				customParts[type] = Info.carParts[externalParts[(int)type]].Clone();
			}
			curPart = customParts[type];
			curPartType = type;
		}
		return curPart;
	}
	public void SetPartTo(PartType type, string partName)
	{
		if (partName == null)
		{ 
			if (externalParts[(int)type] != null)
			{// set part as custom
				modifiedParts[(int)type] = false;
				customParts[type] = Info.carParts[externalParts[(int)type]].Clone();
				externalParts[(int)type] = null;
				curPartType = type;
				curPart = customParts[type];
			}
		}
		else
		{ // set external part
			modifiedParts[(int)type] = false;
			externalParts[(int)type] = partName;
			customParts[type] = null;
			curPartType = type;
			curPart = Info.carParts[externalParts[(int)type]].Clone();
		}
		curPart.Apply(vp);
	}
	public void PreserveUnsavedComponentAsCustom(PartType type)
	{
		externalParts[(int)type] = null;
	}
	public string GetPartName(PartType type)
	{
		if (externalParts[(int)type] != null)
			return externalParts[(int)type];
		else
			return "Custom";
	}
	public bool IsPartModified(PartType curPartType)
	{
		return modifiedParts[(int)curPartType];
	}

	internal void MarkPartModified(PartType curPartType, bool state)
	{
		modifiedParts[(int)curPartType] = state;
	}

	internal void PrepareForSave()
	{
		for(int i=0; i<externalParts.Length; ++i)
		{
			if (externalParts[i] != null)
				customParts[(PartType)i] = null;
		}
		modified = false;
	}
}

[Serializable]
public abstract class PartSavable
{
	public float tier;
	public abstract void Apply(VehicleParent vp);
	public abstract void InitializeFromCar(VehicleParent vp);
	public abstract PartSavable Clone();
}

[Serializable]
public class DriveSavable : PartSavable
{
	// tier 0=FWD, 1=RWD, 2=AWD
	public float secsForMaxSteering;
	public float steerLimitAt0kph;
	public float steerLimitAt100kph;
	public DriveSavable()
	{
	}
	public DriveSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	DriveSavable(DriveSavable original)
	{
		tier = original.tier;
		secsForMaxSteering = original.secsForMaxSteering;
		steerLimitAt0kph = original.steerLimitAt0kph;
		steerLimitAt100kph = original.steerLimitAt100kph;
	}
	public override PartSavable Clone()
	{
		return new DriveSavable(this);
	}

	public override void InitializeFromCar(VehicleParent vp)
	{
		tier = (int)vp.engine.transmission.Drive;
		secsForMaxSteering = vp.steeringControl.secsForMaxSteering;
		steerLimitAt0kph = vp.steeringControl.steerLimitCurve.keys[0].value;
		steerLimitAt100kph = vp.steeringControl.steerLimitCurve.keys[1].value;
	}
	public override void Apply(VehicleParent vp)
	{
		if (vp.wheels[0].tireWidth < vp.wheels[2].tireWidth)
			vp.engine.transmission.Drive = GearboxTransmission.DriveType.RWD;
		else
			vp.engine.transmission.Drive = (GearboxTransmission.DriveType)tier;

		vp.steeringControl.secsForMaxSteering = secsForMaxSteering;
		vp.steeringControl.steerLimitCurve = AnimationCurve.Linear(0, steerLimitAt0kph, 30, steerLimitAt100kph);
	}
}
[Serializable]
public class TyreSavable : PartSavable
{
	public float friction;
	public float frictionStretch;
	public float squeakSlipThreshold;
	public float slipDependence;
	public float axleFriction; // for simulating offroad tyres
	public TyreSavable()
	{
	}
	public TyreSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	TyreSavable(TyreSavable original)
	{
		tier = original.tier;
		friction = original.friction;
		frictionStretch = original.frictionStretch;
		squeakSlipThreshold = original.squeakSlipThreshold;
		slipDependence = original.slipDependence;
		axleFriction = original.axleFriction;
	}
	public override PartSavable Clone()
	{
		return new TyreSavable(this);
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		// get data from RL tyre
		var w = vp.wheels[2];
		friction = w.sidewaysFriction;
		frictionStretch = w.sidewaysCurveStretch;
		squeakSlipThreshold = w.slipThreshold;
		slipDependence = (w.slipDependence == Wheel.SlipDependenceMode.forward) ? 0 : 1;
		axleFriction = w.axleFriction;
	}

	public override void Apply(VehicleParent vp)
	{
		foreach (var w in vp.wheels)
		{
			w.forwardFriction = friction;
			w.sidewaysFriction = friction;
			w.forwardCurveStretch = frictionStretch;
			w.sidewaysCurveStretch = frictionStretch;
			w.slipThreshold = squeakSlipThreshold;
			w.slipDependence = (int)slipDependence == 0 ? Wheel.SlipDependenceMode.forward : Wheel.SlipDependenceMode.independent;
			w.axleFriction = axleFriction;
			// update materials
			var mr = w.transform.GetChild(0).GetComponent<MeshRenderer>();
			// [..^1] = from beginning to last - 1. |         tier+1 cause tyres are named from 1
			string name = mr.sharedMaterials[0].name[..^1] + (tier+1).ToString();
			Material tyreMat = Resources.Load<Material>("materials/" + name);
			Material[] mats = mr.materials;
			mats[0] = tyreMat;
			mr.materials = mats;
		}
	}
}
[Serializable]
public class BoostSavable : PartSavable
{
	public float maxBoost;
	public float batteryConsumption;
	public BoostSavable()
	{
	}
	public BoostSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	BoostSavable(BoostSavable original)
	{
		tier = original.tier;
		maxBoost = original.maxBoost;
		batteryConsumption = original.batteryConsumption;
	}
	public override PartSavable Clone()
	{
		return new BoostSavable(this);
	}

	public override void Apply(VehicleParent vp)
	{
		vp.engine.maxBoost = maxBoost;
		vp.engine.jetConsumption = batteryConsumption;
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		maxBoost = vp.engine.maxBoost;
		batteryConsumption = vp.engine.jetConsumption;
	}
}
[Serializable]
public class GearboxSavable : PartSavable
{
	// tier >=2 -> quick gear reducing
	public float shiftDelaySeconds;
	public float reverseGearRatio;
	public float Gear1Ratio;
	public float Gear2Ratio;
	public float Gear3Ratio;
	public float Gear4Ratio;
	public float Gear5Ratio;
	public float Gear6Ratio;
	public float Gear7Ratio;
	public float Gear8Ratio;
	public GearboxSavable()
	{
	}
	public GearboxSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	GearboxSavable(GearboxSavable original)
	{
		shiftDelaySeconds = original.shiftDelaySeconds;
		reverseGearRatio = original.reverseGearRatio;
		Gear1Ratio = original.Gear1Ratio;
		Gear2Ratio = original.Gear2Ratio;
		Gear3Ratio = original.Gear3Ratio;
		Gear4Ratio = original.Gear4Ratio;
		Gear5Ratio = original.Gear5Ratio;
		Gear6Ratio = original.Gear6Ratio;
		Gear7Ratio = original.Gear7Ratio;
		Gear8Ratio = original.Gear8Ratio;
		tier = original.tier;
	}
	public override PartSavable Clone()
	{
		return new GearboxSavable(this);
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		shiftDelaySeconds = vp.engine.transmission.shiftDelaySeconds;
		var gearStructs = vp.engine.transmission.Gears;
		int gears = vp.engine.transmission.Gears.Length - 2; // without R and N
		reverseGearRatio = gearStructs[0].ratio;
		Gear1Ratio = gearStructs[2].ratio;
		if (gears >= 2)
			Gear2Ratio = gearStructs[3].ratio;
		if (gears >= 3)
			Gear3Ratio = gearStructs[4].ratio;
		if (gears >= 4)
			Gear4Ratio = gearStructs[5].ratio;
		if (gears >= 5)
			Gear5Ratio = gearStructs[6].ratio;
		if (gears >= 6)
			Gear6Ratio = gearStructs[7].ratio;
		if (gears >= 7)
			Gear7Ratio = gearStructs[8].ratio;
		if (gears >= 8)
			Gear8Ratio = gearStructs[9].ratio;
	}
	public override void Apply(VehicleParent vp)
	{
		vp.engine.transmission.shiftDelaySeconds = shiftDelaySeconds;
		int gears;
		if (Gear8Ratio != 0)
			gears = 8;
		else if (Gear7Ratio != 0)
			gears = 7;
		else if (Gear6Ratio != 0)
			gears = 6;
		else if (Gear5Ratio != 0)
			gears = 5;
		else if (Gear4Ratio != 0)
			gears = 4;
		else if (Gear3Ratio != 0)
			gears = 3;
		else if (Gear2Ratio != 0)
			gears = 2;
		else
			gears = 1;
		Gear[] gearStructs = new Gear[gears + 2];
		for(int i=0; i<gears+2; ++i)
		{
			gearStructs[i] = new Gear(0);
		}
		gearStructs[0].ratio = reverseGearRatio;
		gearStructs[1].ratio = 0;
		gearStructs[2].ratio = Gear1Ratio; // 1st gear
		if (gears >= 2)
			gearStructs[3].ratio = Gear2Ratio;
		if (gears >= 3)
			gearStructs[4].ratio = Gear3Ratio;
		if (gears >= 4)
			gearStructs[5].ratio = Gear4Ratio;
		if (gears >= 5)
			gearStructs[6].ratio = Gear5Ratio;
		if (gears >= 6)
			gearStructs[7].ratio = Gear6Ratio;
		if (gears >= 7)
			gearStructs[8].ratio = Gear7Ratio;
		if (gears >= 8)
			gearStructs[9].ratio = Gear8Ratio;

		vp.engine.transmission.Gears = gearStructs;
	}
}
[Serializable]
public class ChassisSavable : PartSavable
{
	public float mass;
	public float longtitunalCOM;
	public float verticalCOM;
	public float drag;
	public float angularDrag;
	public float evoSmoothTime;
	public float staticEvoMaxSpeed;
	public float evoAcceleration;
	public float dragsterEffect;
	public ChassisSavable()
	{
	}
	public ChassisSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	ChassisSavable(ChassisSavable original)
	{
		mass = original.mass;
		longtitunalCOM = original.longtitunalCOM;
		verticalCOM = original.verticalCOM;
		drag = original.drag;
		angularDrag = original.angularDrag;
		evoSmoothTime = original.evoSmoothTime;
		staticEvoMaxSpeed = original.staticEvoMaxSpeed;
		evoAcceleration = original.evoAcceleration;
		dragsterEffect = original.dragsterEffect;
		tier = original.tier;
	}
	public override PartSavable Clone()
	{
		return new ChassisSavable(this);
	}
	public override void Apply(VehicleParent vp)
	{
		vp.originalMass = mass;
		vp.rb.mass = mass;
		vp.originalDrag = drag;
		vp.rb.drag = drag;
		vp.centerOfMassObj.localPosition = new Vector3(0, verticalCOM,longtitunalCOM);
		vp.SetCenterOfMass();
		vp.rb.angularDrag = angularDrag;
		vp.raceBox.evoModule.SetStuntCoeffs(evoSmoothTime, staticEvoMaxSpeed, evoAcceleration);
		vp.GetComponent<SGP_DragsterEffect>().COM_Movement = -dragsterEffect;
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		mass = vp.originalMass;
		drag = vp.originalDrag;
		longtitunalCOM = vp.centerOfMassObj.localPosition.z;
		verticalCOM = vp.centerOfMassObj.localPosition.y;
		angularDrag = vp.rb.angularDrag;
		vp.raceBox.evoModule.GetStuntCoeffs(ref evoSmoothTime, ref staticEvoMaxSpeed, ref evoAcceleration);
		dragsterEffect = -vp.GetComponent<SGP_DragsterEffect>().COM_Movement;
	}
}
[Serializable]
public class EngineSavable : PartSavable
{
	public float audioMaxPitch;
	public float audioMinPitch;
	public float fuelConsumption;
	public float audioType;
	public float inertia;
	public float torque;
	public float torqueCurveType;
	public float redlineKRPM;
	public float cutoffKRPM;//10 vars
	public EngineSavable()
	{
	}
	public EngineSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	EngineSavable(EngineSavable original)
	{
		fuelConsumption = original.fuelConsumption;
		audioMaxPitch = original.audioMaxPitch;
		audioMinPitch = original.audioMinPitch;
		audioType = original.audioType;
		inertia = original.inertia;
		torque = original.torque;
		torqueCurveType = original.torqueCurveType;
		redlineKRPM = original.redlineKRPM;
		cutoffKRPM = original.cutoffKRPM;
		tier = original.tier;
	}
	public override PartSavable Clone()
	{
		return new EngineSavable(this);
	}
	public override void Apply(VehicleParent vp)
	{
		vp.engine.fuelConsumption = fuelConsumption;
		vp.engine.maxPitch = audioMaxPitch;
		vp.engine.minPitch = audioMinPitch;
		vp.engine.inertia = inertia;
		vp.engine.maxTorque = torque;
		vp.engine.limitkRPM = redlineKRPM;
		vp.engine.limit2kRPM = cutoffKRPM;
		vp.engine.torqueCurve = vp.engine.GenerateTorqueCurve((int)torqueCurveType);
		vp.engine.SetEngineAudioClip((int)audioType);
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		fuelConsumption = vp.engine.fuelConsumption;
		audioMaxPitch = vp.engine.maxPitch;
		audioMinPitch = vp.engine.minPitch;
		inertia = vp.engine.inertia;
		torque = vp.engine.maxTorque;
		redlineKRPM = vp.engine.limitkRPM;
		cutoffKRPM = vp.engine.limit2kRPM;
		vp.engine.torqueCurve = vp.engine.GenerateTorqueCurve((int)torqueCurveType);
		vp.engine.SetEngineAudioClip((int)audioType);
	}
}
[Serializable]
public class BatterySavable : PartSavable
{
	public float capacity;
	public float chargingSpeed;
	public float lowBatPercent;
	public float evoBountyPercent;
	public BatterySavable()
	{
	}
	public BatterySavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	BatterySavable(BatterySavable original)
	{
		capacity = original.capacity;
		chargingSpeed = original.chargingSpeed;
		lowBatPercent = original.lowBatPercent;
		evoBountyPercent = original.evoBountyPercent;
		tier = original.tier;
	}
	public override PartSavable Clone()
	{
		return new BatterySavable(this);
	}
	public override void Apply(VehicleParent vp)
	{
		vp.SetBattery(capacity, chargingSpeed, lowBatPercent, evoBountyPercent);
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		capacity = vp.GetBatteryCapacity();
		chargingSpeed = vp.batteryChargingSpeed;
		lowBatPercent = vp.lowBatteryLevel;
		evoBountyPercent = vp.batteryStuntIncreasePercent;
	}
}
[Serializable]
public class BmsSavable : PartSavable
{
	public float driftSpinAssist;
	public float driftSpinSpeed;
	public float driftSpinExponent;
	public float maxDriftAngle;
	public float autoSteerDrift;
	public float driftPush;
	public float downforce;
	public float frontBrakeForce;
	public float rearBrakeForce;
	public BmsSavable()
	{
	}
	public BmsSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	BmsSavable(BmsSavable original)
	{
		driftSpinAssist = original.driftSpinAssist;
		driftSpinSpeed = original.driftSpinSpeed;
		driftSpinExponent = original.driftSpinExponent;
		maxDriftAngle = original.maxDriftAngle;
		autoSteerDrift = original.autoSteerDrift;
		driftPush = original.driftPush;
		downforce = original.downforce;
		frontBrakeForce = original.frontBrakeForce;
		rearBrakeForce = original.rearBrakeForce;
		tier = original.tier;
	}
	public override PartSavable Clone()
	{
		return new BmsSavable(this);
	}
	public override void Apply(VehicleParent vp)
	{
		var bms = vp.transform.GetComponent<VehicleAssist>();
		bms.driftSpinAssist = driftSpinAssist;
		bms.driftSpinSpeed = driftSpinSpeed;
		bms.driftSpinExponent = driftSpinExponent;
		bms.maxDriftAngle = maxDriftAngle;
		bms.autoSteerDrift = autoSteerDrift == 1;
		bms.driftPush = driftPush;
		bms.downforce = downforce;
		vp.wheels[0].suspensionParent.brakeForce = frontBrakeForce;
		vp.wheels[1].suspensionParent.brakeForce = frontBrakeForce;
		vp.wheels[2].suspensionParent.brakeForce = rearBrakeForce;
		vp.wheels[3].suspensionParent.brakeForce = rearBrakeForce;
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		var bms = vp.transform.GetComponent<VehicleAssist>();
		driftSpinAssist = bms.driftSpinAssist;
		driftSpinSpeed = bms.driftSpinSpeed;
		driftSpinExponent = bms.driftSpinExponent;
		maxDriftAngle = bms.maxDriftAngle;
		autoSteerDrift = bms.autoSteerDrift ? 1 : 0;
		driftPush = bms.driftPush;
		downforce = bms.downforce;
		frontBrakeForce = vp.wheels[0].suspensionParent.brakeForce;
		rearBrakeForce = vp.wheels[3].suspensionParent.brakeForce;
	}
}
[Serializable]
public class SuspensionSavable : PartSavable
{
	//front
	public float frontSteerRangeDegs;
	public float frontSpringDistance;
	public float frontSpringForce;
	public float frontSpringExponent;
	public float frontSpringDampening;
	public float frontExtendSpeed;
	//rear
	public float RearSteerRangeDegs;
	public float RearSpringDistance;
	public float RearSpringForce;
	public float RearSpringExponent;
	public float RearSpringDampening;
	public float RearExtendSpeed;
	public SuspensionSavable()
	{
	}
	public SuspensionSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	SuspensionSavable(SuspensionSavable original)
	{
		frontSteerRangeDegs = original.frontSteerRangeDegs;
		frontSpringDistance = original.frontSpringDistance;
		frontSpringForce = original.frontSpringForce;
		frontSpringExponent = original.frontSpringExponent;
		frontSpringDampening = original.frontSpringDampening;
		frontExtendSpeed = original.frontExtendSpeed;
		RearSpringDistance = original.RearSpringDistance;
		RearSpringForce = original.RearSpringForce;
		RearSpringExponent = original.RearSpringExponent;
		RearSpringDampening = original.RearSpringDampening;
		RearExtendSpeed = original.RearExtendSpeed;
	}
	public override PartSavable Clone()
	{
		return new SuspensionSavable(this);
	}

	public override void Apply(VehicleParent vp)
	{
		int i = 0;
		foreach (var w in vp.wheels)
		{
			if (i < 2)
			{
				w.suspensionParent.steerRangeMax = frontSteerRangeDegs;
				w.suspensionParent.steerRangeMin = -frontSteerRangeDegs;
				w.suspensionParent.suspensionDistance = frontSpringDistance;
				w.suspensionParent.springForce = frontSpringForce;
				w.suspensionParent.springExponent = frontSpringExponent;
				w.suspensionParent.springDampening = frontSpringDampening;
				w.suspensionParent.extendSpeed = frontExtendSpeed;
			}
			else
			{
				w.suspensionParent.suspensionDistance = RearSpringDistance;
				w.suspensionParent.springForce = RearSpringForce;
				w.suspensionParent.springExponent = RearSpringExponent;
				w.suspensionParent.springDampening = RearSpringDampening;
				w.suspensionParent.extendSpeed = RearExtendSpeed;
			}
			i++;
		}
	}



	public override void InitializeFromCar(VehicleParent vp)
	{
		frontSteerRangeDegs = vp.wheels[0].suspensionParent.steerRangeMax;
		frontSpringDistance = vp.wheels[0].suspensionParent.suspensionDistance;
		frontSpringForce = vp.wheels[0].suspensionParent.springForce;
		frontSpringExponent = vp.wheels[0].suspensionParent.springExponent;
		frontSpringDampening = vp.wheels[0].suspensionParent.springDampening;
		frontExtendSpeed = vp.wheels[0].suspensionParent.extendSpeed;

		RearSpringDistance = vp.wheels[3].suspensionParent.suspensionDistance;
		RearSpringForce = vp.wheels[3].suspensionParent.springForce;
		RearSpringExponent = vp.wheels[3].suspensionParent.springExponent;
		RearSpringDampening = vp.wheels[3].suspensionParent.springDampening;
		RearExtendSpeed = vp.wheels[3].suspensionParent.extendSpeed;
	}
}

