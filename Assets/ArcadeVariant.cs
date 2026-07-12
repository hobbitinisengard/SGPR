using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static ArcadeVariant.Prize;

[Serializable]
public class ArcadeVariant
{
	public string name;
	[JsonIgnore]
	public Progress progress;
	public Node[] nodes;
	public Prize[] globalPrizes;
	public StartPos[] starts;

	[Serializable]
	public class Progress
	{
		[JsonIgnore]
		public ArcadeVariant parent;

		public List<List<int>> pathsDone;
		public BitField32[] prizesCompleted;
		public BitField32 globalPrizesCompleted;
		public LinkedList<RankingRowData> rankingRows = new();
		public Progress()
		{}
		public Progress(ArcadeVariant parent)
		{
			this.parent = parent;
			int nodes = parent.nodes.Length;
			prizesCompleted = new BitField32[nodes];
			pathsDone = new List<List<int>>(nodes);
			for (int i = 0; i < nodes; ++i)
				pathsDone.Add(new List<int>());
		}
		public float OverallProgressPerc()
		{
			float progress = 0;
			progress += pathsDone.Sum(p => p.Count);
			progress += prizesCompleted.Sum(p => p.CountBits());
			progress += globalPrizesCompleted.CountBits();
			progress /= parent.nodes.Sum(p => p.prizeReqs.Length) + parent.nodes.Sum(n => n.connections.Length) + parent.globalPrizes.Length;
			return progress * 100;
		}
	}
	[Serializable]
	public class StartPos
	{
		public int node;
		public int[] allowedCarsIdxs;
	}
	[Serializable]
	public class Node
	{
		public int id;
		public int[] connections; // ids of other nodes
		public Color32 color;
		public float size;
		public Vector2 coords;

		public string trackName;
		public int laps = 1;
		public RaceType raceType;
		public PavementType pavementType;

		public CarPlacement[] cars;

		public Prize[] prizeReqs;
		public Prize continuationReq;
	}

	[Serializable]
	public class Prize
	{
		public string name;
		public Condition condition;
		public string conditionArgument; // e.g. "3" for position, "120" for time in seconds, "00:01:30.000" for fastest laptime, "1000" for stunt score, "5" for aero stars, etc.

		public enum Condition
		{
			PositionAtLeast,
			LapAtMost,
			StarsAtLeast,
			StuntAtLeast,
			DriftsAtLeast,
			TimeAtMost,
			FastestLaptime,
			AlwaysFirst,//
			AllPathsFound,//
			ExactStunts,
		}
	}
	public static ArcadeVariant GenerateCommunityVariant()
	{
		ArcadeVariant communityVariant = new()
		{
			name = "Community",
			globalPrizes = new Prize[] { new() { name = "car18,car19", condition = Prize.Condition.AllPathsFound } },
			starts = new StartPos[]
				{
					new(){node = 0, allowedCarsIdxs = new int[]{1,8,14}},
					new(){node = 1, allowedCarsIdxs = new int[]{1,6,14}},
				},
			nodes = new Node[]
				{
				new()
				{
					id = 0,
					connections = new int[] { 2,3 },
					color = Color.blue,
					size = 1,
					coords = new Vector2(5,0),
					trackName = "SPECTACULAR STEPS",
					laps = 3,
					cars = new CarPlacement[]
					{
						new() { name = "CP1", carIdx = 10, livery = Livery.Titan, },
						new() { name = "CP2", carIdx = 09, livery = Livery.Rline, },
						new() { name = "CP3", carIdx = 06, livery = Livery.Mysuko, },
						new() { name = "CP4", carIdx = 05, livery = Livery.Caltex, },
						new() { name = "CP5", carIdx = 01, livery = Livery.Titan, },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SPECTACULAR STEPS" } },//new Prize[]{ },
	        pavementType = (PavementType)0,
				},
				new()
				{
						id = 1,
						connections = new int[] { 4,5 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(5,6),
						trackName = "BOLD STRAIGHTS",
						laps = 2,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 7, livery = Livery.Caltex,  },
										new() { name = "CP2", carIdx = 7, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 7, livery = Livery.Itex,  },
										new() { name = "CP4", carIdx = 7, livery = Livery.Mysuko,  },
										new() { name = "CP5", carIdx = 7, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car07,BOLD STRAIGHTS" } },
						pavementType = (PavementType)1,
				},
				new()
				{
						id = 2,
						connections = new int[] { 6,7 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(2.5f,1),
						trackName = "JAPANESE BRIDGE",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 11, livery = Livery.TGR,  },
										new() { name = "CP2", carIdx = 11, livery = Livery.Caltex,  },
										new() { name = "CP3", carIdx = 11, livery = Livery.Itex,  },
										new() { name = "CP4", carIdx = 11, livery = Livery.Rline,  },
										new() { name = "CP5", carIdx = 11, livery = Livery.Mysuko,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.StarsAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "JAPANESE BRIDGE" },
						new() { condition = Prize.Condition.StarsAtLeast, conditionArgument = "3", name = "spn5"}
						},
						pavementType = (PavementType)2,
				},
				new()
				{
						id = 3,
						connections = new int[] { 8,9 },
						color = Color.green,
						size = 1,
						coords = new Vector2(7.5f,1),
						trackName = "ROUTE 45",
						laps = 3,
						cars = new CarPlacement[]
						{
							new() { name = "CP1", carIdx = 5, livery = Livery.Titan,  },
							new() { name = "CP2", carIdx = 5, livery = Livery.Rline,  },
							new() { name = "CP3", carIdx = 5, livery = Livery.Mysuko,  },
							new() { name = "CP4", carIdx = 5, livery = Livery.Caltex,  },
							new() { name = "CP5", carIdx = 5, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "ROUTE 45" } },
						pavementType = (PavementType)3,
				},
				new()
				{
						id = 4,
						connections = new int[] { 10,11 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(2.5f,5),
						trackName = "SHADY TURNS",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 18, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 18, livery = (Livery)2,  },
										new() { name = "CP3", carIdx = 16, livery = (Livery)3,  },
										new() { name = "CP4", carIdx = 16, livery = (Livery)3,  },
										new() { name = "CP5", carIdx = 07, livery = (Livery)4,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.FastestLaptime,conditionArgument = "3"},
						prizeReqs = new Prize[] { 
							new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SHADY TURNS" },
							new() { condition = Prize.Condition.LapAtMost, conditionArgument = "01:15.00", name = "spn4"}
						},
						pavementType = (PavementType)4,
				},
				new()
				{
						id = 5,
						connections = new int[] { 12,13 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(7.5f,5),
						trackName = "THE HEIST",
						laps = 1,
						cars = new CarPlacement[]
						{
						},
						continuationReq = new Prize(){condition = Prize.Condition.DriftsAtLeast,conditionArgument = "750"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.DriftsAtLeast, conditionArgument = "2000", name = "THE HEIST" } },
						pavementType = (PavementType)5,
						raceType = RaceType.Drift,
				},
				new()
				{
						id = 6,
						connections = new int[] { 18,14 },
						color = Color.green,
						size = 1,
						coords = new Vector2(1,1.5f),
						trackName = "CARELLO ELEVATORE",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 17, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 17, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 17, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 10, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 10, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CARELLO ELEVATORE" } },
						pavementType = (PavementType)6,
				},
				new()
				{
						id = 7,
						connections = new int[] { 14,22 },
						color = Color.green,
						size = 1,
						coords = new Vector2(3.5f,1.5f),
						trackName = "HAWK SPACE",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 04, livery = (Livery)7,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 03, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 03, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 04, livery = (Livery)4,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.StarsAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] {
							new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car03,HAWK SPACE" },
							new() { condition = Prize.Condition.ExactStunts, conditionArgument = "SIDE RAILGRIND,2", name = "spn3"}},
						pavementType = (PavementType)0,
				},
				new()
				{
						id = 8,
						connections = new int[] { 22,15 },
						color = Color.green,
						size = 1,
						coords = new Vector2(6.5f,1.5f),
						trackName = "ROAD TO THE TOP II",
						laps = 2,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 15, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 4, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 0, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 4, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 12, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car04,ROAD TO THE TOP II" } },
						pavementType = (PavementType)1,
				},
				new()
				{
						id = 9,
						connections = new int[] { 15,19 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(9f,1.5f),
						trackName = "DUNE ZONE",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car05,DUNE ZONE" } },
						pavementType = (PavementType)2,
				},
				new()
				{
						id = 10,
						connections = new int[] { 20,16 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(1,4.5f),
						trackName = "MIND YOUR STEP",
						laps = 2,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)3,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)7,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car06,MIND YOUR STEP" } },
						pavementType = (PavementType)3,
				},
				new()
				{
						id = 11,
						connections = new int[] { 16,22 },
						color = Color.green,
						size = 1,
						coords = new Vector2(3.5f,4.5f),
						trackName = "ROOFTOP PARTY",
						laps = 2,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car07,ROOFTOP PARTY" } },
						pavementType = (PavementType)4,
				},
				new()
				{
						id = 12,
						connections = new int[] { 22,17 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(6.5f,4.5f),
						trackName = "SANDY RAMPS",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = (Livery)5,  },
										new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 05, livery = (Livery)4,  },
										new() { name = "CP5", carIdx = 01, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { 
							new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car08,SANDY RAMPS" },
							new() {condition = Prize.Condition.StarsAtLeast, conditionArgument="7", name = "spn2"}},
						pavementType = (PavementType)5,
				},
				new()
				{
						id = 13,
						connections = new int[] { 17,21 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(9,4.5f),
						trackName = "WAREHOUSE ROCK",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 0, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 1, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 2, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 3, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 4, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car09,WAREHOUSE ROCK" } },
						pavementType = (PavementType)6,
				},
				new()
				{
						id = 14,
						connections = new int[] { 18,22 },
						color = Color.green,
						size = 1,
						coords = new Vector2(1.5f,2),
						trackName = "TRICK ALLEY",
						laps = 2,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car10,TRICK ALLEY" } },
						pavementType = (PavementType)0,
				},
				new()
				{
						id = 15,
						connections = new int[] { 22,19 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(8.5f,2),
						trackName = "GLASSY HIGHWAY",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { 
							new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car11,GLASSY HIGHWAY" },
							new() { condition = Prize.Condition.ExactStunts, conditionArgument = "SIDE RAILGRIND,2", name = "spn4"}
						},
						pavementType = (PavementType)1,
				},
				new()
				{
						id = 16,
						connections = new int[] { 20,22 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(1.5f,4),
						trackName = "ROAD TO THE TOP",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "spn7,car12,ROAD TO THE TOP" } },
						pavementType = (PavementType)2,
				},
				new()
				{
						id = 17,
						connections = new int[] { 22,21 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(8.5f,4),
						trackName = "TIGER'S PREY",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { 
							new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car13,TIGER'S PREY" },
							new() { condition = Prize.Condition.FastestLaptime, name = "spn2" }
						},
						pavementType = (PavementType)3,
				},
				new()
				{
						id = 18,
						connections = new int[] { 22 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(0.5f,2.5f),
						trackName = "TITANIC WORK",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 1, livery = Livery.Titan,  },
										new() { name = "CP2", carIdx = 2, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 3, livery = Livery.Mysuko,  },
										new() { name = "CP4", carIdx = 4, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 4, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car14,TITANIC WORK" },
						new() { condition = Prize.Condition.ExactStunts, conditionArgument = "SIDE RAILGRIND,1", name = "spn6"}
						},
						pavementType = (PavementType)4,
				},
				new()
				{
						id = 19,
						connections = new int[] { 22 },
						color = Color.green,
						size = 1,
						coords = new Vector2(9.5f,2.5f),
						trackName = "UNDER CONSTRUCTION",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 2, livery = Livery.Titan,  },
										new() { name = "CP2", carIdx = 4, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 6, livery = Livery.Mysuko,  },
										new() { name = "CP4", carIdx = 8, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 10, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { 
							new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "spn4,car15,UNDER CONSTRUCTION" },
							new() { condition = Prize.Condition.TimeAtMost, conditionArgument = "00:44.00", name = "spn7"}
						},
						pavementType = (PavementType)5,
				},
				new()
				{
						id = 20,
						connections = new int[] { 22 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(0.5f,3.5f),
						trackName = "GRASSY HILLS",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = Livery.Titan,  },
										new() { name = "CP2", carIdx = 09, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 08, livery = Livery.Mysuko,  },
										new() { name = "CP4", carIdx = 07, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 06, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "spn3,car16,GRASSY HILLS" } },
						pavementType = (PavementType)6,
				},
				new()
				{
						id = 21,
						connections = new int[] { 22 },
						color = Color.green,
						size = 1,
						coords = new Vector2(9.5f,3.5f),
						trackName = "FULL LOT",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 5, livery = Livery.Titan,  },
										new() { name = "CP2", carIdx = 10, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 15, livery = Livery.Mysuko,  },
										new() { name = "CP4", carIdx = 10, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 5, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "spn2,car17,FULL LOT" } },
						pavementType = (PavementType)0,
				},
				new()
				{
						id = 22,
						connections = new int[] { },
						color = F.I.red,
						size = 2,
						coords = new Vector2(5,3),
						trackName = "WHIRLWIND",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 15, livery = Livery.Team,  },
										new() { name = "CP2", carIdx = 15, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 15, livery = Livery.Team,  },
										new() { name = "CP4", carIdx = 15, livery = Livery.Rline,  },
										new() { name = "CP5", carIdx = 15, livery = Livery.Team,  },
										new() { name = "CP6", carIdx = 15, livery = Livery.Rline,  },
										new() { name = "CP7", carIdx = 15, livery = Livery.Team,  },
										new() { name = "CP8", carIdx = 15, livery = Livery.Rline,  },
										new() { name = "CP9", carIdx = 15, livery = Livery.Team,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "spn1,car18,WHIRLWIND" } },
						pavementType = (PavementType)1,
						raceType = RaceType.Race,
				},
			}
		};
		return communityVariant;
	}
	public static ArcadeVariant GenerateProtoVariant()
	{
		ArcadeVariant reversedVariant = new()
		{
			name = "Prototype",
			globalPrizes = new Prize[] { new() { name = "car16,car17,AZTEC", condition = Prize.Condition.AllPathsFound},
								new() {name = "car12,car18,car19", condition = Prize.Condition.AlwaysFirst} },
			starts = new StartPos[]
				{
						new() {node = 0, allowedCarsIdxs = new int[]{ 0,1 } },
				},
			nodes = new Node[]
				{
				new() {
						id = 0,
						connections = new int[] { 3,1,2,4 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(5,3),
						trackName = "RISE N FALL BETA",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = Livery.Titan,  },
										new() { name = "CP2", carIdx = 09, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 06, livery = Livery.Mysuko,  },
										new() { name = "CP4", carIdx = 05, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 01, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "RISE N FALL BETA" } },
						pavementType = (PavementType)5,
				},
				new() {
						id = 1,
						connections = new int[] { 3,5,9 },
						color = Color.green,
						size = 1,
						coords = new Vector2(4,4),
						trackName = "THE SECRET FIVE",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = Livery.TGR,  },
										new() { name = "CP2", carIdx = 03, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 02, livery = Livery.Itex,  },
										new() { name = "CP4", carIdx = 13, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 03, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "THE SECRET FIVE" } },//new Prize[]{ },
	pavementType = (PavementType)6,
				},
				new() {
						id = 2,
						connections = new int[] { 10,6,4 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(6,4),
						trackName = "CRONOZONE",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 08, livery = Livery.TGR,  },
										new() { name = "CP2", carIdx = 00, livery = Livery.Caltex,  },
										new() { name = "CP3", carIdx = 07, livery = Livery.Itex,  },
										new() { name = "CP4", carIdx = 08, livery = Livery.Rline,  },
										new() { name = "CP5", carIdx = 07, livery = Livery.Mysuko,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CRONOZONE" } },
						pavementType = (PavementType)5,
				},
				new() {
						id = 3,
						connections = new int[] { 20,8 },
						color = Color.green,
						size = 1,
						coords = new Vector2(4,2),
						trackName = "ENGLISH JAM",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = (Livery)6,  },
										new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 01, livery = (Livery)7,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "ENGLISH JAM" } },
						pavementType = (PavementType)1,
				},
				new() {
						id = 4,
						connections = new int[] { 7,11 },
						color = Color.green,
						size = 1,
						coords = new Vector2(6,2),
						trackName = "ITALIAN ICON",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "ITALIAN ICON" } },
						pavementType = (PavementType)0,
				},
				new() {
						id = 5,
						connections = new int[] { 13,9},
						color = Color.blue,
						size = 1,
						coords = new Vector2(3,4),
						trackName = "BANK JOB REVERSE",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car08,BANK JOB REVERSE" } },
						pavementType = (PavementType)1,
				},
				new() {
						id = 6,
						connections = new int[] { 10,14 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(7,4),
						trackName = "CRAZY STRAIGHTS REVERSE",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CRAZY STRAIGHTS REVERSE" } },
						pavementType = (PavementType)2,
				},
				new() {
						id = 7,
						connections = new int[] { 11,15 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(7,2),
						trackName = "CURBED HEIGHTS REVERSE",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = (Livery)7,  },
										new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 01, livery = (Livery)4,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CURBED HEIGHTS REVERSE" } },
						pavementType = (PavementType)3,
				},
				new() {
						id = 8,
						connections = new int[] { 20,12,5},
						color = Color.green,
						size = 1,
						coords = new Vector2(3,2),
						trackName = "INTERSECTOR BETA",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 0, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 1, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 2, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 3, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 4, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car04,INTERSECTOR BETA" } },
						pavementType = (PavementType)4,
				},
				new() {
						id = 9,
						connections = new int[] { 13 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(3,5),
						trackName = "SANDWINDER REVERSE",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car05,SANDWINDER REVERSE" } },
						pavementType = (PavementType)5,
				},
				new() {
						id = 10,
						connections = new int[] { 14 },
						color = Color.green,
						size = 1,
						coords = new Vector2(7,5),
						trackName = "SECRET SIX",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)3,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)7,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SECRET SIX" }  },
						pavementType = (PavementType)6,
				},
				new() {
						id = 11,
						connections = new int[] {15},
						color = Color.blue,
						size = 1,
						coords = new Vector2(7,1),
						trackName = "SNAKESTORM REVERSE",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs =   new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SNAKESTORM REVERSE" }  },
						pavementType = (PavementType)0,
				},
				new() {
						id = 12,
						connections = new int[]{ 16,13 },
						color = Color.green,
						size = 1,
						coords = new Vector2(2,1),
						trackName = "TUBULAR HELL",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = (Livery)5,  },
										new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 05, livery = (Livery)4,  },
										new() { name = "CP5", carIdx = 01, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "TUBULAR HELL" } },
						pavementType = (PavementType)1,
				},
				new() {
						id = 13,
						connections = new int[]{ 17},
						color = Color.green,
						size = 1,
						coords = new Vector2(2,5),
						trackName = "TUBULAR HELL REVERSE",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 0, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 1, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 2, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 3, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 4, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "TUBULAR HELL REVERSE" } },
						pavementType = (PavementType)2,
				},
				new() {
						id = 14,
						connections = new int[]{ 15,18},
						color = Color.green,
						size = 1,
						coords = new Vector2(8,5),
						trackName = "CURB CITY CIRCUIT II",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car15,CURB CITY CIRCUIT II" }},
						pavementType = (PavementType)3,
				},
				new() {
						id = 15,
						connections = new int[]{ 19},
						color = Color.green,
						size = 1,
						coords = new Vector2(8,1),
						trackName = "THE XING",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car02,THE XING" } },
						pavementType = (PavementType)4,
				},
				new() {
						id = 16,
						connections = new int[]{ },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(1,1),
						trackName = "CURB CITY CIRCUIT I",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CURB CITY CIRCUIT I" } },
						pavementType = (PavementType)5,
				},
				new() {
						id = 17,
						connections = new int[]{ },
						color = Color.blue,
						size = 1,
						coords = new Vector2(1,5),
						trackName = "HIGH ROLLER REVERSE",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car13,HIGH ROLLER REVERSE" } },
						pavementType = (PavementType)6
				},
				new() {
						id = 18,
						connections = new int[]{ },
						color = Color.blue,
						size = 1,
						coords = new Vector2(9,5),
						trackName = "BANK JOB BETA",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car10,BANK JOB BETA" } },
						pavementType = (PavementType)0
				},
				new() {
						id = 19,
						connections = new int[]{ },
						color = Color.blue,
						size = 1,
						coords = new Vector2(9,1),
						trackName = "RISE'N'FALL",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car01,RISE'N'FALL" } },
						pavementType = (PavementType)1
				},
				new() {
						id = 20,
						connections = new int[]{ 12},
						color = Color.blue,
						size = 1,
						coords = new Vector2(3,1),
						trackName = "FREEFALL FREEWAY REVERSE",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 14, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 13, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 12, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car11,FREEFALL FREEWAY REVERSE" } },
						pavementType = (PavementType)2
				}
				}
		};
		return reversedVariant;
	}
	public static ArcadeVariant GenerateOriginalVariant()
	{
		ArcadeVariant defaultVariant = new()
		{
			name = "Original",
			globalPrizes = new Prize[] { new() { name = "car11,car12,car16,car17,car18,car19", condition = Prize.Condition.AllPathsFound } },
			starts = new StartPos[]
				{
								new() { node = 15, allowedCarsIdxs = new int[]{6,09,10 } },
								new() { node = 16, allowedCarsIdxs = new int[]{3,14 } },
								new() { node = 17, allowedCarsIdxs = new int[]{0,8 } },
				},
			nodes = new Node[]
				{
				new() {
						id = 15,
						connections = new int[] { 11, 12 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(3,0),
						trackName = "DUST BUSTER",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = Livery.Titan,  },
										new() { name = "CP2", carIdx = 09, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 06, livery = Livery.Mysuko,  },
										new() { name = "CP4", carIdx = 05, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 01, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
						prizeReqs = new Prize[] {
							new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "DUST BUSTER" },

						},
						pavementType = (PavementType)5,
				},
				new() {
						id = 16,
						connections = new int[] { 12, 13 },
						color = Color.green,
						size = 1,
						coords = new Vector2(5,0),
						trackName = "SECRET SIX",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = Livery.TGR,  },
										new() { name = "CP2", carIdx = 03, livery = Livery.Rline,  },
										new() { name = "CP3", carIdx = 02, livery = Livery.Itex,  },
										new() { name = "CP4", carIdx = 13, livery = Livery.Caltex,  },
										new() { name = "CP5", carIdx = 03, livery = Livery.Titan,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
						prizeReqs = new Prize[]{new Prize(){condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SECRET SIX" } },
						pavementType = (PavementType)6,
				},
				new() {
						id = 17,
						connections = new int[] { 13, 14 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(7,0),
						trackName = "BANK JOB",
						laps = 3,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 08, livery = Livery.TGR,  },
										new() { name = "CP2", carIdx = 00, livery = Livery.Caltex,  },
										new() { name = "CP3", carIdx = 07, livery = Livery.Itex,  },
										new() { name = "CP4", carIdx = 08, livery = Livery.Rline,  },
										new() { name = "CP5", carIdx = 07, livery = Livery.Mysuko,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
						prizeReqs = new Prize[]{ },
						pavementType = (PavementType)5,
				},
				new() {
						id = 11,
						connections = new int[] { 6,7 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(2,1),
						trackName = "SANDWINDER",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = (Livery)6,  },
										new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 01, livery = (Livery)7,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[]{new (){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1", name="SANDWINDER" } },
						pavementType = (PavementType)1,
				},
				new() {
						id = 12,
						connections = new int[] { 7,8 },
						color = Color.green,
						size = 1,
						coords = new Vector2(4,1),
						trackName = "SNAKESTORM",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SNAKESTORM" } },
						pavementType = (PavementType)0,
				},
				new() {
						id = 13,
						connections = new int[] { 8,9 },
						color = Color.green,
						size = 1,
						coords = new Vector2(6,1),
						trackName = "THE LOOPBACK",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "THE LOOPBACK" } },
						pavementType = (PavementType)2,
				},
				new() {
						id = 14,
						connections = new int[] { 9,10 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(8,1),
						trackName = "CURBED HEIGHTS",
						laps = 4,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CURBED HEIGHTS" } },
						pavementType = (PavementType)2,
				},
				new() {
						id = 6,
						connections = new int[] { 0,1 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(1,2),
						trackName = "SUNKEN SIGHTS",
						laps = 5,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = (Livery)7,  },
										new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 01, livery = (Livery)4,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SUNKEN SIGHTS" } },
						pavementType = (PavementType)2,
				},
				new() {
						id = 7,
						connections = new int[] { 1,2 },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(3,2),
						trackName = "HELIPAD HEIGHTS",
						laps = 5,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 0, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 1, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 2, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 3, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 4, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "HELIPAD HEIGHTS" } },
						pavementType = (PavementType)2,
				},
				new() {
						id = 8,
						connections = new int[] { 2,3 },
						color = Color.green,
						size = 1,
						coords = new Vector2(5,2),
						trackName = "FREEFALL FREEWAY",
						laps = 5,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "FREEFALL FREEWAY" } },
						pavementType = (PavementType)2,
				},
				new() {
						id = 9,
						connections = new int[] { 3,4 },
						color = Color.blue,
						size = 1,
						coords = new Vector2(7,2),
						trackName = "HIGH ROLLER",
						laps = 5,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)3,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)7,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "HIGH ROLLER" }  },
						pavementType = (PavementType)0,
				},
				new() {
						id = 10,
						connections = new int[] {4,5},
						color = Color.blue,
						size = 1,
						coords = new Vector2(9,2),
						trackName = "CRAZY STRAIGHTS",
						laps = 5,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
						prizeReqs =     new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CRAZY STRAIGHTS" }  },
						pavementType = (PavementType)5,
				},
				new() {
						id = 0,
						connections = new int[]{ },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(0,3),
						trackName = "ROUGHDUST FLATS",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 10, livery = (Livery)5,  },
										new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
										new() { name = "CP4", carIdx = 05, livery = (Livery)4,  },
										new() { name = "CP5", carIdx = 01, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car05,ROUGHDUST FLATS" } },
						pavementType = (PavementType)1,
				},
				new() {
						id = 1,
						connections = new int[]{ },
						color = F.I.orange,
						size = 1,
						coords = new Vector2(2,3),
						trackName = "FLYING FINISH",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 0, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 1, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 2, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 3, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 4, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car04,FLYING FINISH" } },
						pavementType = (PavementType)0,
				},
				new() {
						id = 2,
						connections = new int[]{ },
						color = Color.green,
						size = 1,
						coords = new Vector2(4,3),
						trackName = "HIGHFLY CLEARWAY",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car02,HIGHFLY CLEARWAY" }},
						pavementType = (PavementType)1,
				},
				new() {
						id = 3,
						connections = new int[]{ },
						color = Color.green,
						size = 1,
						coords = new Vector2(6,3),
						trackName = "TWIN LOOP CIRCUIT",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
										new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car07,TWIN LOOP CIRCUIT" } },
						pavementType = (PavementType)5,
				},
				new() {
						id = 4,
						connections = new int[]{ },
						color = Color.blue,
						size = 1,
						coords = new Vector2(8,3),
						trackName = "WATERFRONT DASH",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car15,WATERFRONT DASH" } },
						pavementType = (PavementType)6,
				},
				new() {
						id = 5,
						connections = new int[]{ },
						color = Color.blue,
						size = 1,
						coords = new Vector2(10,3),
						trackName = "INTERSECTOR",
						laps = 6,
						cars = new CarPlacement[]
						{
										new() { name = "CP1", carIdx = 8, livery = (Livery)2,  },
										new() { name = "CP2", carIdx = 0, livery = (Livery)3,  },
										new() { name = "CP3", carIdx = 7, livery = (Livery)4,  },
										new() { name = "CP4", carIdx = 8, livery = (Livery)6,  },
										new() { name = "CP5", carIdx = 7, livery = (Livery)5,  },
						},
						continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
						prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car13,INTERSECTOR" } },
						pavementType = (PavementType)6
				}
				}
		};
		return defaultVariant;
	}

	internal static ArcadeVariant[] GenerateDefaultVariants()
	{
		ArcadeVariant[] newVariants = new ArcadeVariant[] { GenerateOriginalVariant(), GenerateCommunityVariant(),  GenerateProtoVariant() };
		return newVariants;
	}
}
