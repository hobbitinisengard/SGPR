using Newtonsoft.Json;
using RVP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using System.Security.Cryptography;
using PathCreation;
using UnityEngine.EventSystems;
using Unity.Multiplayer.Playmode;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public enum PlayerState { InRace, InLobbyUnready, InLobbyReady};
public enum Envir { GER, JAP, SPN, FRA, ENG, USA, ITA, MEX };
public enum CarGroup { Wild, Aero, Speed, Team };
public enum Livery { Random = 0, Special = 1, TGR, Rline, Itex, Caltex, Titan, Mysuko }
public enum RecordType { BestLap, RaceTime, StuntScore, DriftScore }
public enum ScoringType { Championship, Points, Victory }
public enum ActionHappening { InLobby, InRace }
public enum PavementType { Arena, Volcano, Asphalt, Energy, Grid, Japan, Jungle, Random }
public enum MultiMode { Singleplayer, Multiplayer };
public enum RaceType { Race, Knockout, Stunt, Drift, TimeTrial }
public enum CpuLevel { Normal };

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
	public float steerGamma = 0;
	public string serverName = "";
	public string serverPassword = "";
	public string serverMaxPlayers = "10";
	public string[] quickMessages = new string[10];
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
	public MultiPlayerSelector mpSelectorInitializer;
	public Shader transpShader;
	public Shader opaqueShader;
	public Text versionText;
	public const string VERSION = "0.3.5";
	public bool minimized { get; private set;  }
	void OnApplicationFocus(bool hasFocus)
	{
		minimized = !hasFocus;
	}
	private void Awake()
	{
		F.I = this;
		MultiPlayerSelector.I = mpSelectorInitializer;
		versionText.text = VERSION;
		MPtags = CurrentPlayer.ReadOnlyTags().Count;
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
		PopulateSFXData();
		ReloadCarsData();
		PopulateTrackData();
		ReloadCarPartsData();
		LoadRanking();
		icons = Resources.LoadAll<Sprite>(trackImagesPath + "tiles");
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
	public string lastPath { get { return documentsSGPRpath + "path.txt"; } }

	public Livery s_PlayerCarSponsor = Livery.Special;

	int MPtags;

	public readonly int maxCarsInRace = 10;

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

	public RankingData rankingData;
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
			Debug.Log(userdataPath);
			string playerSettings = File.ReadAllText(userdataPath);
			playerData = JsonConvert.DeserializeObject<PlayerSettingsData>(playerSettings);
			Debug.Log(playerData == null);
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
		new (800, 800,1), //jap
		new (1462, 2480,1),//spn
		new (2170, 1560,1),//fra
		new (1170, 817,1),//eng
		new (739, 1060,1),//usa
		new (1406, 1337,1),// ita
		new (564, 1231,1), //mex
	};
	public readonly int[] skys = new int[] { 8, 2, 5, 1, 4, 3, 7, 9 };

	public int Environments = 8;
	public int Liveries = 7;

	public readonly string[] EnvirDescs =
	{
		"GERMANY\n\nLoud crowd cheering and powerful spotlights..This german arena is really a place to show off.",
		"JAPAN\n\nHere in this calm japanese dojo placed on the outskirts of Kyoto you can meditate or organize a race!",
		"SPAIN\n\nBeaches like this usually ooze holidays. This is not an exception: warm sand, palms, and sun.. What could people possibly want more? Maybe a RC car race :)",
		"FRANCE\n\nThis shadowy warehouse is full of boxes, forklifts and machinery. There are some really dark places here.",
		"ENGLAND\n\nEnglish go-kart track is a good location to test your driving skills. This place has a reputation for great races.",
		"USA\n\nAre you looking for an intense experience? Racing on top of a multistorey parking lot located in the heart of New York will be a bombastic idea!",
		"ITALY\n\nFeeling mediterranean? This italian coast is very scenic, especially at night. There are two dangers here to look out however: staircase descent and water!",
		"MEXICO\n\nOnly some people are in a possession of info that there's this ancient place located in the middle of an unknown mexican forest, where aztecs used to race RC-cars. However no-one really knows how to get there."
	};

	public readonly string carPrefabsPath = "carModels/";
	public readonly string carImagesPath = "carImages/";
	public readonly string trackImagesPath = "trackImages/";
	public readonly string editorTilesPath = "tiles/objects/";
	public ResultsView resultsView;
	public ViewSwitcher viewSwitcher;
	public Chat chat;
	public PathCreator universalPath;

	public List<int> stuntpointsContainer = new();
	public List<ReplayCam> replayCams = new();
	public Vector3[] carSGPstats;
	public Car[] cars;
	public ScoringType scoringType;
	public MultiMode gameMode = MultiMode.Singleplayer;
	public ActionHappening actionHappening = ActionHappening.InLobby;
	public Dictionary<string, PartSavable> carParts;
	public SortedDictionary<string, TrackHeader> tracks;
	public Dictionary<string, AudioClip> audioClips;

	public bool loaded = false;
	public int roadLayer = 6;

	public string visibleInPictureModeTag = "VisibleInPictureMode";
	public readonly int ignoreWheelCastLayer = 8;
	public readonly int vehicleLayer = 9;
	public readonly int connectorLayer = 11;
	public readonly int invisibleLevelLayer = 12;
	public readonly int terrainLayer = 13;
	public readonly int cameraLayer = 14;
	public readonly int flagLayer = 15;
	public readonly int racingLineLayer = 16;
	public readonly int pitsLineLayer = 17;
	public readonly int pitsZoneLayer = 18;
	public readonly int aeroTunnel = 19;
	public readonly int surfaceLayer = 23;
	public readonly int ghostLayer = 24;
	public readonly int carCarCollisionLayer = 26;

	public readonly Color32 yellow = new(255, 223, 0, 255);
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
	public string s_playerCarName = "car01";
	[NonSerialized]
	public RaceType s_raceType = RaceType.Race;
	/// <summary>
	/// set to 0 to indicate freeroam
	/// </summary>
	public int s_laps = 3;
	public bool s_inEditor = true;
	public bool s_isNight = false;
	public CpuLevel s_cpuLevel = CpuLevel.Normal;
	public int s_cpuRivals = 0; // 0-9
	[NonSerialized]
	public PavementType s_roadType = PavementType.Random;
	public bool s_catchup = true;
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
	public readonly string version = "0.3";

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
			return cars[i - 1];
		}
		catch
		{
			Debug.Log(name);
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
				new (0,CarGroup.Speed, "MEAN STREAK","Fast, light and agile, this racer offers much for those who wish to modify their vehicle."),
				new (45000,CarGroup.Wild, "THE HUSTLER","Sturdy 4x4 pick-up truck with an eye for the outrageous!"),
				new (50000,CarGroup.Aero, "TWIN EAGLE","Take flight with this light and speedy stuntcar."),
				new (0,CarGroup.Aero, "SKY HAWK","Get airborne with this very versatile stunt car."),
				new (30000,CarGroup.Speed, "THE PHANTOM","Fast, sleek and tough to handle."),
				new (30000,CarGroup.Wild, "ROAD HOG","Rock and Roll with the rough ridin' road hog."),
				new (0,CarGroup.Wild, "DUNE RAT","Defy the laws of physics in this buggy."),
				new (50000,CarGroup.Speed, "LIGHTNIN'","Supercharged super speed. Easy does it!"),
				new (30000,CarGroup.Speed, "ALLEY KAT","Sleek and powerful, this cat is ready to roar."),
				new (40000,CarGroup.Wild, "SAND SHARK","This beachcomber is at home on any stunt circuit."),
				new (45000,CarGroup.Wild, "THE BRUTE","Unleash the Brute for no-nonsense on the road!"),
				new (70000,CarGroup.Aero, "WILD DART","Fly fast and true with this stuntcar."),
				new (65000,CarGroup.Wild, "RAGING BULL","Powerful and fast, this streetwise 4x4 is incredible."),
				new (15000,CarGroup.Aero, "FLYING MANTIS","Super light and very fast."),
				new (35000,CarGroup.Aero, "STUNT MONKEY","Monkey see, monkey do! Go bananas with this wild ride!"),
				new (50000,CarGroup.Speed, "INFERNO","This speed demon is on fire!"),
				new (35000,CarGroup.Team, "FORK","Despite its looks, it moves like fork lightning!"),
				new (55000,CarGroup.Team, "WORM MOBILE","Super Speedy Buggy!"),
				new (100000,CarGroup.Team, "FORMULA 17","Incredibly fast racing car."),
				new (90000,CarGroup.Team, "TEAM MACHINE","The ultimate, hugely versatile stock car.")
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
				string filepath = partsPath + "car" + (i + 1).ToString() + partInfos[^1].fileExtension;
				string jsonText = File.ReadAllText(filepath);
				cars[i].config = new CarConfig("car" + (i + 1).ToString(), jsonText);
			});
		}
	}
	public void AddCar()
	{
		tracks["car" + (1 + Mathf.RoundToInt(19 * UnityEngine.Random.value)).ToString()].unlocked = true;
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
		tracks.Add("JAP", new TrackHeader(0, 0, 4, Envir.JAP, null, new int[] { }, null, false));
		tracks.Add("GER", new TrackHeader(0, 0, 4, Envir.GER, null, new int[] { }, null, false));
		tracks.Add("SPN", new TrackHeader(0, 0, 4, Envir.SPN, null, new int[] { }, null, false));
		tracks.Add("FRA", new TrackHeader(0, 0, 4, Envir.FRA, null, new int[] { }, null, false));
		tracks.Add("ENG", new TrackHeader(0, 0, 4, Envir.ENG, null, new int[] { }, null, false));
		tracks.Add("USA", new TrackHeader(0, 0, 4, Envir.USA, null, new int[] { }, null, false));
		tracks.Add("ITA", new TrackHeader(0, 0, 4, Envir.ITA, null, new int[] { }, null, false));
		tracks.Add("MEX", new TrackHeader(0, 0, 4, Envir.MEX, null, new int[] { }, null, false));

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
				string json = JsonConvert.SerializeObject(tracks[name].records);
				File.WriteAllText(recordsPath, json);
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
		Debug.Log("set" + exposedParameter + " to level:" + toLogLevel.ToString());
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
	public string desc;
	/// <summary>
	/// whether the track can be raced on (has its path closed)
	/// </summary>
	public bool valid;
	public Envir envir;
	public CarGroup preferredCarClass;
	/// <summary>
	/// starts from 0 (sprites are from 4!)
	/// </summary>
	public int difficulty;//
	public bool unlocked;//
	public int[] icons;
	/// <summary>
	/// lap, race, stunt, drift 
	/// </summary>
	[NonSerialized]
	public TrackRecords records;

	public TrackHeader()
	{
		records = new();
	}
	public TrackHeader(int unlocked, CarGroup prefCarClass, int trackDifficulty,
		Envir envir, string author, int[] icons, string desc, bool valid = true)
		: this()
	{
		this.unlocked = unlocked > 0;
		this.preferredCarClass = prefCarClass;
		this.difficulty = trackDifficulty;
		this.envir = envir;
		this.author = author;
		this.desc = desc;
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
		this.desc = h.desc;
		this.valid = h.valid;
		this.icons = h.icons;
	}

	public int TrackOrigin
	{
		get { return (author == "Team17") ? 0 : 1; }
	}
	public bool IsOriginal
	{
		get { return TrackOrigin == 0; }
	}
}

public class Car
{
	public string desc;
	public string name;
	public CarGroup category;
	public CarConfig config;
	public int price;
	public Car(int price, CarGroup carClass, string name, string desc)
	{
		this.desc = desc;
		this.category = carClass;
		this.name = name;
		this.price = price;
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

		// Load a PNG or JPG image from disk to a Texture2D, assign this texture to a new sprite and return its reference

		Texture2D SpriteTexture = LoadTexture(FilePath);
		Sprite NewSprite = Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0, 0), PixelsPerUnit, 0, spriteType);

		return NewSprite;
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

