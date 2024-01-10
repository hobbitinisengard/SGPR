using Newtonsoft.Json;
using RVP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using static Info;

public static class Info
{
	public readonly static string documentsSGPRpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Stunt GP Reloaded\\";
	public readonly static string partsPath = documentsSGPRpath + "parts\\";
	public readonly static string tracksPath = documentsSGPRpath + "tracks\\";
	public readonly static string userdataPath = documentsSGPRpath + "userdata.json";
	public readonly static string lastPath = documentsSGPRpath + "path.txt";
	public static PlayerSettingsData ReadSettingsDataFromJson()
	{
		PlayerSettingsData settingsData;
		if (!File.Exists(userdataPath))
		{
			settingsData = new PlayerSettingsData();
			string serializedSettings = JsonConvert.SerializeObject(settingsData);
			File.WriteAllText(userdataPath, serializedSettings);
		}
		else
		{
			string playerSettings = File.ReadAllText(userdataPath);
			settingsData = JsonConvert.DeserializeObject<PlayerSettingsData>(playerSettings);
		}
		return settingsData;
	}
	public static void SaveSettingsDataToJson(in AudioMixer mainMixer)
	{
		var settingsData = new PlayerSettingsData()
		{
			lastPlayerName = s_playerName,
			musicVol = ReadMixerLevelLog("musicVol", mainMixer),
			sfxVol = ReadMixerLevelLog("sfxVol", mainMixer)
		};

		string jsonText = JsonConvert.SerializeObject(settingsData);
		File.WriteAllText(userdataPath, jsonText);
	}
	public static PartInfo[] partInfos = new PartInfo[]
	{
		new PartInfo("Itex", "suscfg"),
		new PartInfo("Mysuko", "bmscfg"),
		new PartInfo("Titan", "batcfg"),
		new PartInfo("Caltex", "engcfg"),
		new PartInfo("TGR", "chacfg"),
		new PartInfo("Itex", "grscfg"),
		new PartInfo("Mysuko", "jetcfg"),
		new PartInfo("Rline", "tyrcfg"),
		new PartInfo("TGR", "drvcfg"),
		new PartInfo("", "carcfg")
	};
	//public static string[] extensionsSuffixes = new string[] { "suscfg", "bmscfg", "batcfg",
	//		"engcfg", "chacfg", "grscfg", "jetcfg", "tyrcfg", "drvcfg", "carcfg" };
	public enum Livery { Special = 1, TGR, Rline, Itex, Caltex, Titan, Mysuko }
	public const int Liveries = 7;
	public enum RecordType { BestLap, RaceTime, StuntScore, DriftScore }
	public enum PavementType { Highway, RedSand, Asphalt, Electric, TimeTrial, Japanese, GreenSand, Random }
	/// <summary>
	/// valid pavements from 0 to ..
	/// </summary>
	public const int pavementTypes = 6;

	public enum RaceType { Race, Stunt, Drift, Knockout, Survival }
	public const int RaceTypes = 5;
	public enum Envir { GER, JAP, SPN, FRA, ENG, USA, ITA, MEX };
	public readonly static Vector3[] invisibleLevelDimensions = new Vector3[]{
		new (564, 1231,1), //ger
		new (800, 800,1), //jap
		new (1462, 1316,1),
		new (824, 817,1),
		new (1170, 817,1),
		new (739, 1060,1),
		new (1406, 1337,1),// ita
		new (564, 1231,1), //mex
	};
	public static readonly int[] skys = new int[] { 8, 2, 5, 1, 4, 3, 7, 9 };

	public const int Environments = 8;
	public static readonly string[] EnvirDescs =
	{
		"GERMANY\n\nLoud crowd cheering and powerful spotlights..This german arena is really a place to show off.",
		"JAPAN\n\nHere in this calm japanese dojo placed on the outskirts of Kyoto you can meditate or organize a race!",
		"SPAIN\n\nBeaches like this usually ooze holidays. This is not an exception: warm sand, palms, and sun.. What could people possibly want more? Maybe a RC car race :)",
		"FRANCE\n\nThis shadowy warehouse is full of boxes, forklifts and machinery. There are some really dark places here.",
		"ENGLAND\n\nEnglish go-kart track is a good location to test your driving skills. This place has a reputation for great races.",
		"USA\n\nAre you looking for an intense experience? Racing on top of a multistorey parking lot located in the heart of New York will be a bombastic idea!",
		"ITALY\n\nFeeling mediterranean? This italian coast is very scenic, especially at night. There are two dangers here to look out however: staircase descent and water!",
		"MEXICO\n\nOnly some people are in the possession of info that there's this ancient place located in the middle of an unknown mexican forest, where aztecs used to race RC-cars. However no-one really knows how to get there."
	};
	public enum CarGroup { Wild, Aero, Speed, Team };
	public enum TrackOrigin { Original, Custom };
	public static readonly string carModelsPath = "carModels/";
	public static readonly string carImagesPath = "carImages/";
	public static readonly string trackImagesPath = "trackImages/";
	public static readonly string editorTilesPath = "tiles/objects/";
	public static Vector3[] carSGPstats;
	public static Car[] cars;
	public static Dictionary<string, PartSavable> carParts;
	public static SortedDictionary<string, TrackHeader> tracks;
	public static Dictionary<string, AudioClip> audioClips;
	public static DateTime raceStartDate = DateTime.MinValue;
	public static bool loaded = false;
	public const int roadLayer = 6;
	public const int ignoreWheelCastLayer = 8;
	public const int vehicleLayer = 9;
	public const int connectorLayer = 11;
	public const int invisibleLevelLayer = 12;
	public const int terrainLayer = 13;
	public const int cameraLayer = 14;
	public const int flagLayer = 15;
	public static readonly int[] racingLineLayers = new int[] {16,25,27};
	public const int pitsLineLayer = 17;
	public const int pitsZoneLayer = 18;
	public const int grindTrigger = 19;
	public const int ghostLayer = 24;
	public const int trailLayer = 25;

	public const int firstExternalSurface = 4;

	/// <summary>
	/// Only one object at the time can have this layer
	/// </summary>
	public const int selectionLayer = 20;

	// curr/next session data
	public static bool s_spectator;
	public static List<VehicleParent> s_cars = new List<VehicleParent>();
	public static string s_trackName = "USA";
	public static string s_playerCarName = "car01";
	public static string s_playerName = "P1";
	public static RaceType s_raceType = RaceType.Race;
	public static int s_laps = 3;
	public static bool s_inEditor = true;
	public static bool s_isNight = false;
	public static int s_cpuLevel = 0;
	public static int s_rivals = 0; // 0-9
	public static PavementType s_roadType = PavementType.Random;
	public static bool s_catchup = true;
	public static int s_resultPos = 3;

	public static readonly string[] IconNames =
	{
		"Stunty", "Loop", "Jumpy", "Windy", "Intersecting", "No_pits", "No_jumps", "Icy", "Sandy", "Offroad"
	};
	public static Sprite[] icons;
	public static bool gamePaused;
	public static readonly string version = "0.2b";
	public static Car Car(string name)
	{ // i.e. car05
		int i = int.Parse(name[3..]);
		return cars[i - 1];
	}
	public static void ReloadCarPartsData()
	{
		string[] extensionsSuffixes = Info.partInfos.Select(i => i.fileExtension).ToArray();
		string[] filepaths = Directory.GetFiles(Info.partsPath)
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
	public static void ReloadCarsData()
	{
		if (cars == null)
		{
			cars = new Car[]
			{
			new Car(1,CarGroup.Speed, "MEAN STREAK\n\nFast, light and agile, this racer offers much for those who wish to modify their vehicle."),
			new Car(1,CarGroup.Wild, "THE HUSTLER\n\nSturdy 4x4 pick-up truck with an eye for the outrageous!"),
			new Car(1,CarGroup.Aero, "TWIN EAGLE\n\nTake flight with this light and speedy stuntcar."),
			new Car(1,CarGroup.Aero, "SKY HAWK\n\nGet airborne with this very versatile stunt car."),
			new Car(1,CarGroup.Speed, "THE PHANTOM\n\nFast, sleek and tough to handle."),
			new Car(1,CarGroup.Wild, "ROAD HOG\n\nRock and Roll with the rough ridin' road hog."),
			new Car(1,CarGroup.Wild, "DUNE RAT\n\nDefy the laws of physics in this buggy."),
			new Car(1,CarGroup.Speed, "NITRO LIGHTNIN''\n\nSupercharged super speed. Easy does it!"),
			new Car(1,CarGroup.Speed, "ALLEY KAT\n\nSleek and powerful, this cat is ready to roar."),
			new Car(1,CarGroup.Wild, "SAND SHARK\n\nThis beachcomber is at home on any stunt circuit."),
			new Car(0,CarGroup.Wild, "THE BRUTE\n\nUnleash the Brute for no-nonsense on the road!"),
			new Car(0,CarGroup.Aero, "WILD DART\n\nFly fast and true with this stuntcar."),
			new Car(0,CarGroup.Wild, "RAGING BULL\n\nPowerful and fast, this streetwise 4x4 is incredible."),
			new Car(0,CarGroup.Aero, "FLYING MANTIS\n\nSuper light and very fast."),
			new Car(0,CarGroup.Aero, "STUNT MONKEY\n\nMonkey see, monkey do! Go bananas with this wild ride!"),
			new Car(0,CarGroup.Speed, "INFERNO\n\nThis speed demon is on fire!"),
			new Car(0,CarGroup.Team, "THE FORKSTER\n\nDespite its looks, it moves like fork lightning!"),
			new Car(0,CarGroup.Team, "WORMS MOBILE\n\nSuper Speedy Buggy!"),
			new Car(0,CarGroup.Team, "FORMULA 17\n\nIncredibly fast racing car."),
			new Car(0,CarGroup.Team, "TEAM MACHINE\n\nThe ultimate, hugely versatile stock car.")
			};
		}
		ReloadCarConfigs();
	}
	public static void ReloadCarConfigs()
	{
		string carSuffix = partInfos[^1].fileExtension;
		string[] filepaths = Directory.GetFiles(Info.partsPath)
			.Where(filepath => filepath.ToLower().EndsWith(carSuffix))
			.ToArray();

		for (int i = 0; i < cars.Length; ++i)
		{
			string filepath = Info.partsPath + "car" + (i + 1).ToString() + "." + partInfos[^1].fileExtension;
			string jsonText = File.ReadAllText(filepath);
			cars[i].config = new CarConfig(null, null, jsonText);
		}
	}
	public static void AddCar()
	{
		tracks["car" + (1 + Mathf.RoundToInt(19 * UnityEngine.Random.value)).ToString()].unlocked = true;
	}
	public static void PopulateTrackData()
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
			TrackHeader header = JsonConvert.DeserializeObject<TrackHeader>(trackJson);
			tracks.Add(name, header);
		}
	}
	public static float ReadMixerLevelLog(string exposedParameter, AudioMixer mixer)
	{
		mixer.GetFloat(exposedParameter, out float inVal);
		return Mathf.Pow(10, 3 / 160f * inVal);
	}
	public static void SetMixerLevelLog(string exposedParameter, float val01, in AudioMixer mixer)
	{
		float toLogLevel = 80 * 2 / 3f * Mathf.Log10(val01);
		if (toLogLevel < -80)
			toLogLevel = -80;
		mixer.SetFloat(exposedParameter, toLogLevel);
	}
	public static float InGroupPos(Transform child)
	{
		if (child.parent.childCount <= 1)
			return 0;
		return (float)child.GetSiblingIndex() / (child.parent.childCount - 1);
	}
	internal static void PopulateSFXData()
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
	public static string LoadLastFolderPath()
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
	public static void SaveLastFolderPath(string path)
	{
		if (path == null)
		{
			Debug.LogError("path null");
			return;
		}
		Debug.Log(path);
		StreamWriter w = new StreamWriter(lastPath);
		w.WriteLine(path);
		w.Close();
	}
	public static string ToLaptimeStr(this TimeSpan t)
	{
		string shortForm = "";
		if (t.Hours > 0)
			shortForm += string.Format("{0:D2}.", t.Hours);
		shortForm += string.Format("{0:D2}:{1:D2}.{2:D2}", t.Minutes, t.Seconds, Mathf.RoundToInt(t.Milliseconds / 10f));
		return shortForm;
	}
}
[Serializable]
public class TrackHeader
{
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
		public static Record[] RecordTemplate()
		{
			return new Record[]
			{
				new Record(null, 35999,35999),
				new Record(null, 0),
				new Record(null, 0),
				new Record(null, 0),
			};
		}
	}

	public string author;
	public string desc;
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
	public Record[] records;

	public TrackHeader()
	{
		// lap race stunt drift
		records = new Record[]
		{
			new Record(null, 3599),
			new Record(null, 0),
			new Record(null, 0),
			new Record(null, 0)
		};
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
	public int TrackOrigin()
	{
		return (author != "Team17") ? 1 : 0;
	}
}

public class Car
{
	public string desc;
	public CarGroup category;
	public CarConfig config;
	public bool unlocked;
	public Car(int unlocked, CarGroup carClass, string desc)
	{
		this.desc = desc;
		this.category = carClass;
		//if (sgpBars == null)
		//{
		//	sgpBars = new float[3];
		//	for (int i = 0; i < 3; ++i)
		//		sgpBars[i] = UnityEngine.Random.value;
		//}
		//this.sgpBars = sgpBars;
		this.unlocked = unlocked > 0;
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

