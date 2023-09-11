using System;
using System.Collections.Generic;
using UnityEngine;
using static Info;
public static class Info
{
	public enum Livery { Caltex, Rline, Mysuko, Titan, Itex, TGR, Special}
	public static readonly int Liveries = 7;
	public enum RaceType { Race, Stunt, Drift, Knockout, Survival};
	public static readonly int RaceTypes = 5;
	public enum Envir { GER, JAP, SPN, FRA, ENG, USA, ITA, MEX };
	public static readonly int Environments = 8;
	public enum CarGroup { Wild, Aero, Speed, Team };
	public enum TrackOrigin { Original, Custom};
	public static readonly string carImagesPath = "carImages/";
	public static readonly string trackImagesPath = "trackImages/";
	public static Dictionary<string, Car> cars;
	public static SortedDictionary<string, Track> tracks;

	// next session data
	public static CarSetup[] s_carSetups;
	public static string s_trackName;
	public static RaceType s_raceType = RaceType.Race;
	public static int s_laps = 3;
	public static bool s_isNight = false;
	public static int s_cpuLevel = 20;
	public static int s_rivals = 3;
	public static bool s_reversed = false;
	public static bool s_catchup = true;

	public static string[] flagNames =
	{
		"Stunty", "Loop", "Jumpy", "Windy", "Intersecting", "No_pit", "No_jumps", "Icy", "Sandy", "Offroad"
	};
	internal static Sprite[] icons;

	public static void PopulateCarsData()
	{
		if (cars == null)
			cars = new Dictionary<string, Car>();

		cars.Add("car01", new Car(0,CarGroup.Speed, "MEAN STREAK\n\nFast, light and agile, this racer offers much for those who wish to modify their vehicle."));
		cars.Add("car02", new Car(0,CarGroup.Wild, "THE HUSTLER\n\nSturdy 4x4 pick-up truck with an eye for the outrageous!"));
		cars.Add("car03", new Car(0,CarGroup.Aero, "TWIN EAGLE\n\nTake flight with this light and speedy stuntcar."));
		cars.Add("car04", new Car(0,CarGroup.Aero, "SKY HAWK\n\nGet airborne with this very versatile stunt car."));
		cars.Add("car05", new Car(0,CarGroup.Speed, "THE PHANTOM\n\nFast, sleek and tough to handle."));
		cars.Add("car06", new Car(0,CarGroup.Wild, "ROAD HOG\n\nRock and Roll with the rough ridin' road hog."));
		cars.Add("car07", new Car(0,CarGroup.Wild, "DUNE RAT\n\nDefy the laws of physics in this buggy."));
		cars.Add("car08", new Car(0,CarGroup.Speed, "NITRO LIGHTNIN''\n\nSupercharged super speed. Easy does it!"));
		cars.Add("car09", new Car(0,CarGroup.Speed, "ALLEY KAT\n\nSleek and powerful, this cat is ready to roar."));
		cars.Add("car10", new Car(0,CarGroup.Wild, "SAND SHARK\n\nThis beachcomber is at home on any stunt circuit."));
		cars.Add("car11", new Car(0,CarGroup.Wild, "THE BRUTE\n\nUnleash the Brute for no-nonsense on the road!"));
		cars.Add("car12", new Car(0,CarGroup.Aero, "WILD DART\n\nFly fast and true with this stuntcar."));
		cars.Add("car13", new Car(0,CarGroup.Wild, "RAGING BULL\n\nPowerful and fast, this streetwise 4x4 is incredible."));
		cars.Add("car14", new Car(0,CarGroup.Aero, "FLYING MANTIS\n\nSuper light and very fast."));
		cars.Add("car15", new Car(0,CarGroup.Aero, "STUNT MONKEY\n\nMonkey see, monkey do! Go bananas with this wild ride!"));
		cars.Add("car16", new Car(0,CarGroup.Speed, "INFERNO\n\nThis speed demon is on fire!"));
		cars.Add("car17", new Car(1,CarGroup.Team, "THE FORKSTER\n\nDespite its looks, it moves like fork lightning!"));
		cars.Add("car18", new Car(0,CarGroup.Team, "WORMS MOBILE\n\nSuper Speedy Buggy!"));
		cars.Add("car19", new Car(0,CarGroup.Team, "FORMULA 17\n\nIncredibly fast racing car."));
		cars.Add("car20", new Car(1,CarGroup.Team, "TEAM MACHINE\n\nThe ultimate, hugely versatile stock car."));
	}
	
	public static void AddCar()
	{
		tracks["car" + (1 + Mathf.RoundToInt(19 * UnityEngine.Random.value)).ToString()].unlocked = true;
	}
	public static void PopulateTrackData()
	{
		if (tracks == null)
			tracks = new SortedDictionary<string, Track>();
		// 0         1			2			3			4					5				6				7		8			9
		//"stunty", "loop", "jumpy", "windy", "intersecting", "no_pit", "no_jumps", "icy", "sandy", "offroad"
		//										unlock   preffered				   author            flags          
		tracks.Add("track01", new Track(1, (CarGroup)2,6, Envir.FRA, "vo", new int[] { 2 }, "CRAZY STRAIGHTS\n\nThis long speed track offers opportunity for a number of jump stunts."));
		tracks.Add("track02", new Track(1, (CarGroup)2,4, Envir.JAP, "vo", new int[] { 0 }, "BANK JOB\n\nThis short, speedy circuit offers a number of stunt opportunities and high-banks for sneaky overtaking."));
		tracks.Add("track03", new Track(1, (CarGroup)1,7, Envir.JAP, "vo", new int[] { 2 }, "TUBULAR HELL\n\nA long and winding track with many ramps. Try not to climb too high in the tubular sections!"));
		tracks.Add("track04", new Track(1, (CarGroup)2,6, Envir.FRA, "vo", new int[] { 2 }, "CURBED HEIGHTS\n\nA long, high track that is best navigated by hugging the racing line.."));
		tracks.Add("track05", new Track(1, (CarGroup)1,8, Envir.ITA, "vo", new int[] { 2 }, "FLYING FINISH\n\nA short, tough and dramatic track with a huge jump over the finish line!"));
		tracks.Add("track06", new Track(1, (CarGroup)1,7, Envir.SPN, "vo", new int[] { 2 }, "SECRET SIX\n\nAn exciting track with plenty of ramps and a cross-over."));
		tracks.Add("track07", new Track(1, (CarGroup)0,8, Envir.SPN, "vo", new int[] { 8 }, "THE SANDWINDER\n\nA huge, winding off-road track featuring a very bumpy mid section and multi-level turns."));
		tracks.Add("track08", new Track(1, (CarGroup)0,6, Envir.SPN, "vo", new int[] { 8,6 }, "ROUGHDUST FLATS\n\nThe only flat track in the original Stunt GP collection, this is far from a gentle experience!"));
		tracks.Add("track09", new Track(1, (CarGroup)1,7, Envir.ENG, "vo", new int[] { 2 }, "THE LOOPBACK\n\nA long, fast track with many opportunities for jumps and stunts."));
		tracks.Add("track10", new Track(1, (CarGroup)2,7, Envir.ENG, "vo", new int[] { 4 }, "INTERSECTOR\n\nA long, fast track with a multitude of mad crossovers!"));
		tracks.Add("track11", new Track(1, (CarGroup)1,6, Envir.SPN, null, new int[] { 2 }, "RISE'N'FALL\n\nHave you got the stomach for the massive climb and fall? Not to mention the loop!"));
		tracks.Add("track12", new Track(1, (CarGroup)2,5, Envir.SPN, null, new int[] { 0 }, "WIDE WALL CHASE\n\nA very fast night track where you can use the burns for over-taking."));
		tracks.Add("track13", new Track(1, (CarGroup)2,5, Envir.USA, null, new int[] { 3 }, "CURB CITY CIRCUIT\n\nA track of contrasts, straight outside and tricky inside."));
		tracks.Add("track14", new Track(1, (CarGroup)2,9, Envir.ITA, null, new int[] { 3 }, "WATERFRONT DASH\n\nThis long and winding track takes you all over the waterfront!"));
		tracks.Add("track15", new Track(1, (CarGroup)1,6, Envir.SPN, null, new int[] { 2 }, "FREEFALL FREEWAY\n\nTwo huge jumps and a mighty climb feature in this evening excursion."));
		tracks.Add("track16", new Track(1, (CarGroup)2,7, Envir.GER, null, new int[] { 3 }, "HIGH ROLLER\n\nThis complex, winding track has a number of high curved bends."));
		tracks.Add("track17", new Track(1, (CarGroup)0,9, Envir.JAP, null, new int[] { 8 }, "HIGHFLY CLEARWAY\n\nA large and difficult track with bumps, jumps and cross-overs."));
		tracks.Add("track18", new Track(1, (CarGroup)1,6, Envir.USA, null, new int[] { 2 }, "HELIPAD HEIGHTS\n\nThis interior and exterior track features a spectacular leap across a skyline!"));
		tracks.Add("track19", new Track(1, (CarGroup)0,6, Envir.SPN, null, new int[] { 8 }, "SUNKEN SIGHTS\n\nA three level sunken area and a high banked climb are the highlights of this dusty track."));
		tracks.Add("track20", new Track(1, (CarGroup)0,4, Envir.JAP, null, new int[] { 8 }, "DUST BUSTER\n\nThis fast, relatively flat track offers many ways to drive."));
		tracks.Add("track21", new Track(1, (CarGroup)1,8, Envir.GER, null, new int[] { 1 }, "TWINLOOP CIRCUIT\n\nTwo huge climbs and a double loop make this a formidable track."));
		tracks.Add("track22", new Track(1, (CarGroup)1,6, Envir.GER, null, new int[] { 1 }, "SNAKESTORM\n\nA long, winding track with multi-levels and a loop."));
		tracks.Add("track23", new Track(1, (CarGroup)2,4, Envir.USA, null, new int[] { 0 }, "SKYTOP SPEED CIRCUIT\n\nA very small, fast track with a couple of jumps and high banks."));
		tracks.Add("track24", new Track(1, (CarGroup)2,4, Envir.FRA, null, new int[] { 0 }, "THE CHRONOZONE\n\nA very fast track with burns and a very sharp turn."));

		// lap race stunt drift
		Track.Record[] records = new Track.Record[]
		{
			new Track.Record("Viatrufka", 87.3f, true),
			new Track.Record("T17", 187.3f, true),
			new Track.Record(null, 0, true),
			new Track.Record("Via", 3500, false),
		};
		tracks["track02"].records = records;
	}
	public static float InGroupPos(Transform child)
	{
		if (child.parent.childCount <= 1)
			return 0;
		return (float)child.GetSiblingIndex() / (child.parent.childCount - 1);
	}
}

public class Track
{
	public class Record
	{
		public string playerName;
		public float secondsOrPts;
		public float requiredSecondsOrPts;
		public bool isTime;
		public Record(string playerName, float secondsOrPts, bool isTime, float requiredSecondsOrPts = 1e6f)
		{
			this.playerName = playerName;
			this.secondsOrPts = secondsOrPts;
			this.requiredSecondsOrPts = requiredSecondsOrPts;
			this.isTime = isTime;
		}
	}

	public string author;
	public string desc;
	public bool valid;
	public Envir envir;
	public CarGroup preferredCarClass;//
	public int difficulty;//
	public bool unlocked;//
	public int[] flags;
	/// <summary>
	/// lap, race, stunt, drift 
	/// </summary>
	public Record[] records = new Record[4];

	public Track(int unlocked, CarGroup prefCarClass, int trackDifficulty, 
		Envir envir, string author, int[] flags, string desc, bool valid = true)
	{
		this.unlocked = unlocked > 0;
		this.preferredCarClass = prefCarClass;
		this.difficulty = trackDifficulty;
		this.envir = envir;
		this.author = author;
		this.desc = desc;
		this.valid = valid;
		this.flags = flags;
	}
	public int TrackOrigin()
	{
		return Convert.ToInt32(author != null); 
	}
}

public class Car
{
	public string desc;
	public CarGroup carClass;
	public float[] sgpBars;
	public bool unlocked;
	public Car(int unlocked, CarGroup carClass, string desc, float[] sgpBars = null)
	{
		this.desc = desc;
		this.carClass = carClass;
		if (sgpBars == null)
		{
			sgpBars = new float[3];
			for (int i = 0; i < 3; ++i)
				sgpBars[i] = UnityEngine.Random.value;
		}
		this.sgpBars = sgpBars;
		this.unlocked = unlocked > 0;
	}
}

public class CarSetup
{
	public string carName;
	public string driverName;
	public Livery livery;
}



