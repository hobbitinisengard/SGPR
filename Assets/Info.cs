using Newtonsoft.Json;
using RVP;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Unity.Multiplayer.Playmode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
public enum PlayerState { InRace, InLobbyUnready, InLobbyReady };
public enum Envir { GER, JAP, SPN, FRA, ENG, USA, ITA, MEX };
public enum CarGroup { Wild, Aero, Speed, Team };
public enum Livery { Random = 0, Team = 1, TGR = 2, Rline = 3, Itex = 4, Caltex = 5, Titan = 6, Mysuko = 7 }
public enum RecordType { BestLap, RaceTime, StuntScore, DriftScore }
public enum ScoringType { Championship, Points, Victory }
public enum ActionHappening { InLobby, InRace }
public enum PavementType { Arena, Volcano, Asphalt, Energy, Grid, Japan, Jungle, Random }
public enum GameMode { Exhibition, Multiplayer, Splitscreen, Arcade };
public enum RaceType { Race, Knockout, Stunt, Drift, TimeTrial }
public enum CpuLevel { Easy, Medium, Hard };
public enum TimeOfDay { Day, Night };
public enum Language { English, Polish };


[Serializable]
public class PlayerSettingsData
{
	[JsonConverter(typeof(DecimalFormatJsonConverter), 1)]
	public float musicVol = 1;
	[JsonConverter(typeof(DecimalFormatJsonConverter), 1)]
	public float sfxVol = 1;
	public int fpsLimit = 60;
	public bool vSync = true;
	public string playerName = "";
	public float deadzone = 0;
	public string serverName = "";
	public string serverPassword = "";
	public string serverMaxPlayers = "10";
	public string[] quickMessages = new string[10];
	public bool trail = false;
	public string currentArcadeVariant = "Original";
	public Language language = Language.English;
}
[Serializable]
public class RankingData
{
	public LinkedList<RankingRowData> TeamVic = new();
	public LinkedList<RankingRowData> TeamPts = new();
	public LinkedList<RankingRowData> TeamChamp = new();
	public LinkedList<RankingRowData> Vic = new();
	public LinkedList<RankingRowData> Pts = new();
	public LinkedList<RankingRowData> Champ = new();
}

public class Info : MonoBehaviour
{
	public const string stringFormatWithoutHours = @"mm\:ss\.ff";
	public const string stringFormatWithHours = @"h\.mm\:ss\.ff";

	public const int TimeOfDays = 2;
	StringTable localizedTable;
	public GameObject renderTextureCam;
	public LoadSelector loadSelector;
	public CarSelector carSelector;
	[NonSerialized]
	public ArcadeVariant curVariant;
	public ArcadeVariant.Node curNode
	{
		get
		{
			if (curArcadeNodeID == -1)
				return curVariant.nodes[targetArcadeNodeID];
			return curVariant.nodes[curArcadeNodeID];
		}
	}
	public ArcadeVariant.Node targetNode
	{
		get
		{
			return curVariant.nodes[targetArcadeNodeID];
		}
	}
	[NonSerialized]
	public int curArcadeNodeID = -1;
	[NonSerialized]
	public int targetArcadeNodeID = 0;
	[NonSerialized]
	public int curArcadeScore = 0;
	public ArcadeSelector arcadeSelector;
	/// <summary> number of remaining stunts to complete the objective </summary>
	public (string, int)[] arcadeObjectiveStunts;

	public const string TranslationTableName = "Default";
	public MultiPlayerSelector mpSelectorInitializer;
	public Text versionText;
	public Material transpMaterial;
	public Material opaqueMaterial;
	public Material emissiveRearLighter;
	public Material emissiveRearDarker;
	public Mesh sphereMesh;
	public AudioMixer mainAudioMixer;
	public const string VERSION = "0.5.6";
	public bool minimized { get; private set; }
	[DllImport("user32.dll")]
	static extern bool SetCursorPos(int X, int Y);
	void OnApplicationFocus(bool hasFocus)
	{
		minimized = !hasFocus;
		//if (minimized)
		//	F.I.enterRef.action.Disable();
		//else
		//	F.I.enterRef.action.Enable();
	}
	/// <summary>Retrieves localized string from loaded localization table</summary>
	public string LocStr(string key)
	{
		var entry = localizedTable.GetEntry(key);
		return entry == null ? key : entry.GetLocalizedString();
	}
	private void Awake()
	{
		F.I = this;
		SetCursorPos(0, 0);
		UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
		MultiPlayerSelector.I = mpSelectorInitializer;
		versionText.text = VERSION;
		int MPtags = CurrentPlayer.ReadOnlyTags().Count();
		switch (MPtags)
		{
			case 1:
				_documentsSGPRpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Stunt GPR 2\\";
				Debug.LogWarning("Player 2 Started");
				break;
			case 2:
				_documentsSGPRpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Stunt GPR 3\\";
				Debug.LogWarning("Player 3 Started");
				break;
			default:
				_documentsSGPRpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\STUNT GP REMASTERED\\";
				break;
		}

		if (!Directory.Exists(documentsSGPRpath))
		{
			Debug.LogWarning(documentsSGPRpath + " doesnt exist");
			Directory.CreateDirectory(documentsSGPRpath);
		}
		F.CopyDocumentsData(Application.streamingAssetsPath, documentsSGPRpath);

		Application.targetFrameRate = playerData.fpsLimit;
		QualitySettings.vSyncCount = playerData.vSync ? 1 : 0;
		ReadSettingsDataFromJson();
		PopulateSFXData();
		ReloadCarsData();
		PopulateTrackData();
		ReloadCarPartsData();
		LoadRanking();
		LoadArcade();
		icons = Resources.LoadAll<Sprite>(trackImagesPath + "tiles");
		LocalizationSettings.InitializeSynchronously = true;
	}
	private void Start()
	{
		UpdateLanguage(true);

	}
	public void UpdateLanguage(bool firstRun = false)
	{
		StartCoroutine(UpdateLanguageCo(firstRun));
	}
	IEnumerator UpdateLanguageCo(bool firstRun)
	{
		if (firstRun)
			yield return LocalizationSettings.InitializationOperation.WaitForCompletion();
		LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[(int)playerData.language];
		localizedTable = LocalizationSettings.StringDatabase.GetTable(TranslationTableName);
	}

	string _documentsSGPRpath;
	public string documentsSGPRpath
	{
		get { return _documentsSGPRpath; }
		private set { _documentsSGPRpath = value; }
	}
	public string partsPath { get { return documentsSGPRpath + "parts\\"; } }
	public string tracksPath { get { return documentsSGPRpath + "tracks\\"; } }
	public string userdataPath { get { return documentsSGPRpath + "userdata.json"; } }
	public string rankingPath { get { return documentsSGPRpath + "ranking.json"; } }
	public string arcadePath { get { return documentsSGPRpath + "arcade\\"; } }
	public string lastPath { get { return documentsSGPRpath + "path.txt"; } }

	public const string arcadeProgressExtension = ".progress";
	public const string alreadyWalkedTag = "walked";

	public Livery s_PlayerCarSponsor = Livery.TGR;

	public readonly int maxCarsInRace = 10;

	// menu inputs
	public PlayerSettingsData playerData;
	public InputActionReference shiftRef;
	public InputActionReference escRef;
	public InputActionReference enterRef;
	public InputActionReference quickMessageRef;
	public InputActionReference chatButtonInput;
	public InputActionReference move2Ref;
	public InputActionReference shiftInputRef;
	public InputActionReference ctrlInputRef;
	public InputActionReference altInputRef;
	public InputActionReference pointRef;

	// car steering inputs
	public InputActionReference driveRef;
	public InputActionReference boostInput;
	public InputActionReference evoInput;
	public InputActionReference honkInput;
	public InputActionReference rollInput;
	public InputActionReference resetOnTrackInput;
	public InputActionReference bunnyhopInput;
	public InputActionReference lookBackInput;
	public InputActionReference lookAxisInput;

	public RankingData rankingData;
	[NonSerialized]
	public List<ArcadeVariant> arcadeVariants;

	[NonSerialized]
	public bool alwaysFirst = true;
	[NonSerialized]
	public bool alwaysBestStuntScore = true;
	[NonSerialized]
	public Livery?[] unlockedLiveries = new Livery?[] { Livery.Random, null, null, null, null, null, null, null };

	public void LoadArcade()
	{
		// iterate over all files in arcadePath
		arcadeVariants = new List<ArcadeVariant>();
		if (!Directory.Exists(arcadePath))
		{
			Directory.CreateDirectory(arcadePath);
		}

		ArcadeVariant[] newVariants = ArcadeVariant.GenerateDefaultVariants();
		foreach (var newVariant in newVariants)
		{
			string serializedVariant = JsonConvert.SerializeObject(newVariant, Formatting.Indented);
			string filepath = Path.Combine(arcadePath, $"{newVariant.name}.json");
			File.WriteAllText(filepath, serializedVariant);
		}

		string[] filepaths = Directory.GetFiles(arcadePath, "*.json", SearchOption.TopDirectoryOnly);

		foreach (var filepath in filepaths)
		{
			string jsonText = File.ReadAllText(filepath);
			try
			{
				ArcadeVariant variant = JsonConvert.DeserializeObject<ArcadeVariant>(jsonText);
				if (variant != null)
					arcadeVariants.Add(variant);

				string progressPath = Path.ChangeExtension(filepath, arcadeProgressExtension);
				if (File.Exists(progressPath))
				{
					string progressText = File.ReadAllText(progressPath);
					variant.progress = JsonConvert.DeserializeObject<ArcadeVariant.Progress>(progressText);
					variant.progress.parent = variant;
				}
				else
				{
					variant.progress = new ArcadeVariant.Progress(variant);
					//string serializedProgress = JsonConvert.SerializeObject(variant.progress, Formatting.Indented);
					//await File.WriteAllTextAsync(progressPath, serializedProgress);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Error loading arcade variant from " + filepath + ": " + e.Message);
			}
		}
		SwitchArcadeVariant(playerData.currentArcadeVariant);
	}
	public void SaveArcadeProgress()
	{
		string jsonText = JsonConvert.SerializeObject(F.I.curVariant.progress);
		File.WriteAllText(arcadePath + F.I.curVariant.name + arcadeProgressExtension, jsonText);
	}
	public void SwitchArcadeVariant(string newVariantName)
	{
		curVariant = arcadeVariants.FirstOrDefault(v => v.name == newVariantName);
		// sort curVariant nodes by id to ensure correct order
		Array.Sort(F.I.curVariant.nodes, (a, b) => a.id.CompareTo(b.id));

		for (int i = 1; i < unlockedLiveries.Length; i++)
			unlockedLiveries[i] = (Livery)i;

		foreach (var track in tracks)
		{
			if (track.Key.Length > 3) // don't unlock default environment tracks
				track.Value.unlocked = true;
		}

		foreach (var car in cars)
		{
			car.starter = false;
			car.unlocked = true;
		}
		foreach (var s in F.I.curVariant.starts)
			foreach (var i in s.allowedCarsIdxs)
				cars[i].starter = true;

		if (playerData.playerName == "HAXOR")
			return;// unlock all for testing 

		playerData.currentArcadeVariant = curVariant.name;

		List<string> prizeNamesToBeLocked = new();
		foreach (var variant in F.I.arcadeVariants)
		{
			for (int i = 0; i < variant.nodes.Length; ++i)// lock prizes for uncompleted prizes
			{
				for (int j = 0; j < variant.nodes[i].prizeReqs.Length; ++j)
				{
					if (variant.progress.prizesCompleted[i].GetBits(j) == 0)
						prizeNamesToBeLocked.AddRange(variant.nodes[i].prizeReqs[j].name.Split(','));
				}
			}

			for (int i = 0; i < variant.globalPrizes.Length; ++i)
			{
				var gprize = variant.globalPrizes[i];
				if (gprize.condition == ArcadeVariant.Prize.Condition.AlwaysFirst)
				{
					if (variant.progress.globalPrizesCompleted.GetBits(i) == 0)
					{
						prizeNamesToBeLocked.AddRange(gprize.name.Split(","));
					}
				}
				else if (gprize.condition == ArcadeVariant.Prize.Condition.AllPathsFound)
				{
					if (variant.progress.pathsDone.Sum(p => p.Count) != variant.nodes.Sum(n => n.connections.Length))
					{
						prizeNamesToBeLocked.AddRange(gprize.name.Split(","));
					}
				}
			}

			foreach (var prizeName in prizeNamesToBeLocked)
			{
				if (prizeName.StartsWith("car"))
				{
					if (variant.name == curVariant.name) 
						F.I.Car(prizeName).unlocked = false;
				}
				else if (prizeName.StartsWith("spn"))
				{
					int liveryIdx = int.Parse(prizeName[3..]);
					unlockedLiveries[liveryIdx] = null;
				}
				else
				{
					tracks[prizeName].unlocked = false;
				}
			}

			prizeNamesToBeLocked.Clear();
		}
	}

	public async void LoadRanking()
	{
		if (!File.Exists(rankingPath))
		{
			rankingData = new RankingData();
			string serializedRanking = JsonConvert.SerializeObject(rankingData);
			await File.WriteAllTextAsync(rankingPath, serializedRanking);
		}
		else
		{
			string serializedRanking = await File.ReadAllTextAsync(rankingPath);
			rankingData = JsonConvert.DeserializeObject<RankingData>(serializedRanking);
		}
	}

	public async void SaveRanking()
	{
		string serializedRanking = JsonConvert.SerializeObject(rankingData);
		await File.WriteAllTextAsync(rankingPath, serializedRanking);
		F.I.SaveArcadeProgress();
	}
	public string SHA(string filePath)
	{
		string hash;
		using (var cryptoProvider = new SHA1CryptoServiceProvider())
		{
			byte[] buffer = File.ReadAllBytes(filePath);
			hash = BitConverter.ToString(cryptoProvider.ComputeHash(buffer));
		}
		return hash;
	}
	public string SHA(in byte[] buffer)
	{
		using var cryptoProvider = new SHA1CryptoServiceProvider();
		return BitConverter.ToString(cryptoProvider.ComputeHash(buffer));
	}
	//public static void MessageSet(this Player player, string msg)
	//{
	//	player.Data[k_message].Value = msg;
	//}


	public void ReadSettingsDataFromJson()
	{
		if (!File.Exists(userdataPath))
		{
			Debug.Log("No " + userdataPath);
			playerData = new PlayerSettingsData();
			string serializedSettings = JsonConvert.SerializeObject(playerData);
			File.WriteAllText(userdataPath, serializedSettings);
		}
		else
		{
			//Debug.Log(userdataPath);
			string playerSettings = File.ReadAllText(userdataPath);
			playerData = JsonConvert.DeserializeObject<PlayerSettingsData>(playerSettings);
		}
	}
	public void SaveSettingsDataToJson()
	{
		string jsonText = JsonConvert.SerializeObject(playerData);
		File.WriteAllText(userdataPath, jsonText);
	}
	public void SaveSettingsDataToJson(in AudioMixer mainMixer)
	{
		playerData.musicVol = ReadMixerLevelLog("musicVol", mainMixer);
		playerData.sfxVol = ReadMixerLevelLog("sfxVol", mainMixer);
		string jsonText = JsonConvert.SerializeObject(playerData);
		File.WriteAllText(userdataPath, jsonText);
	}

	public PartInfo[] partInfos = new PartInfo[]
	{
		new ("Itex", ".suscfg"),
		new ("Mysuko", ".bmscfg"),
		new ("Titan", ".batcfg"),
		new ("Caltex", ".engcfg"),
		new ("TGR", ".chacfg"),
		new ("Itex", ".grscfg"),
		new ("Mysuko", ".jetcfg"),
		new ("Rline", ".tyrcfg"),
		new ("TGR", ".drvcfg"),
		new ("Titan", ".hnkcfg"),
		new ("", ".carcfg"),
	};
	//public static string[] extensionsSuffixes = new string[] { "suscfg", "bmscfg", "batcfg",
	//		"engcfg", "chacfg", "grscfg", "jetcfg", "tyrcfg", "drvcfg", "carcfg" };


	/// <summary>
	/// Number of track textures. Set pavementTypes+1 for random texture.
	/// </summary>
	public readonly int pavementTypes = 6;
	public readonly int RaceTypes = 5;

	public readonly Vector3[] invisibleLevelDimensions = new Vector3[]{
		new (564, 1231,1), //ger
		new (800, 900,1),  //jap
		new (1462, 2480,1),//spn
		new (2170, 1560,1),//fra
		new (1170, 817,1), //eng
		new (739, 1060,1), //usa
		new (1406, 1337,1),// ita
		new (564, 1231,1), //mex
	};
	public readonly int[] skys = new int[] {
		8, //ger
		2, //jap
		5, //spn
		1, //fra
		4, //eng
		3, //usa
		7, // ita
		8  //mex
	};

	public int Environments = 8;
	public int Liveries = 7;
	//public readonly string[] EnvirDescs =
	//{
	//	"GERMANY\n\nLoud crowd cheering and powerful spotlights..This german arena is really a place to show off.",
	//	"JAPAN\n\nHere in this calm japanese dojo placed on the outskirts of Kyoto you can meditate or organize a race!",
	//	"SPAIN\n\nBeaches like this usually ooze holidays. This is not an exception: warm sand, palms, and sun.. What could people possibly want more? Maybe a RC car race :)",
	//	"FRANCE\n\nThis shadowy warehouse is full of boxes, forklifts and machinery. There are some really dark places here.",
	//	"ENGLAND\n\nEnglish go-kart track is a good location to test your driving skills. This place has a reputation for great races.",
	//	"USA\n\nAre you looking for an intense experience? Racing on top of a multistorey parking lot located in the heart of New York will be a bombastic idea!",
	//	"ITALY\n\nFeeling mediterranean? This italian coast is very scenic, especially at night. There are two dangers here to look out however: staircase descent and water!",
	//	"MEXICO\n\nOnly some people are in a possession of info that there's this ancient place located in the middle of an unknown mexican forest, where aztecs used to race RC-cars. However no-one really knows how to get there."
	//};

	public readonly string carPrefabsPath = "carModels/";
	public readonly string carImagesPath = "carImages/";
	public readonly string trackImagesPath = "trackImages/";
	public readonly string editorTilesPath = "tiles/objects/";
	public RankingView rankingView;
	public ResultsView resultsView;
	public ViewSwitcher viewSwitcher;
	public Chat chat;
	[NonSerialized]
	public List<int> stuntpointsContainer = new();
	[NonSerialized]
	public List<ReplayCam> replayCams = new();
	[NonSerialized]
	public Vector3[] carSGPstats;
	[NonSerialized]
	public Car[] cars;
	public ScoringType scoringType;
	public GameMode gameMode = GameMode.Exhibition;
	public ActionHappening actionHappening = ActionHappening.InLobby;
	[NonSerialized]
	public Dictionary<string, PartSavable> carParts;
	[NonSerialized]
	public SortedDictionary<string, TrackHeader> tracks;
	[NonSerialized]
	public Dictionary<string, AudioClip> audioClips;
	[NonSerialized]
	public bool loaded = false;
	[NonSerialized]
	public int roadLayer = 6;
	[NonSerialized]
	public int pylonLayer = 9;

	public string visibleInPictureModeTag = "VisibleInPictureMode";
	public readonly int ignoreWheelCastLayer = 8;
	public readonly int vehicleLayer = 9;
	public readonly int connectorLayer = 11;
	public readonly int invisibleLevelLayer = 12;
	public readonly int terrainLayer = 13;
	public readonly int cameraLayer = 14;
	public readonly int flagLayer = 15;
	public readonly int[] racingLineLayers = new[] { 16, 25, 27, 28 };
	public readonly int pitsLineLayer = 17;
	public readonly int pitsZoneLayer = 18;
	public readonly int aeroTunnel = 19;
	public readonly int vehicleTriggerLayer = 22;
	public readonly int surfaceLayer = 23;
	public readonly int ghostLayer = 24;
	public readonly int carCarCollisionLayer = 26;

	public readonly Color32 yellow = new(255, 223, 0, 255);
	public readonly Color32 orange = new(255, 69, 0, 255);
	public readonly Color32 red = new(255, 64, 64, 255);
	/// <summary>
	/// Only one object at the time can have this layer
	/// </summary>
	public int selectionLayer = 20;
	[NonSerialized]
	public bool randomPavement = true;
	// curr/next session data
	public bool s_spectator;
	public List<VehicleParent> s_cars = new();
	public string s_trackName = "USA";
	/// <summary>
	/// e.g car01
	/// </summary>
	public int s_playerCarIdx = 0;
	[NonSerialized]
	public RaceType s_raceType = RaceType.Race;
	/// <summary>
	/// set to 0 to indicate freeroam
	/// </summary>
	public int s_laps = 3;
	public bool s_inEditor = true;
	public TimeOfDay s_timeOfDay = TimeOfDay.Day;
	public CpuLevel s_cpuLevel = CpuLevel.Medium;
	public int s_cpuRivals = 0; // 0-9
	[NonSerialized]
	public PavementType s_roadType = PavementType.Random;
	public bool catchup = true;
	public int s_resultPos = 3;
	public bool teams = false;
	public int ServerIdGenerator = 0;

	public readonly string[] IconNames =
	{
		"Stunty", "Loop", "Jumpy", "Windy", "Intersecting", "No_pits", "No_jumps", "Icy", "Sandy", "Offroad"
	};
	public Sprite[] icons;
	public bool gamePaused;
	internal bool controllerInUse;
	internal bool randomCars;
	internal bool randomTracks;
	internal int hostId;
	public int racingPathResolution = 10;

	public const int AfterMultiPlayerRaceWaitForPlayersSeconds = 30;

	public EventSystem eventSystem;
	public DateTime raceStartDate = DateTime.MinValue;
	public byte Rounds = 0;
	public byte CurRound;
	public const string usersManualURL = "https://docs.google.com/document/d/1PNb95xUi0pdOjPetwu-MNLeIwpVN6t8rxAmukKEpB2E/";
	public readonly int maxConcurrentUsers = 30;

	public Car Car(string name)
	{ // i.e. car05
		try
		{
			int i = int.Parse(name[3..]);
			return cars[i];
		}
		catch
		{
			Debug.LogError(name);
			return cars[0];
		}
	}
	public void ReloadCarPartsData()
	{
		string[] extensionsSuffixes = partInfos.Select(i => i.fileExtension).ToArray();
		string[] filepaths = Directory.GetFiles(partsPath)
			.Where(filepath => extensionsSuffixes.Any(filepath.ToLower().EndsWith))
			.ToArray();
		if (carParts == null)
			carParts = new Dictionary<string, PartSavable>();
		else
			carParts.Clear();
		foreach (var filepath in filepaths)
		{
			ComponentPanel.AddPart(filepath);
		}
	}
	public void ReloadCarsData()
	{
		if (cars == null)
		{
			cars = new Car[]
			{
				new ("car00",0,CarGroup.Speed, Livery.Itex, "MEAN STREAK","Fast, light and agile, this racer offers much for those who wish to modify their vehicle."),
				new ("car01",45000,CarGroup.Wild,Livery.Caltex, "THE HUSTLER","Sturdy 4x4 pick-up truck with an eye for the outrageous!"),
				new ("car02",50000,CarGroup.Aero, Livery.Mysuko, "TWIN EAGLE","Take flight with this light and speedy stuntcar."),
				new ("car03",0,CarGroup.Aero, Livery.TGR, "SKY HAWK","Get airborne with this very versatile stunt car."),
				new ("car04",30000,CarGroup.Speed, Livery.Rline, "THE PHANTOM","Fast, sleek and tough to handle."),
				new ("car05",30000,CarGroup.Wild, Livery.Titan, "ROAD HOG","Rock and Roll with the rough ridin' road hog."),
				new ("car06",0,CarGroup.Wild, Livery.Itex, "DUNE RAT","Defy the laws of physics in this buggy."),
				new ("car07",50000,CarGroup.Speed, Livery.Titan, "LIGHTNIN'","Supercharged super speed. Easy does it!"),
				new ("car08",30000,CarGroup.Speed, Livery.Caltex, "ALLEY KAT","Sleek and powerful, this cat is ready to roar."),
				new ("car09",40000,CarGroup.Wild, Livery.Itex, "SAND SHARK","This beachcomber is at home on any stunt circuit."),
				new ("car10",45000,CarGroup.Wild, Livery.TGR, "THE BRUTE","Unleash the Brute for no-nonsense on the road!"),
				new ("car11",70000,CarGroup.Aero, Livery.Titan,"WILD DART","Fly fast and true with this stuntcar."),
				new ("car12",65000,CarGroup.Wild, Livery.Mysuko, "RAGING BULL","Powerful and fast, this streetwise 4x4 is incredible."),
				new ("car13",15000,CarGroup.Aero, Livery.Caltex, "FLYING MANTIS","Super light and very fast."),
				new ("car14",35000,CarGroup.Aero, Livery.Rline, "STUNT MONKEY","Monkey see, monkey do! Go bananas with this wild ride!"),
				new ("car15",50000,CarGroup.Speed, Livery.Titan, "INFERNO","This speed demon is on fire!"),
				new ("car16",35000,CarGroup.Team, Livery.Team, "FORK","Despite its looks, it moves like fork lightning!"),
				new ("car17",55000,CarGroup.Team, Livery.Team, "WORM MOBILE","Super Speedy Buggy!"),
				new ("car18",100000,CarGroup.Team, Livery.Itex, "FORMULA 17","Incredibly fast racing car."),
				new ("car19",90000,CarGroup.Team, Livery.Team, "TEAM MACHINE","The ultimate, hugely versatile stock car.")
			};
		}
		ReloadCarConfigs();
	}
	public async void ReloadCarConfigs()
	{
		string carSuffix = partInfos[^1].fileExtension;
		string[] filepaths = Directory.GetFiles(partsPath)
			.Where(filepath => filepath.ToLower().EndsWith(carSuffix))
			.ToArray();

		for (int i = 0; i < cars.Length; ++i)
		{
			await Task.Run(() =>
			{
				string filepath = partsPath + "car" + i.ToString() + partInfos[^1].fileExtension;
				string jsonText = File.ReadAllText(filepath);
				cars[i].config = new CarConfig("car" + i.ToString(), jsonText);
			});
		}
	}
	public void PopulateTrackData()
	{
		if (tracks == null)
			tracks = new SortedDictionary<string, TrackHeader>();
		else
			return;
		// 0         1			2			3			4					5				6				7		8			9
		//"stunty", "loop", "jumpy", "windy", "intersecting", "no_pit", "no_jumps", "icy", "sandy", "offroad"
		//										unlock   preffered				   author            flags
		tracks.Add("JAP", new TrackHeader(0, 0, 4, Envir.JAP, null, new int[] { }, null, null, false));
		tracks.Add("GER", new TrackHeader(0, 0, 4, Envir.GER, null, new int[] { }, null, null, false));
		tracks.Add("SPN", new TrackHeader(0, 0, 4, Envir.SPN, null, new int[] { }, null, null, false));
		tracks.Add("FRA", new TrackHeader(0, 0, 4, Envir.FRA, null, new int[] { }, null, null, false));
		tracks.Add("ENG", new TrackHeader(0, 0, 4, Envir.ENG, null, new int[] { }, null, null, false));
		tracks.Add("USA", new TrackHeader(0, 0, 4, Envir.USA, null, new int[] { }, null, null, false));
		tracks.Add("ITA", new TrackHeader(0, 0, 4, Envir.ITA, null, new int[] { }, null, null, false));
		tracks.Add("MEX", new TrackHeader(0, 0, 4, Envir.MEX, null, new int[] { }, null, null, false));

		//tracks.Add("track01", new TrackHeader(1, (CarGroup)2, 6, Envir.FRA, null, new int[] { 2 }, "CRAZY STRAIGHTS\n\nThis long speed track offers opportunity for a number of jump stunts."));
		//tracks.Add("track02", new TrackHeader(1, (CarGroup)2, 4, Envir.JAP, null, new int[] { 0 }, "BANK JOB\n\nThis short, speedy circuit offers a number of stunt opportunities and high-banks for sneaky overtaking."));
		//tracks.Add("track03", new TrackHeader(1, (CarGroup)1, 7, Envir.JAP, null, new int[] { 2 }, "TUBULAR HELL\n\nA long and winding track with many ramps. Try not to climb too high in the tubular sections!"));
		//tracks.Add("track04", new TrackHeader(1, (CarGroup)2,6, Envir.FRA, null, new int[] { 2 }, "CURBED HEIGHTS\n\nA long, high track that is best navigated by hugging the racing line.."));
		//tracks.Add("track05", new TrackHeader(1, (CarGroup)1,8, Envir.ITA, null, new int[] { 2 }, "FLYING FINISH\n\nA short, tough and dramatic track with a huge jump over the finish line!"));
		//tracks.Add("track06", new TrackHeader(1, (CarGroup)1,7, Envir.SPN, null, new int[] { 2 }, "SECRET SIX\n\nAn exciting track with plenty of ramps and a cross-over."));
		//tracks.Add("track07", new TrackHeader(1, (CarGroup)0,8, Envir.SPN, null, new int[] { 8 }, "THE SANDWINDER\n\nA huge, winding off-road track featuring a very bumpy mid section and multi-level turns."));
		//tracks.Add("track08", new TrackHeader(1, (CarGroup)0,6, Envir.SPN, null, new int[] { 8,6 }, "ROUGHDUST FLATS\n\nThe only flat track in the original Stunt GP collection, this is far from a gentle experience!"));
		//tracks.Add("track09", new TrackHeader(1, (CarGroup)1,7, Envir.ENG, null, new int[] { 2 }, "THE LOOPBACK\n\nA long, fast track with many opportunities for jumps and stunts."));
		//tracks.Add("track10", new TrackHeader(1, (CarGroup)2,7, Envir.ENG, null, new int[] { 4 }, "INTERSECTOR\n\nA long, fast track with a multitude of mad crossovers!"));
		//tracks.Add("track11", new TrackHeader(1, (CarGroup)1,6, Envir.SPN, null, new int[] { 2 }, "RISE'N'FALL\n\nHave you got the stomach for the massive climb and fall? Not to mention the loop!"));
		//tracks.Add("track12", new TrackHeader(1, (CarGroup)2,5, Envir.SPN, null, new int[] { 0 }, "WIDE WALL CHASE\n\nA very fast night track where you can use the burns for over-taking."));
		//tracks.Add("track13", new TrackHeader(1, (CarGroup)2,5, Envir.USA, null, new int[] { 3 }, "CURB CITY CIRCUIT\n\nA track of contrasts, straight outside and tricky inside."));
		//tracks.Add("track14", new TrackHeader(1, (CarGroup)2,9, Envir.ITA, null, new int[] { 3 }, "WATERFRONT DASH\n\nThis long and winding track takes you all over the waterfront!"));
		//tracks.Add("track15", new TrackHeader(1, (CarGroup)1,6, Envir.SPN, null, new int[] { 2 }, "FREEFALL FREEWAY\n\nTwo huge jumps and a mighty climb feature in this evening excursion."));
		//tracks.Add("track16", new TrackHeader(1, (CarGroup)2,7, Envir.GER, null, new int[] { 3 }, "HIGH ROLLER\n\nThis complex, winding track has a number of high curved bends."));
		//tracks.Add("track17", new TrackHeader(1, (CarGroup)0,9, Envir.JAP, null, new int[] { 8 }, "HIGHFLY CLEARWAY\n\nA large and difficult track with bumps, jumps and cross-overs."));
		//tracks.Add("track18", new TrackHeader(1, (CarGroup)1,6, Envir.USA, null, new int[] { 2 }, "HELIPAD HEIGHTS\n\nThis interior and exterior track features a spectacular leap across a skyline!"));
		//tracks.Add("track19", new TrackHeader(1, (CarGroup)0,6, Envir.SPN, null, new int[] { 8 }, "SUNKEN SIGHTS\n\nA three level sunken area and a high banked climb are the highlights of this dusty track."));
		//tracks.Add("track20", new TrackHeader(1, (CarGroup)0,4, Envir.JAP, null, new int[] { 8 }, "DUST BUSTER\n\nThis fast, relatively flat track offers many ways to drive."));
		//tracks.Add("track21", new TrackHeader(1, (CarGroup)1,8, Envir.GER, null, new int[] { 1 }, "TWINLOOP CIRCUIT\n\nTwo huge climbs and a double loop make this a formidable track."));
		//tracks.Add("track22", new TrackHeader(1, (CarGroup)1,6, Envir.GER, null, new int[] { 1 }, "SNAKESTORM\n\nA long, winding track with multi-levels and a loop."));
		//tracks.Add("track23", new TrackHeader(1, (CarGroup)2,4, Envir.USA, null, new int[] { 0 }, "SKYTOP SPEED CIRCUIT\n\nA very small, fast track with a couple of jumps and high banks."));
		//tracks.Add("track24", new TrackHeader(1, (CarGroup)2,4, Envir.FRA, null, new int[] { 0 }, "THE CHRONOZONE\n\nA very fast track with burns and a very sharp turn."));

		// lap race stunt drift
		//TrackHeader.Record[] records = new TrackHeader.Record[]
		//{
		//	new TrackHeader.Record("Viatrufka", 87.3f),
		//	new TrackHeader.Record("T17", 187.3f),
		//	new TrackHeader.Record(null, 0),
		//	new TrackHeader.Record("Via", 3500),
		//};
		//tracks["track02"].records = records;
		string[] trackFiles = Directory.GetFiles(tracksPath, "*.track");
		foreach (var path in trackFiles)
		{
			string trackJson = File.ReadAllText(path);
			string name = Path.GetFileNameWithoutExtension(path);
			string recordsPath = path[..path.IndexOf('.')] + ".rec";
			TrackHeader header = JsonConvert.DeserializeObject<TrackHeader>(trackJson);

			if (header.localizedNames.Any(n => n == "")) // if importing old tracks with no localizations, at least write a name
				for (int i = 0; i < header.localizedNames.Length; ++i)
					header.localizedNames[i] = name;

			tracks.Add(name, header);

			if (File.Exists(recordsPath))
			{
				string recordsJson = File.ReadAllText(recordsPath);
				TrackRecords records = JsonConvert.DeserializeObject<TrackRecords>(recordsJson);
				tracks[name].records = records;
			}
			else
			{
				tracks[name].records = new();
			}
		}
	}
	public float ReadMixerLevelLog(string exposedParameter, AudioMixer mixer)
	{
		mixer.GetFloat(exposedParameter, out float inVal);
		return Mathf.Pow(10, 3 / 160f * inVal);
	}
	public void SetMixerLevelLog(string exposedParameter, float val01, in AudioMixer mixer)
	{
		float toLogLevel = 80 * 2 / 3f * Mathf.Log10(val01);
		if (toLogLevel < -80)
			toLogLevel = -80;
		//Debug.Log("set" + exposedParameter + " to level:" + toLogLevel.ToString());
		mixer.SetFloat(exposedParameter, toLogLevel);
	}
	public float InGroupPos(Transform child)
	{
		if (child.parent.childCount <= 1)
			return 0;
		return (float)child.GetSiblingIndex() / (child.parent.childCount - 1);
	}
	internal void PopulateSFXData()
	{
		if (audioClips == null)
			audioClips = new Dictionary<string, AudioClip>();
		else
			return;

		var clipsSFX = Resources.LoadAll<AudioClip>("sfx");
		foreach (var c in clipsSFX)
			audioClips.Add(c.name, c);
	}

	/// <summary>
	/// Loads latest path from StreamingAssets/Path.txt
	/// </summary>
	/// <returns></returns>
	public string LoadLastFolderPath()
	{
		string MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		StreamReader w = new StreamReader(lastPath);
		string LastTrackPath = w.ReadLine();
		w.Close();
		if (LastTrackPath == "")
			LastTrackPath = MyDocuments;
		//Debug.Log("LoadPath:" + LastTrackPath);
		return LastTrackPath;
	}

	/// <summary>
	/// Saves latest path to My Documents\path.txt
	/// </summary>
	public void SaveLastFolderPath(string path)
	{
		if (path == null)
		{
			Debug.LogError("path null");
			return;
		}
		Debug.Log(path);
		StreamWriter w = new(lastPath);
		w.WriteLine(path);
		w.Close();
	}

	public static Mesh MergeVertices(Mesh combinedMesh, float threshold = 0.7f)
	{
		Vector3[] oldVerts = combinedMesh.vertices;
		int[] oldTris = combinedMesh.triangles;

		Vector3Int Quantize(Vector3 v) =>
				new Vector3Int(
						Mathf.RoundToInt(v.x / threshold),
						Mathf.RoundToInt(v.y / threshold),
						Mathf.RoundToInt(v.z / threshold)
				);

		var dict = new ConcurrentDictionary<Vector3Int, int>();
		var verts = new List<Vector3>();
		int[] map = new int[oldVerts.Length];
		int counter = -1;
		object lockObj = new object();

		Parallel.For(0, oldVerts.Length, i =>
		{
			Vector3Int q = Quantize(oldVerts[i]);
			int? assigned = null;

			// Check 27 neighboring bins
			for (int dx = -1; dx <= 1 && !assigned.HasValue; dx++)
				for (int dy = -1; dy <= 1 && !assigned.HasValue; dy++)
					for (int dz = -1; dz <= 1 && !assigned.HasValue; dz++)
					{
						Vector3Int neighbor = new Vector3Int(q.x + dx, q.y + dy, q.z + dz);
						if (dict.TryGetValue(neighbor, out int idx))
						{
							Vector3 existing;
							lock (lockObj) existing = verts[idx];
							if (Vector3.Distance(existing, oldVerts[i]) < threshold)
								assigned = idx;
						}
					}

			if (!assigned.HasValue)
			{
				lock (lockObj)
				{
					// Double-check after entering lock to avoid duplicate rep
					if (!dict.TryGetValue(q, out int idx2))
					{
						int newIndex = ++counter;
						verts.Add(oldVerts[i]);
						dict[q] = newIndex;
						assigned = newIndex;
					}
					else
					{
						assigned = idx2;
					}
				}
			}

			map[i] = assigned.Value;
		});

		int[] newTris = new int[oldTris.Length];
		for (int i = 0; i < oldTris.Length; i++)
			newTris[i] = map[oldTris[i]];

		Mesh weldedMesh = new Mesh();
		weldedMesh.vertices = verts.ToArray();
		weldedMesh.triangles = newTris;
		weldedMesh.RecalculateNormals();
		return weldedMesh;
	}


}
[Serializable]
public class Record
{
	public string playerName;
	public float secondsOrPts;
	public float requiredSecondsOrPts;
	public Record(string playerName, float secondsOrPts, float requiredSecondsOrPts = 0)
	{
		this.playerName = playerName;
		this.secondsOrPts = secondsOrPts;
		this.requiredSecondsOrPts = requiredSecondsOrPts;
	}
	private Record()
	{
		this.playerName = null;
		this.secondsOrPts = 0;
		this.requiredSecondsOrPts = 0;
	}
	public Record(Record r)
	{
		this.playerName = r.playerName;
		this.secondsOrPts = r.secondsOrPts;
		this.requiredSecondsOrPts = r.requiredSecondsOrPts;
	}
}
[Serializable]
public class TrackRecords
{
	public Record lap;
	public Record race;
	public Record stunt;
	public Record drift;
	public TrackRecords()
	{
		lap = new(null, 35999, 35999);
		race = new(null, 0);
		stunt = new(null, 0);
		drift = new(null, 0);
	}
	public TrackRecords(TrackRecords records)
	{
		lap = records.lap;
		race = records.race;
		stunt = records.stunt;
		drift = records.drift;
	}
	public Record this[int key]
	{
		get
		{
			return key switch
			{
				0 => lap,
				1 => race,
				2 => stunt,
				3 => drift,
				_ => lap,
			};
		}
		set
		{
			switch (key)
			{
				case 0:
					lap = value;
					break;
				case 1:
					race = value;
					break;
				case 2:
					stunt = value;
					break;
				case 3:
					drift = value;
					break;
			}
		}
	}

}

[Serializable]
public class TrackHeader
{
	public string author;
	/// <summary>Read from helper method instead of this field</summary>
	public string[] localizedDescriptions;
	/// <summary>Read from helper method instead of this field</summary>
	public string[] localizedNames;
	/// <summary>
	/// whether the track can be raced on (has its path closed)
	/// </summary>
	public bool valid;
	public Envir envir;
	public CarGroup preferredCarClass;
	/// <summary>
	/// starts from 0 (sprites are from 4!)
	/// </summary>
	public int difficulty;
	public bool unlocked;
	public int[] icons;
	/// <summary>
	/// lap, race, stunt, drift 
	/// </summary>
	[NonSerialized]
	public TrackRecords records;
	public TrackHeader()
	{
		records = new();
		localizedDescriptions = EmptyLocStrArray("");
		localizedNames = EmptyLocStrArray("");
	}
	public TrackHeader(int unlocked, CarGroup prefCarClass, int trackDifficulty,
		Envir envir, string author, int[] icons, string[] localizedDescriptions, string[] localizedNames, bool valid = true)
		: this()
	{
		this.unlocked = unlocked > 0;
		this.preferredCarClass = prefCarClass;
		this.difficulty = trackDifficulty;
		this.envir = envir;
		this.author = author;

		localizedDescriptions ??= EmptyLocStrArray("----");
		this.localizedDescriptions = localizedDescriptions;

		localizedNames ??= EmptyLocStrArray("----");
		this.localizedNames = localizedNames;
		this.valid = valid;
		this.icons = icons;
	}

	public TrackHeader(TrackHeader h)
	{
		this.unlocked = h.unlocked;
		this.preferredCarClass = h.preferredCarClass;
		this.difficulty = h.difficulty;
		this.envir = h.envir;
		this.author = h.author;
		this.localizedDescriptions = h.localizedDescriptions;
		this.localizedNames = h.localizedNames;
		this.valid = h.valid;
		this.icons = h.icons;
	}
	public static string[] EmptyLocStrArray(string str)
	{
		string[] descs = new string[LocalizationSettings.AvailableLocales.Locales.Count()];
		for (int i = 0; i < descs.Length; i++)
			descs[i] = str;

		return descs;
	}
	[JsonIgnore]
	public int TrackOrigin
	{
		get { return (author == "Team17") ? 0 : 1; }
	}
	[JsonIgnore]
	public bool IsOriginal
	{
		get { return TrackOrigin == 0; }
	}
	[JsonIgnore]
	public string LocalizedDesc
	{
		get
		{
			return localizedDescriptions[(int)F.I.playerData.language];
		}
	}
	[JsonIgnore]
	public string LocalizedName
	{
		get
		{
			return localizedNames[(int)F.I.playerData.language];
		}
	}
}

public class Car
{
	public string desc;
	public string name;
	public CarGroup category;
	public CarConfig config;
	public int price;
	public int rooster;
	public string internalName;
	public bool unlocked = false;
	public bool starter = false;
	public Livery defaultLivery { get; private set; } = Livery.TGR;
	public Car(string internalName, int price, CarGroup carClass, Livery defaultLivery, string name, string desc, int rooster = 0)
	{
		this.internalName = internalName;
		this.desc = desc;
		this.category = carClass;
		this.name = name;
		this.price = price;
		this.rooster = rooster;
		this.defaultLivery = defaultLivery;
	}
	public static int Name2Index(string name)
	{ // i.e. car05
		try
		{
			int i = int.Parse(name[3..]);
			return i;
		}
		catch
		{
			Debug.Log(name);
			return 0;
		}
	}
}
public struct PartInfo
{
	public string manufacturer;
	public string fileExtension;
	public PartInfo(string m, string e)
	{
		manufacturer = m;
		fileExtension = e;
	}
}

public static class IMG2Sprite
{

	//Static class instead of _instance
	// Usage from any other script:
	// MySprite = IMG2Sprite.LoadNewSprite(FilePath, [PixelsPerUnit (optional)], [spriteType(optional)])

	public static Sprite LoadNewSprite(string FilePath, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
	{
		Sprite sprite;
		Texture2D tex;
		if (File.Exists(FilePath))
		{
			tex = LoadTexture(FilePath);
		}
		else
		{
			tex = new Texture2D(1024, 1024);
		}

		sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);
		return sprite;
	}

	public static Sprite ConvertTextureToSprite(Texture2D texture, float PixelsPerUnit = 100.0f, SpriteMeshType spriteType = SpriteMeshType.Tight)
	{
		// Converts a Texture2D to a sprite, assign this texture to a new sprite and return its reference

		Sprite NewSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);

		return NewSprite;
	}

	public static Texture2D LoadTexture(string FilePath)
	{

		// Load a PNG or JPG file from disk to a Texture2D
		// Returns null if load fails

		Texture2D Tex2D;
		byte[] FileData;

		if (File.Exists(FilePath))
		{
			FileData = File.ReadAllBytes(FilePath);
			Tex2D = new Texture2D(2, 2);           // Create new "empty" texture
			if (Tex2D.LoadImage(FileData))           // Load the imagedata into the texture (size is set automatically)
				return Tex2D;                 // If data = readable -> return texture
		}
		return null;                     // Return null if load failed
	}

}

