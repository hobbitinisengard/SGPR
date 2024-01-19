using Newtonsoft.Json;
using RVP;
using SFB;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
public enum PartType
{
	Suspension,
	Bms,
	Battery,
	Gears, 
	Chassis,
	Engine,
	Boost,
	Tyre,
	Drive,
	Honk,
	None
}
public class ComponentPanel : MonoBehaviour
{
	public GameObject settersPanel;
	public GameObject mainMenu;
	public AudioMixerSnapshot paused;
	public AudioMixerSnapshot unPaused;
	/// <summary>
	/// shows component name or carConfig name if no component is selected
	/// </summary>
	public TextMeshProUGUI bottomNameText;
	public RaceManager raceManager;
	public GameObject YouSurePanel;
	PartType selectedPart;
	CarConfig carConfig;
	private void OnEnable()
	{
		paused.TransitionTo(0);
		Time.timeScale = 0;
		Info.gamePaused = true;
		if (carConfig == null)
			NewSetupButton();
	}
	void OnDisable()
	{
		unPaused.TransitionTo(0);
		Time.timeScale = 1;
		Info.gamePaused = false;
	}
	private void Awake()
	{
		raceManager.ClosingRace += Reset;
	}
	public void Reset()
	{
		carConfig = null;
		bottomNameText.text = "-";
	}
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			BackToComponentMenu();
	}
	public void NewSetupButton()
	{
		carConfig = new CarConfig(raceManager.hud.vp);
		YouSurePanel.SetActive(false);
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
		else if (filepath.EndsWith("hnkcfg"))
			part = JsonConvert.DeserializeObject<HonkSavable>(jsonText);
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
		bottomNameText.text = carConfig.GetPartName(selectedPart);

		PopulatePropertyTable();
	}

	public void LoadFromFile()
	{
		string[] extensions = Info.partInfos.Select(i => i.fileExtension).ToArray();
		var extensionFilter = new[] {
			 new ExtensionFilter("SGPR car parts configuration files", extensions)};
		string filepath = StandaloneFileBrowser.OpenFilePanel("Select configuration file..",
				Info.partsPath, extensionFilter, false)[0];

		if (filepath.Length > 0)
		{
			if (filepath.EndsWith("carcfg"))
			{
				// Load carcfg menu
				if (filepath.Length > 0)
				{
					string jsonText = File.ReadAllText(filepath);
					bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
					carConfig = new CarConfig(raceManager.hud.vp, bottomNameText.text, jsonText);

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
		{ // dropdown has only external parts.
			string[] files = Directory.GetFiles(
				Info.partsPath, "*." + Info.partInfos[i].fileExtension);

			int choiceIdx = -1;
			string selectedPartName = carConfig.GetPartName((PartType)i);
			for (int j = 0; j < files.Length; ++j)
			{
				files[j] = Path.GetFileNameWithoutExtension(files[j]);
				if (choiceIdx == -1 && selectedPartName.Contains(files[j]))
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
		if (mainMenu.activeSelf)
			return;
		Info.ReloadCarPartsData();
		PopulateCarConfigTable();
		bottomNameText.text = (carConfig.Modified ? "*" : "") + carConfig.name;
		settersPanel.SetActive(false);
		mainMenu.SetActive(true);

		for (int i = 0; i < settersPanel.transform.childCount; ++i)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
	}
	public async void SaveConfig()
	{
		if (mainMenu.activeSelf)
		{ // saving car configuration
			string filepath = StandaloneFileBrowser.SaveFilePanel("Save car config file..",
				Info.partsPath, carConfig.name, CarConfig.extension);
			if (filepath != null && filepath.Length > 3)
			{
				await Task.Run(() => 
				{
					carConfig.PrepareForSave();
					string serializedJson = JsonConvert.SerializeObject(carConfig, Formatting.Indented);
					File.WriteAllText(filepath, serializedJson);
				});
				bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
				carConfig.name = bottomNameText.text;
				if (bottomNameText.text.Contains("car"))
					Info.ReloadCarConfigs();
			}
		}
		else
		{ // saving part
			string filepath = StandaloneFileBrowser.SaveFilePanel("Save part file..",
				Info.partsPath, carConfig.GetPartName(selectedPart), Info.partInfos[(int)selectedPart].fileExtension);
			if (filepath != null && filepath.Length > 3)
			{
				await Task.Run(() =>
				{
					var curPart = carConfig.GetPart(selectedPart);
					string serializedJson = JsonConvert.SerializeObject(curPart, Formatting.Indented);
					File.WriteAllText(filepath, serializedJson);
					if (!File.Exists(filepath))
						Info.carParts.Add(bottomNameText.text, curPart);
				});
				bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
				carConfig.SetPartTo(selectedPart, bottomNameText.text);
			}
		}
		
	}
	void EditCarConfigCallback(PartType newPartType, string partName)
	{
		if (carConfig != null)
		{
			if (!carConfig.Modified)
			{
				bottomNameText.text = "*" + bottomNameText.text;
			}
			selectedPart = newPartType;
			carConfig.SetPartTo(newPartType, partName);
		}
	}
	void EditPartCallback()
	{
		if (bottomNameText.text[0] != '*')
		{
			bottomNameText.text = "*" + bottomNameText.text;
			carConfig.MarkModified(selectedPart);
		}
		var curPart = carConfig.GetPart(selectedPart);
		var fields = curPart.GetType().GetFields();
		int j = 0;
		foreach (FieldInfo field in fields)
		{
			field.SetValue(curPart, settersPanel.transform.GetChild(j).GetComponent<PropertySetter>().value);
			j++;
		}
		curPart.Apply(raceManager.hud.vp);
	}
}
[Serializable]
public class PartsStruct
{
	public SuspensionSavable sus;
	public BmsSavable bms;
	public BatterySavable battery;
	public GearboxSavable gearbox;
	public EngineSavable engine;
	public ChassisSavable chassis;
	public BoostSavable boost;
	public TyreSavable tyre;
	public DriveSavable drive;
	public HonkSavable honk;
	public PartSavable this[PartType key]
	{
		get
		{
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
				PartType.Honk => honk,
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
				case PartType.Honk: //-
					honk = (HonkSavable)value;
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
	[JsonIgnore]
	bool setAnyOtherCarPart;
	[JsonIgnore]
	public bool Modified
	{
		get
		{
			return setAnyOtherCarPart || modifiedParts.Any(p => p);
		}
	}
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
					tier = 1 + Info.carParts[externalParts[(int)type]].tier;
				else
					tier = 1 + customParts[type].tier;
				return Mathf.Clamp(tier, 1, 5);
			}
			int maxTier = 5;
			float S = (Tier(PartType.Suspension) + Tier(PartType.Bms) + Tier(PartType.Chassis)) / (3 * maxTier);
			// PartType.Drive has only 3 tiers -> scale it to 5
			float G = (Tier(PartType.Suspension) + Tier(PartType.Bms) + 3 * Tier(PartType.Tyre) + 2 * 5 / 3f * Tier(PartType.Drive)) / (7 * maxTier);
			float P = (Tier(PartType.Battery) + 2 * Tier(PartType.Engine) + 2 * Tier(PartType.Gears) + Tier(PartType.Boost)) / (6 * maxTier);
			return new float[] { S, G, P };
		}
	}
	[NonSerialized]
	public static readonly string extension = "carcfg";
	[NonSerialized]
	bool[] modifiedParts = new bool[10];
	[SerializeField]
	string[] externalParts;
	[SerializeField]
	PartsStruct customParts;
	/// <summary>
	/// Initialize from Json (json.net)
	/// </summary>
	public CarConfig()
	{ }
	/// <summary>
	/// Initialize from car's config
	/// </summary>
	/// <param name="vp"></param>
	public CarConfig(VehicleParent vp)
	{
		this.vp = vp;
		CarConfig original = vp.carConfig;
		name = "car"+vp.carNumber.ToString();
		externalParts = new string[original.externalParts.Length];
		for(int i=0; i< original.externalParts.Length; i++)
		{
			externalParts[i] = original.externalParts[i];
		}
		customParts = new PartsStruct();
		customParts[PartType.Suspension] = new SuspensionSavable((SuspensionSavable)original.customParts[PartType.Suspension]);
		customParts[PartType.Bms] = new BmsSavable((BmsSavable)original.customParts[PartType.Bms]);
		customParts[PartType.Battery] = new BatterySavable((BatterySavable)original.customParts[PartType.Battery]);
		customParts[PartType.Engine] = new EngineSavable((EngineSavable)original.customParts[PartType.Engine]);
		customParts[PartType.Gears] = new GearboxSavable((GearboxSavable)original.customParts[PartType.Gears]);
		customParts[PartType.Chassis] = new ChassisSavable((ChassisSavable)original.customParts[PartType.Chassis]);
		customParts[PartType.Boost] = new BoostSavable((BoostSavable)original.customParts[PartType.Boost]);
		customParts[PartType.Tyre] = new TyreSavable((TyreSavable)original.customParts[PartType.Tyre]);
		customParts[PartType.Drive] = new DriveSavable((DriveSavable)original.customParts[PartType.Drive]);
		customParts[PartType.Honk] = new HonkSavable((HonkSavable)original.customParts[PartType.Honk]);
		Apply();
	}
	public CarConfig(VehicleParent vp, string name, string jsonText)
	{
		this.vp = vp;
		this.name = name;
		var data = JsonConvert.DeserializeObject<CarConfig>(jsonText);
		externalParts = data.externalParts;
		customParts = data.customParts;
		Apply();
	}
	public void Apply(VehicleParent vp = null)
	{
		if (vp == null)
			vp = this.vp;

		if (vp)
		{
			for (int i = 0; i < externalParts.Length; ++i)
			{
				GetPart((PartType)i).Apply(vp);
			}
		}
	}
	public PartSavable GetPart(PartType type)
	{
		if (customParts[type] == null)
		{
			customParts[type] = Info.carParts[externalParts[(int)type]].Clone();
		}
		return customParts[type];
	}
	/// <param name="partName">set to null, to set as custom</param>
	public void SetPartTo(PartType type, string partName)
	{
		if (partName == null)
		{
			if (externalParts[(int)type] != null)
			{// set part as custom
				setAnyOtherCarPart = true;
				customParts[type] = Info.carParts[externalParts[(int)type]].Clone();
				externalParts[(int)type] = null;
				customParts[type].Apply(vp);
			}
		}
		else
		{  // set external part
			setAnyOtherCarPart = true;
			modifiedParts[(int)type] = false;
			externalParts[(int)type] = partName;
			customParts[type] = null;
			Info.carParts[externalParts[(int)type]].Apply(vp);
		}
	}
	public string GetPartName(PartType type)
	{
		if (customParts[type] != null)
		{
			if (externalParts[(int)type] != null)
				return (modifiedParts[(int)type] ? "*" : "") + externalParts[(int)type];
			else
				return (modifiedParts[(int)type] ? "*" : "") + "Custom";
		}
		else
		{
			if (externalParts[(int)type] == null)
				Debug.LogError("both custom and external don't exist");
			return externalParts[(int)type];
		}
	}
	public bool IsPartModified(PartType curPartType)
	{
		return externalParts[(int)curPartType] != null && customParts[curPartType] != null;
	}

	internal void PrepareForSave()
	{
		for (int i = 0; i < externalParts.Length; ++i)
		{
			if (IsPartModified((PartType)i))
				externalParts[i] = null;
			modifiedParts[i] = false;
		}
		setAnyOtherCarPart = false;
	}

	public void MarkModified(PartType selectedPart)
	{
		modifiedParts[(int)selectedPart] = true;
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
public class HonkSavable : PartSavable
{
	public float honkType;
	public HonkSavable()
	{
	}
	public override void Apply(VehicleParent vp)
	{
		vp.SetHonkerAudio((int)honkType);
	}
	public HonkSavable(HonkSavable original)
	{
		tier = original.tier;
		honkType = original.honkType;
	}
	public override PartSavable Clone()
	{
		return new HonkSavable(this);
	}

	public override void InitializeFromCar(VehicleParent vp)
	{
		float.TryParse(vp.honkerAudio.clip.name[^1..], out honkType);
	}
}
[Serializable]
public class DriveSavable : PartSavable
{
	// tier 0=FWD, 1=RWD, 2=AWD
	public float steerAdd;
	public float holdComebackSpeed;
	public float steerLimitAt0;
	public float steerLimitAt200;
	public float steerComebackAt0;
	public float steerComebackAt200;
	public DriveSavable()
	{
	}
	public DriveSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	public DriveSavable(DriveSavable original)
	{
		tier = original.tier;
		steerAdd = original.steerAdd;
		holdComebackSpeed = original.holdComebackSpeed;
		steerLimitAt0 = original.steerLimitAt0;
		steerLimitAt200 = original.steerLimitAt200;
		steerComebackAt0 = original.steerComebackAt0;
		steerComebackAt200 = original.steerComebackAt200;
	}
	public override PartSavable Clone()
	{
		return new DriveSavable(this);
	}

	public override void InitializeFromCar(VehicleParent vp)
	{
		tier = (int)vp.engine.transmission.Drive;
		steerAdd = vp.steeringControl.steerAdd;
		holdComebackSpeed = vp.steeringControl.holdComebackSpeed;
		steerLimitAt0 = vp.steeringControl.steerLimitCurve.keys[0].value;
		steerLimitAt200 = vp.steeringControl.steerLimitCurve.keys[1].value;
		steerComebackAt0 = vp.steeringControl.steerComebackCurve.keys[0].value;
		steerComebackAt200 = vp.steeringControl.steerComebackCurve.keys[1].value;
	}
	public override void Apply(VehicleParent vp)
	{
		if (vp.wheels[0].tireWidth < vp.wheels[2].tireWidth)
			vp.engine.transmission.Drive = GearboxTransmission.DriveType.RWD;
		else
			vp.engine.transmission.Drive = (GearboxTransmission.DriveType)tier;

		vp.steeringControl.steerAdd = steerAdd;
		vp.steeringControl.holdComebackSpeed = holdComebackSpeed;
		vp.steeringControl.steerLimitCurve = AnimationCurve.Linear(0, steerLimitAt0, 56, steerLimitAt200);
		vp.steeringControl.steerComebackCurve = AnimationCurve.Linear(0, steerComebackAt0, 56, steerComebackAt200);
	}
}
[Serializable]
public class TyreSavable : PartSavable
{
	public float frontFriction;
	public float rearFriction;
	public float frontFrictionStretch;
	public float rearFrictionStretch;
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
	public TyreSavable(TyreSavable original)
	{
		tier = original.tier;
		frontFriction = original.frontFriction;
		rearFriction = original.rearFriction;
		frontFrictionStretch = original.frontFrictionStretch;
		rearFrictionStretch = original.rearFrictionStretch;
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
		var rear = vp.wheels[2];
		var front = vp.wheels[0];
		frontFriction = front.sidewaysFriction;
		rearFriction = rear.sidewaysFriction;
		frontFrictionStretch = front.sidewaysCurveStretch;
		rearFrictionStretch = rear.sidewaysCurveStretch;
		squeakSlipThreshold = rear.slipThreshold;
		slipDependence = (rear.slipDependence == Wheel.SlipDependenceMode.forward) ? 0 : 1;
		axleFriction = rear.axleFriction;
	}

	public override void Apply(VehicleParent vp)
	{
		vp.steeringControl.frontSidewaysCoeff = frontFriction;
		for (int i = 0; i < 4; ++i)
		{
			var w = vp.wheels[i];

			if(i< 2)
				w.SetInitFrictions(frontFriction, frontFriction);
			else
			{
				if(Info.s_raceType == Info.RaceType.Drift)
					w.SetInitFrictions(frontFriction-2, frontFriction - 2);
				else
					w.SetInitFrictions(rearFriction, rearFriction);
			}
			w.forwardCurveStretch = (i < 2) ? frontFrictionStretch : rearFrictionStretch;
			w.sidewaysCurveStretch = (i < 2) ? frontFrictionStretch : rearFrictionStretch;
			w.slipThreshold = squeakSlipThreshold;
			w.slipDependence = (int)slipDependence == 0 ? Wheel.SlipDependenceMode.forward : Wheel.SlipDependenceMode.independent;
			w.axleFriction = axleFriction;
			// update materials
			//var mr = w.transform.GetChild(0).GetComponent<MeshRenderer>();
			//// [..^1] = from beginning to last - 1. |         tier+1 cause tyres are named from 1
			//string name = mr.sharedMaterials[0].name[..^1] + (tier+1).ToString();
			//Material tyreMat = Resources.Load<Material>("materials/" + name);
			//Material[] mats = mr.materials;
			//mats[0] = tyreMat;
			//mr.materials = mats;
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
	public BoostSavable(BoostSavable original)
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
	public GearboxSavable(GearboxSavable original)
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
		for (int i = 0; i < gears + 2; ++i)
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
		vp.engine.transmission.skipNeutral = tier > 1;
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
	public ChassisSavable(ChassisSavable original)
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

		if(Info.s_raceType == Info.RaceType.Drift)
			vp.centerOfMassObj.localPosition = new Vector3(0, verticalCOM, -0.05f);
		else
			vp.centerOfMassObj.localPosition = new Vector3(0, verticalCOM, longtitunalCOM);

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
	public EngineSavable(EngineSavable original)
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
	public BatterySavable(BatterySavable original)
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
		capacity = vp.batteryCapacity;
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
	public BmsSavable(BmsSavable original)
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
	public SuspensionSavable(SuspensionSavable original)
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

