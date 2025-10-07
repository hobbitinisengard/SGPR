using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

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
		public LinkedList<RankingRowData> rankingRows = new ();
		public Progress()
		{
			
		}
		public Progress(ArcadeVariant parent)
		{
			this.parent = parent;
			int nodes = parent.nodes.Length;
			prizesCompleted = new BitField32[nodes];
			pathsDone = new List<List<int>>(nodes);
			for (int i = 0; i < nodes; ++i)
				pathsDone.Add(new List<int>());
		}
		public float OverallProgress()
		{
			float progress = 0;
			progress += pathsDone.Sum(p => p.Count);
			progress += prizesCompleted.Sum(p => p.CountBits());
			progress += globalPrizesCompleted.CountBits();
			progress /= parent.nodes.Sum(p => p.prizeReqs.Length) + parent.nodes.Sum(n => n.connections.Length) + parent.globalPrizes.Length;
			return progress;
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

		public CpuLevel cpuLevel = CpuLevel.Hard;
		public TimeOfDay timeOfDay;

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
			AeroStarsAtLeast,
			StuntAtLeast,
			DriftsAtLeast,
			TimeAtMost,
			FastestLaptime,
			AlwaysFirst,
			AllPathsFound,
		}
	}
	public static ArcadeVariant GenerateCommunityVariant()
	{
		ArcadeVariant communityVariant = new()
		{
			name = "Community",
			globalPrizes = new Prize[] { new() { name = "car20", condition = Prize.Condition.AllPathsFound } },
			starts = new StartPos[]
			{
				new(){node = 0, allowedCarsIdxs = new int[]{9}},
				new(){node = 1, allowedCarsIdxs = new int[]{3}},
				new(){node = 2, allowedCarsIdxs = new int[]{8}},
			},
			nodes = new Node[]
			{
				new()
				{
					id = 0,
					connections = new int[] { 12 },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(3,0),
					trackName = "DUST BUSTER REVERSE",
					laps = 6,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = Livery.Titan,  },
							new() { name = "CP2", carIdx = 09, livery = Livery.Rline,  },
							new() { name = "CP3", carIdx = 06, livery = Livery.Mysuko,  },
							new() { name = "CP4", carIdx = 05, livery = Livery.Caltex,  },
							new() { name = "CP5", carIdx = 01, livery = Livery.Titan,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "DUST BUSTER REVERSE" } },//new Prize[]{ },
					pavementType = (PavementType)5,
				},
			}
		};
		return communityVariant;
	}
	public static ArcadeVariant GenerateSidesweeperVariant()
	{
		ArcadeVariant reversedVariant = new()
		{
			name = "Sidesweeper",
			globalPrizes = new Prize[] { new() { name = "car16", condition = Prize.Condition.AllPathsFound},
				new() {name = "car18,car19", condition = Prize.Condition.AlwaysFirst} },
			starts = new StartPos[]
			{
				new() {node = 0, allowedCarsIdxs = new int[]{ 16,1 } },//fork,hustler
				new() {node = 1, allowedCarsIdxs = new int[]{ 5 } },//road hog
				new() {node = 2, allowedCarsIdxs = new int[]{ 2 } },//twin eagle
				new() {node = 3, allowedCarsIdxs = new int[]{ 13 } },//mantis
				new() {node = 4, allowedCarsIdxs = new int[]{ 15 } },//inferno
				new() {node = 5, allowedCarsIdxs = new int[]{ 4 } },//inferno
			},
			nodes = new Node[]
			{
				new() {
					id = 15,
					connections = new int[] { 12 },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(3,0),
					trackName = "DUST BUSTER REVERSE",
					laps = 6,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = Livery.Titan,  },
							new() { name = "CP2", carIdx = 09, livery = Livery.Rline,  },
							new() { name = "CP3", carIdx = 06, livery = Livery.Mysuko,  },
							new() { name = "CP4", carIdx = 05, livery = Livery.Caltex,  },
							new() { name = "CP5", carIdx = 01, livery = Livery.Titan,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "DUST BUSTER REVERSE" } },//new Prize[]{ },
					pavementType = (PavementType)5,
				},
				new() {
					id = 16,
					connections = new int[] { 13 },
					color = Color.green,
					size = 1,
					coords = new Vector2(5,0),
					trackName = "SECRET SIX REVERSE",
					laps = 6,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 14, livery = Livery.TGR,  },
							new() { name = "CP2", carIdx = 03, livery = Livery.Rline,  },
							new() { name = "CP3", carIdx = 02, livery = Livery.Itex,  },
							new() { name = "CP4", carIdx = 13, livery = Livery.Caltex,  },
							new() { name = "CP5", carIdx = 03, livery = Livery.Titan,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SECRET SIX REVERSE" } },//new Prize[]{ },
					pavementType = (PavementType)6,
				},
				new() {
					id = 17,
					connections = new int[] { 14 },
					color = Color.blue,
					size = 1,
					coords = new Vector2(7,0),
					trackName = "BANK JOB REVERSE",
					laps = 6,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 08, livery = Livery.TGR,  },
							new() { name = "CP2", carIdx = 00, livery = Livery.Caltex,  },
							new() { name = "CP3", carIdx = 07, livery = Livery.Itex,  },
							new() { name = "CP4", carIdx = 08, livery = Livery.Rline,  },
							new() { name = "CP5", carIdx = 07, livery = Livery.Mysuko,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "BANK JOB REVERSE" } },//new Prize[]{ },
					pavementType = (PavementType)5,
				},
				new() {
					id = 11,
					connections = new int[] { 7,15 },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(2,1),
					trackName = "THE SANDWINDER REVERSE",
					laps = 6,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = (Livery)6,  },
							new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
							new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
							new() { name = "CP5", carIdx = 01, livery = (Livery)7,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "THE SANDWINDER REVERSE" } },//new Prize[]{ },
					pavementType = (PavementType)1,
				},
				new() {
					id = 12,
					connections = new int[] { 8,16 },
					color = Color.green,
					size = 1,
					coords = new Vector2(4,1),
					trackName = "SNAKESTORM REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SNAKESTORM REVERSE" } },
					pavementType = (PavementType)0,
				},
				new() {
					id = 13,
					connections = new int[] { 9,17 },
					color = Color.green,
					size = 1,
					coords = new Vector2(6,1),
					trackName = "THE LOOPBACK REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "THE LOOPBACK REVERSE" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 14,
					connections = new int[] { 10 },
					color = Color.blue,
					size = 1,
					coords = new Vector2(8,1),
					trackName = "CURBED HEIGHTS REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CURBED HEIGHTS REVERSE" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 6,
					connections = new int[] { 1,11 },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(1,2),
					trackName = "SUNKEN SIGHTS",
					laps = 6,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = (Livery)7,  },
							new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
							new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
							new() { name = "CP5", carIdx = 01, livery = (Livery)4,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "SUNKEN SIGHTS REVERSE" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 7,
					connections = new int[] { 2,12},
					color = F.I.orange,
					size = 1,
					coords = new Vector2(3,2),
					trackName = "HELIPAD HEIGHTS REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "HELIPAD HEIGHTS REVERSE" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 8,
					connections = new int[] { 3,13 },
					color = Color.green,
					size = 1,
					coords = new Vector2(5,2),
					trackName = "FREEFALL FREEWAY REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "FREEFALL FREEWAY REVERSE" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 9,
					connections = new int[] { 4,14 },
					color = Color.blue,
					size = 1,
					coords = new Vector2(7,2),
					trackName = "HIGH ROLLER REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "HIGH ROLLER REVERSE" }  },
					pavementType = (PavementType)0,
				},
				new() {
					id = 10,
					connections = new int[] {5},
					color = Color.blue,
					size = 1,
					coords = new Vector2(9,2),
					trackName = "CRAZY STRAIGHTS REVERSE",
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
					prizeReqs =   new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "CRAZY STRAIGHTS REVERSE" }  },
					pavementType = (PavementType)5,
				},
				new() {
					id = 0,
					connections = new int[]{ 6 },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(0,3),
					trackName = "ROUGHDUST FLATS REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "ROUGHDUST FLATS REVERSE" } },
					pavementType = (PavementType)1,
				},
				new() {
					id = 1,
					connections = new int[]{ 7},
					color = F.I.orange,
					size = 1,
					coords = new Vector2(2,3),
					trackName = "FLYING FINISH REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car5,FLYING FINISH REVERSE" } },
					pavementType = (PavementType)0,
				},
				new() {
					id = 2,
					connections = new int[]{ 8},
					color = Color.green,
					size = 1,
					coords = new Vector2(4,3),
					trackName = "HIGHFLY CLEARWAY REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car2,HIGHFLY CLEARWAY REVERSE" }},
					pavementType = (PavementType)1,
				},
				new() {
					id = 3,
					connections = new int[]{ 9},
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car13" } },
					pavementType = (PavementType)5,
				},
				new() {
					id = 4,
					connections = new int[]{ 10},
					color = Color.blue,
					size = 1,
					coords = new Vector2(8,3),
					trackName = "WATERFRONT DASH REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car15,WATERFRONT DASH REVERSE" } },
					pavementType = (PavementType)6,
				},
				new() {
					id = 5,
					connections = new int[]{ },
					color = Color.blue,
					size = 1,
					coords = new Vector2(10,3),
					trackName = "INTERSECTOR REVERSE",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car4,INTERSECTOR REVERSE" } },
					pavementType = (PavementType)6
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
			globalPrizes = new Prize[] { new() { name = "car12,car13,car17", condition = Prize.Condition.AllPathsFound } },
			starts = new StartPos[]
			{
				new() { node = 15, allowedCarsIdxs = new int[]{6,09,10 } },
				new() { node = 16, allowedCarsIdxs = new int[]{3,14 } },
				new() { node = 17, allowedCarsIdxs = new int[]{0,7,8 } },
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
					laps = 1,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = Livery.Titan,  },
							new() { name = "CP2", carIdx = 09, livery = Livery.Rline,  },
							new() { name = "CP3", carIdx = 06, livery = Livery.Mysuko,  },
							new() { name = "CP4", carIdx = 05, livery = Livery.Caltex,  },
							new() { name = "CP5", carIdx = 01, livery = Livery.Titan,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "spn1,DUST BUSTER" } },//new Prize[]{ },
					pavementType = (PavementType)5,
				},
				new() {
					id = 16,
					connections = new int[] { 12, 13 },
					color = Color.green,
					size = 1,
					coords = new Vector2(5,0),
					trackName = "SECRET SIX",
					laps = 1,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 14, livery = Livery.TGR,  },
							new() { name = "CP2", carIdx = 03, livery = Livery.Rline,  },
							new() { name = "CP3", carIdx = 02, livery = Livery.Itex,  },
							new() { name = "CP4", carIdx = 13, livery = Livery.Caltex,  },
							new() { name = "CP5", carIdx = 03, livery = Livery.Titan,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
					prizeReqs = new Prize[]{ },
					pavementType = (PavementType)6,
				},
				new() {
					id = 17,
					connections = new int[] { 13, 14 },
					color = Color.blue,
					size = 1,
					coords = new Vector2(7,0),
					trackName = "BANK JOB",
					laps = 1,
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
					trackName = "THE SANDWINDER",
					laps = 1,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = (Livery)6,  },
							new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
							new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
							new() { name = "CP5", carIdx = 01, livery = (Livery)7,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
					prizeReqs = new Prize[]{ },
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "spn2,SNAKESTORM" } },
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "spn3,THE LOOPBACK" } },
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "spn4,CURBED HEIGHTS" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 6,
					connections = new int[] { 0,1 },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(1,2),
					trackName = "SUNKEN SIGHTS",
					laps = 1,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = (Livery)7,  },
							new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
							new() { name = "CP4", carIdx = 05, livery = (Livery)6,  },
							new() { name = "CP5", carIdx = 01, livery = (Livery)4,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "spn5,SUNKEN SIGHTS" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 7,
					connections = new int[] { 1,2 },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(3,2),
					trackName = "HELIPAD HEIGHTS",
					laps = 2,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 0, livery = (Livery)2,  },
							new() { name = "CP2", carIdx = 1, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 2, livery = (Livery)4,  },
							new() { name = "CP4", carIdx = 3, livery = (Livery)5,  },
							new() { name = "CP5", carIdx = 4, livery = (Livery)6,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "spn6,HELIPAD HEIGHTS" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 8,
					connections = new int[] { 2,3 },
					color = Color.green,
					size = 1,
					coords = new Vector2(5,2),
					trackName = "FREEFALL FREEWAY",
					laps = 2,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
							new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
							new() { name = "CP4", carIdx = 13, livery = (Livery)5,  },
							new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "spn7,FREEFALL FREEWAY" } },
					pavementType = (PavementType)2,
				},
				new() {
					id = 9,
					connections = new int[] { 3,4 },
					color = Color.blue,
					size = 1,
					coords = new Vector2(7,2),
					trackName = "HIGH ROLLER",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "HIGH ROLLER" }	},
					pavementType = (PavementType)0,
				},
				new() {
					id = 10,
					connections = new int[] {4,5},
					color = Color.blue,
					size = 1,
					coords = new Vector2(9,2),
					trackName = "CRAZY STRAIGHTS",
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
					prizeReqs =		new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "7", name = "CRAZY STRAIGHTS" }	},
					pavementType = (PavementType)5,
				},
				new() {
					id = 0,
					connections = new int[]{ },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(0,3),
					trackName = "ROUGHDUST FLATS",
					laps = 1,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 10, livery = (Livery)5,  },
							new() { name = "CP2", carIdx = 09, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 06, livery = (Livery)5,  },
							new() { name = "CP4", carIdx = 05, livery = (Livery)4,  },
							new() { name = "CP5", carIdx = 01, livery = (Livery)6,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car2,ROUGHDUST FLATS" } },
					pavementType = (PavementType)1,
				},
				new() {
					id = 1,
					connections = new int[]{ },
					color = F.I.orange,
					size = 1,
					coords = new Vector2(2,3),
					trackName = "FLYING FINISH",
					laps = 3,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 0, livery = (Livery)2,  },
							new() { name = "CP2", carIdx = 1, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 2, livery = (Livery)4,  },
							new() { name = "CP4", carIdx = 3, livery = (Livery)5,  },
							new() { name = "CP5", carIdx = 4, livery = (Livery)6,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car6,FLYING FINISH" } },
					pavementType = (PavementType)0,
				},
				new() {
					id = 2,
					connections = new int[]{ },
					color = Color.green,
					size = 1,
					coords = new Vector2(4,3),
					trackName = "HIGHFLY CLEARWAY",
					laps = 3,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
							new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
							new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
							new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car3,HIGHFLY CLEARWAY" }},
					pavementType = (PavementType)1,
				},
				new() {
					id = 3,
					connections = new int[]{ },
					color = Color.green,
					size = 1,
					coords = new Vector2(6,3),
					trackName = "TWIN LOOP CIRCUIT",
					laps = 3,
					cars = new CarPlacement[]
					{
							new() { name = "CP1", carIdx = 14, livery = (Livery)2,  },
							new() { name = "CP2", carIdx = 03, livery = (Livery)3,  },
							new() { name = "CP3", carIdx = 02, livery = (Livery)4,  },
							new() { name = "CP4", carIdx = 12, livery = (Livery)5,  },
							new() { name = "CP5", carIdx = 11, livery = (Livery)6,  },
					},
					continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car14,TWIN LOOP CIRCUIT" } },
					pavementType = (PavementType)5,
				},
				new() {
					id = 4,
					connections = new int[]{ },
					color = Color.blue,
					size = 1,
					coords = new Vector2(8,3),
					trackName = "WATERFRONT DASH",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car16,WATERFRONT DASH" } },
					pavementType = (PavementType)6,
				},
				new() {
					id = 5,
					connections = new int[]{ },
					color = Color.blue,
					size = 1,
					coords = new Vector2(10,3),
					trackName = "INTERSECTOR",
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
					prizeReqs = new Prize[] { new() { condition = Prize.Condition.PositionAtLeast, conditionArgument = "1", name = "car5,INTERSECTOR" } },
					pavementType = (PavementType)6
				}
			}
		};
		return defaultVariant;
	}
}
