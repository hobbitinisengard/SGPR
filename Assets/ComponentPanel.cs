using Newtonsoft.Json;
using RVP;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using SimpleFileBrowser;
using System.Collections;
using UnityEditor;
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
	VehicleParent vp;
	public GameObject settersPanel;
	public GameObject mainMenu;
	public AudioMixerSnapshot paused;
	public AudioMixerSnapshot unPaused;
	/// <summary>
	/// shows component name or carConfig name if no component is selected
	/// </summary>
	public TextMeshProUGUI bottomNameText;
	public GameObject YouSurePanel;
	PartType selectedPart;
	private void OnEnable()
	{
		F.I.escRef.action.performed += OnEscPressed;
		Cursor.visible = true;
		paused.TransitionTo(0);
		Time.timeScale = 0;
		F.I.gamePaused = true;
		if(vp == null)
			NewSetupButton();
	}

	void OnEscPressed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
	{
		BackToComponentMenu();
	}

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
		if (mainMenu.activeSelf)
		{
			bottomNameText.text = vp.carConfig.name;
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
		F.I.carParts.Add(partName, part);
	}
	public void OpenComponentConfigMenu(ConfigEnumSelector type)
	{
		mainMenu.SetActive(false);
		settersPanel.SetActive(true);

		// part can be custom or external
		selectedPart = type.componentType;
		bottomNameText.text = vp.carConfig.GetPartName(selectedPart);

		PopulatePropertyTable();
	}

	IEnumerator ShowLoadDialogCoroutine()
	{
		// Show a load file dialog and wait for a response from user
		// Load file/folder: file, Allow multiple selection: true
		// Initial path: default (Documents), Initial filename: empty
		// Title: "Load File", Submit button text: "Load"
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, F.I.partsPath, null, "Select configuration file..", "Load");

		// Dialog is closed
		Debug.Log(FileBrowser.Success); // (FileBrowser.Success) - whether the user has selected some files or cancelled the operation 

		if (FileBrowser.Success)
			LoadFromFile(FileBrowser.Result);
	}

	public void LoadFromFile()
	{
		if (!FileBrowser.IsOpen)
		{
			string[] extensions = F.I.partInfos.Select(i => i.fileExtension).ToArray();
			var extensionFilter = new[] {
			 new FileBrowser.Filter("SGPR car parts configuration files", extensions)};
			FileBrowser.SetFilters(true, extensionFilter);
			StartCoroutine(ShowLoadDialogCoroutine());
		}
	}
	public void LoadFromFile(string[] filepaths)
	{
		string filepath = filepaths[0];
		if (filepath.Length > 0)
		{
			if (filepath.EndsWith("carcfg"))
			{
				// Load carcfg menu
				if (filepath.Length > 0)
				{
					string jsonText = File.ReadAllText(filepath);
					bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
					vp.carConfig = new CarConfig(bottomNameText.text, jsonText);
					vp.carConfig.Apply();
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
					for (int i = 0; i < F.I.partInfos.Length - 1; ++i)
					{
						if (filepath.EndsWith(F.I.partInfos[i].fileExtension))
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
					vp.carConfig.SetPartTo(selectedPart, bottomNameText.text);
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
				F.I.partsPath, "*." + F.I.partInfos[i].fileExtension);

			int choiceIdx = -1;
			string selectedPartName = vp.carConfig.GetPartName((PartType)i);
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
		PartSavable curPart = vp.carConfig.GetPartReadonly(selectedPart);
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
		F.I.ReloadCarPartsData();
		PopulateCarConfigTable();
		bottomNameText.text = (vp.carConfig.Modified ? "*" : "") + vp.carConfig.name;
		settersPanel.SetActive(false);
		mainMenu.SetActive(true);

		for (int i = 0; i < settersPanel.transform.childCount; ++i)
			Destroy(settersPanel.transform.GetChild(i).gameObject);
	}
	public void SaveConfig()
	{
		StartCoroutine(SaveConfigCo());
	}
	IEnumerator SaveConfigCo()
	{
		if (!FileBrowser.IsOpen)
		{
			if (mainMenu.activeSelf)
			{ // saving car configuration
				var extensionFilter = new[] { new FileBrowser.Filter("SGPR car config file", CarConfig.extension) };
				FileBrowser.SetFilters(false, extensionFilter);

				yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, F.I.partsPath, vp.carConfig.name, "Save car config file..", "Save");

				if (FileBrowser.Success)
				{
					string filepath = FileBrowser.Result[0];
					if (filepath.Length > 3)
					{
						vp.carConfig.PrepareForSave();
						string serializedJson = JsonConvert.SerializeObject(vp.carConfig, Formatting.Indented);
						File.WriteAllText(filepath, serializedJson);

						bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
						vp.carConfig.name = bottomNameText.text;
						if (bottomNameText.text.Contains("car"))
							F.I.ReloadCarConfigs();
					}
				}
			}
			else
			{ // saving part
				var extensionFilter = new[] { new FileBrowser.Filter("SGPR Car part", F.I.partInfos[(int)selectedPart].fileExtension) };
				FileBrowser.SetFilters(false, extensionFilter);

				yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, F.I.partsPath, vp.carConfig.GetPartName(selectedPart), "Save part file..", "Save");
				if (FileBrowser.Success)
				{
					string filepath = FileBrowser.Result[0];
					if (filepath.Length > 3)
					{
						var curPart = vp.carConfig.GetPart(selectedPart);
						string serializedJson = JsonConvert.SerializeObject(curPart, Formatting.Indented);
						File.WriteAllText(filepath, serializedJson);
						if (!File.Exists(filepath))
							F.I.carParts.Add(bottomNameText.text, curPart);

						bottomNameText.text = Path.GetFileNameWithoutExtension(filepath);
						vp.carConfig.SetPartTo(selectedPart, bottomNameText.text);
					}
				}
			}
		}
	}
	void EditCarConfigCallback(PartType newPartType, string partName)
	{
		if (vp.carConfig != null)
		{
			if (!vp.carConfig.Modified)
			{
				bottomNameText.text = "*" + bottomNameText.text;
			}
			selectedPart = newPartType;
			vp.carConfig.SetPartTo(newPartType, partName);
		}
	}
	void EditPartCallback()
	{
		if (bottomNameText.text[0] != '*')
		{
			bottomNameText.text = "*" + bottomNameText.text;
			vp.carConfig.MarkModified(selectedPart);
		}
		var curPart = vp.carConfig.GetPart(selectedPart);
		var fields = curPart.GetType().GetFields();
		int j = 0;
		foreach (FieldInfo field in fields)
		{
			field.SetValue(curPart, settersPanel.transform.GetChild(j).GetComponent<PropertySetter>().value);
			j++;
		}
		curPart.Apply(RaceManager.I.hud.vp);
	}
}
[Serializable]
public class PartsArray
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
				case PartType.Suspension:
					sus = (SuspensionSavable)value;
					break;
				case PartType.Bms:
					bms = (BmsSavable)value;
					break;
				case PartType.Battery:
					battery = (BatterySavable)value;
					break;
				case PartType.Engine:
					engine = (EngineSavable)value;
					break;
				case PartType.Chassis:
					chassis = (ChassisSavable)value;
					break;
				case PartType.Gears:
					gearbox = (GearboxSavable)value;
					break;
				case PartType.Boost:
					boost = (BoostSavable)value;
					break;
				case PartType.Tyre:
					tyre = (TyreSavable)value;
					break;
				case PartType.Drive:
					drive = (DriveSavable)value;
					break;
				case PartType.Honk:
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
	[JsonIgnore]
	bool carConfigModified;
	[JsonIgnore]
	public bool Modified
	{
		get
		{
			return carConfigModified || modifiedParts.Any(p => p);
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
			PartSavable Part(PartType type)
			{
				if (externalParts[(int)type] != null)
					return F.I.carParts[externalParts[(int)type]];
				else
					return customParts[type];
			}
			float C01(float min, float max, float val)
			{
				return Mathf.Clamp(Mathf.InverseLerp(min, max, val), .15f, 1);
			}

			var chassis = (ChassisSavable)Part(PartType.Chassis);
			var tyre = (TyreSavable)Part(PartType.Tyre);
			var engine = (EngineSavable)Part(PartType.Engine);
			float S = C01(400, 1200, chassis.staticEvoMaxSpeed);
			float G = C01(2, 0, (tyre.sideFriction - tyre.shiftRearFriction) / chassis.mass);
			float P = (C01(0.025f, .2f, engine.torque / chassis.mass) + C01(4,10,tyre.forwardFriction / chassis.mass))/2f;
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
	PartsArray customParts;
	public CarConfig()
	{}
	/// <summary>
	/// Initialize from car's config
	/// </summary>
	/// <param name="vp"></param>
	public CarConfig(CarConfig cc)
	{
		name = cc.name;
		externalParts = new string[10];
		for (int i = 0; i < cc.externalParts.Length; i++)
		{
			externalParts[i] = cc.externalParts[i];
		}
		customParts = new PartsArray();
		customParts[PartType.Suspension] = new SuspensionSavable((SuspensionSavable)cc.customParts[PartType.Suspension]);
		customParts[PartType.Bms] = new BmsSavable((BmsSavable)cc.customParts[PartType.Bms]);
		customParts[PartType.Battery] = new BatterySavable((BatterySavable)cc.customParts[PartType.Battery]);
		customParts[PartType.Engine] = new EngineSavable((EngineSavable)cc.customParts[PartType.Engine]);
		customParts[PartType.Gears] = new GearboxSavable((GearboxSavable)cc.customParts[PartType.Gears]);
		customParts[PartType.Chassis] = new ChassisSavable((ChassisSavable)cc.customParts[PartType.Chassis]);
		customParts[PartType.Boost] = new BoostSavable((BoostSavable)cc.customParts[PartType.Boost]);
		customParts[PartType.Tyre] = new TyreSavable((TyreSavable)cc.customParts[PartType.Tyre]);
		customParts[PartType.Drive] = new DriveSavable((DriveSavable)cc.customParts[PartType.Drive]);
		customParts[PartType.Honk] = new HonkSavable((HonkSavable)cc.customParts[PartType.Honk]);
		Apply();
	}
	public CarConfig(string name, string jsonText)
	{
		this.name = name;
		var data = JsonConvert.DeserializeObject<CarConfig>(jsonText);
		externalParts = data.externalParts;
		customParts = data.customParts;
		Apply();
	}
	public void Apply(VehicleParent vp = null)
	{
		if (vp)
		{
			for (int i = 0; i < externalParts.Length; ++i)
			{
				GetPart((PartType)i).Apply(vp);
			}
		}
	}
	/// <summary>
	/// Use this only to read it
	/// </summary>
	public PartSavable GetPartReadonly(PartType type)
	{
		if (customParts[type] == null)
			return F.I.carParts[externalParts[(int)type]];
		return customParts[type];
	}
	/// <summary>
	/// Use this if you want to read and modify it
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public PartSavable GetPart(PartType type)
	{
		if (customParts[type] == null)
			customParts[type] = F.I.carParts[externalParts[(int)type]].Clone();
		return customParts[type];
	}
	/// <param name="partName">set to null, to set as custom</param>
	public void SetPartTo(PartType type, string partName)
	{
		if (partName == null)
		{
			if (externalParts[(int)type] != null)
			{// set part as custom
				carConfigModified = true;
				customParts[type] = F.I.carParts[externalParts[(int)type]].Clone();
				externalParts[(int)type] = null;
			}
		}
		else
		{  // set external part
			carConfigModified = true;
			modifiedParts[(int)type] = false;
			externalParts[(int)type] = partName;
			customParts[type] = null;
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
		carConfigModified = false;
	}

	public void MarkModified(PartType selectedPart)
	{
		modifiedParts[(int)selectedPart] = true;
	}
}

[Serializable]
public abstract class PartSavable
{
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
	// tier 0=RWD, 1=FWD, 2=AWD
	public float driveType;
	public float steerAdd;
	public float holdComebackSpeed;
	public float steerLimitAt0;
	public float steerLimitAt200;
	public float steerLimitAt300;
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
		steerAdd = original.steerAdd;
		holdComebackSpeed = original.holdComebackSpeed;
		steerLimitAt0 = original.steerLimitAt0;
		steerLimitAt200 = original.steerLimitAt200;
		steerLimitAt300 = original.steerLimitAt300;
		steerComebackAt0 = original.steerComebackAt0;
		steerComebackAt200 = original.steerComebackAt200;
	}
	public override PartSavable Clone()
	{
		return new DriveSavable(this);
	}

	public override void InitializeFromCar(VehicleParent vp)
	{
		steerAdd = vp.steeringControl.steerAdd;
		holdComebackSpeed = vp.steeringControl.holdComebackSpeed;
		steerLimitAt0 = vp.steeringControl.steerLimitCurve.keys[0].value;
		steerLimitAt200 = vp.steeringControl.steerLimitCurve.keys[1].value;
		steerLimitAt300 = vp.steeringControl.steerLimitCurve.keys[2].value;
		
		steerComebackAt0 = vp.steeringControl.steerComebackCurve.keys[0].value;
		steerComebackAt200 = vp.steeringControl.steerComebackCurve.keys[1].value;
	}
	public override void Apply(VehicleParent vp)
	{
		if (vp.wheels[0].tireWidth < vp.wheels[2].tireWidth)
			vp.engine.transmission.Drive = GearboxTransmission.DriveType.RWD;
		else
			vp.engine.transmission.Drive = (GearboxTransmission.DriveType)driveType;

		vp.steeringControl.steerAdd = steerAdd;
		vp.steeringControl.holdComebackSpeed = holdComebackSpeed;

		//vp.steeringControl.steerLimitCurve = AnimationCurve.Linear(0, steerLimitAt0, 83, steerLimitAt200);
		vp.steeringControl.steerLimitCurve = new AnimationCurve(new Keyframe[] {
			new (0, steerLimitAt0, 0, -0.03f),
			new (56, steerLimitAt200, -0.0012f, 0),
			new (83, steerLimitAt300, 0, 0)
		});

		vp.steeringControl.steerComebackCurve = AnimationCurve.Linear(0, steerComebackAt0, 56, steerComebackAt200);
	}
}
[Serializable]
public class TyreSavable : PartSavable
{
	public float forwardFriction;
	public float sideFriction;
	public float frontFrictionStretch;
	public float rearFrictionStretch;
	public float shiftRearFriction;
	public float squeakSlipThreshold;
	public float slipDependence;
	public float axleFriction;
	public float offroadTread;
	public float driftRearFriction;
	public float driftRearFrictionInit;
	public float torqueThreshold;
	public TyreSavable()
	{
	}
	public TyreSavable(VehicleParent vp)
	{
		InitializeFromCar(vp);
	}
	public TyreSavable(TyreSavable original)
	{
		forwardFriction = original.forwardFriction;
		sideFriction = original.sideFriction;
		frontFrictionStretch = original.frontFrictionStretch;
		rearFrictionStretch = original.rearFrictionStretch;
		shiftRearFriction = original.shiftRearFriction;
		squeakSlipThreshold = original.squeakSlipThreshold;
		slipDependence = original.slipDependence;
		axleFriction = original.axleFriction;
		offroadTread = original.offroadTread;
		driftRearFriction = original.driftRearFriction;
		driftRearFrictionInit = original.driftRearFrictionInit;
		torqueThreshold = original.torqueThreshold;
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
		offroadTread = vp.tyresOffroad;
		forwardFriction = front.sidewaysFriction;
		sideFriction = rear.sidewaysFriction;
		frontFrictionStretch = front.sidewaysCurveStretch;
		rearFrictionStretch = rear.sidewaysCurveStretch;
		shiftRearFriction = vp.steeringControl.shiftRearFriction;
		driftRearFriction = vp.steeringControl.driftRearFriction;
		squeakSlipThreshold = rear.slipThreshold;
		slipDependence = 2;
		axleFriction = rear.axleFriction;
		torqueThreshold = vp.wheels[2].torqueThreshold;
	}

	public override void Apply(VehicleParent vp)
	{
		vp.tyresOffroad = offroadTread;
		vp.steeringControl.shiftRearFriction = shiftRearFriction;
		vp.steeringControl.driftRearFriction = driftRearFriction;
		for (int i = 0; i < 4; ++i)
		{
			var w = vp.wheels[i];

			w.SetInitFrictions(forwardFriction, (F.I.s_raceType == RaceType.Drift) ? driftRearFrictionInit : sideFriction, (i < 2) ? frontFrictionStretch : rearFrictionStretch);

			w.slipThreshold = squeakSlipThreshold;
			w.slipDependence = (F.I.s_raceType == RaceType.Drift) ? 
				Wheel.SlipDependenceMode.independent : Wheel.SlipDependenceMode.independent;
			w.axleFriction = axleFriction;
			w.torqueThreshold = torqueThreshold;
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
	}
	public override PartSavable Clone()
	{
		return new GearboxSavable(this);
	}
	public int NumberOfGears
	{
		get
		{
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
			return gears;
		}
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
		int gears = NumberOfGears;
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
		vp.engine.transmission.skipNeutral = shiftDelaySeconds > 0.5f;
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
	}
	public override PartSavable Clone()
	{
		return new ChassisSavable(this);
	}
	public override void Apply(VehicleParent vp)
	{
		vp.rb.centerOfMass = new Vector3(0, verticalCOM, (F.I.s_raceType == RaceType.Drift) ? 0 : longtitunalCOM);
		vp.SetChassis(mass, drag, angularDrag);
		vp.raceBox.evoModule.SetStuntCoeffs(evoSmoothTime, staticEvoMaxSpeed, evoAcceleration);
		vp.GetComponent<SGP_DragsterEffect>().COM_Movement = vp.followAI.isCPU ? 0 : -dragsterEffect;
	}
	public override void InitializeFromCar(VehicleParent vp)
	{
		mass = vp.originalMass;
		drag = vp.originalDrag;
		var com = vp.rb.centerOfMass;
		longtitunalCOM = com.z;
		verticalCOM = com.y;
		angularDrag = vp.AngularDrag;
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
	public float cutoffKRPM;
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
		vp.engine.maxTorque = (F.I.s_raceType == RaceType.Drift) ? Mathf.Max(torque, vp.originalMass/2f) : torque;
		vp.engine.limitkRPM = redlineKRPM;
		vp.engine.limit2kRPM = cutoffKRPM;
		vp.engine.torqueCurve = vp.engine.GenerateTorqueCurve((int)torqueCurveType);
		vp.engine.SetEngineAudioClip((int)audioType);
		vp.engine.GetMaxRPM();

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
		vp.engine.GetMaxRPM();
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
		bms.driftPush = (F.I.s_raceType == RaceType.Drift) ? Mathf.Max(driftPush, 1) : driftPush;
		bms.downforce = (F.I.s_raceType == RaceType.Drift) ? 0 : downforce;
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
	//rear
	public float RearSteerRangeDegs;
	public float RearSpringDistance;
	public float RearSpringForce;
	public float RearSpringExponent;
	public float RearSpringDampening;
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
		RearSpringDistance = original.RearSpringDistance;
		RearSpringForce = original.RearSpringForce;
		RearSpringExponent = original.RearSpringExponent;
		RearSpringDampening = original.RearSpringDampening;
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
			}
			else
			{
				w.suspensionParent.suspensionDistance = RearSpringDistance;
				w.suspensionParent.springForce = RearSpringForce;
				w.suspensionParent.springExponent = RearSpringExponent;
				w.suspensionParent.springDampening = RearSpringDampening;
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

		RearSpringDistance = vp.wheels[3].suspensionParent.suspensionDistance;
		RearSpringForce = vp.wheels[3].suspensionParent.springForce;
		RearSpringExponent = vp.wheels[3].suspensionParent.springExponent;
		RearSpringDampening = vp.wheels[3].suspensionParent.springDampening;
	}
}

