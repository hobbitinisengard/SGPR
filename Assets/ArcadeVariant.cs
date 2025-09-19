using System;
using UnityEngine;

[Serializable]
public class ArcadeVariant
{
    public string name;
    public RaceNode[] raceNodes;
    public Prize[] globalPrizes;
    public StartPosition[] starts;

    [Serializable]
    public class StartPosition
    {
        public int node;
        public string allowedCars;
    }
    [Serializable]
    public class RaceNode
    {
        public int id;
        public int[] connections; // ids of other nodes
        public Color32 color;
        public float size;
        public Vector2 coords;

        public string trackName;
        public int laps = 3;
        public RaceType raceType;
        public PavementType pavementType = PavementType.Random;

        public CpuLevel cpuLevel = CpuLevel.Hard;
        public TimeOfDay timeOfDay = TimeOfDay.Day;

        public CarPlacement[] cars;

        public Prize prizeSetup;
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
            AlwaysHighestAero,
            AllPathsFound,
        }
    }
    public static ArcadeVariant GenerateOriginalVariant()
    {
        ArcadeVariant defaultVariant = new()
        {
            name = "Original",
            globalPrizes = new Prize[]
            {   new(){ name = "car12car13car17",condition = Prize.Condition.AllPathsFound}
            },
            starts = new StartPosition[]
            {
                // wild: 2,6,7,10,11,13
                // aero: 3,4,5,12,14,15
                // speed: 1,5,8,9,16
                // team: 17,18,19,20
                new() { node = 16, allowedCars = "7 10 11" },
                new() { node = 17, allowedCars = "4 15" },
                new() { node = 18, allowedCars = "1 8 9" },
            },
            raceNodes = new RaceNode[]
            {
                new() {
                    id = 16,
                    connections = new int[] { 12, 13 },
                    color = F.I.orange,
                    size = 1,
                    coords = new Vector2(3,0),
                    trackName = "DUST BUSTER",
                    laps = 3,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "11", livery = Livery.Titan,  },
                        new() { name = "CP2", carName = "10", livery = Livery.Rline,  },
                        new() { name = "CP3", carName = "7", livery = Livery.Mysuko,  },
                        new() { name = "CP4", carName = "6", livery = Livery.Caltex,  },
                        new() { name = "CP5", carName = "2", livery = Livery.Titan,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
                    pavementType = (PavementType)5,
                },
                new() {
                    id = 17,
                    connections = new int[] { 13, 14 },
                    color = Color.green,
                    size = 1,
                    coords = new Vector2(5,0),
                    trackName = "SECRET SIX",
                    laps = 3,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "15", livery = Livery.TGR,  },
                        new() { name = "CP2", carName = "4", livery = Livery.Rline,  },
                        new() { name = "CP3", carName = "3", livery = Livery.Itex,  },
                        new() { name = "CP4", carName = "14", livery = Livery.Caltex,  },
                        new() { name = "CP5", carName = "4", livery = Livery.Titan,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
                    pavementType = (PavementType)6,
                },
                new() {
                    id = 18,
                    connections = new int[] { 14, 15 },
                    color = Color.blue,
                    size = 1,
                    coords = new Vector2(7,0),
                    trackName = "BANK JOB",
                    laps = 3,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "9", livery = Livery.TGR,  },
                        new() { name = "CP2", carName = "1", livery = Livery.Caltex,  },
                        new() { name = "CP3", carName = "8", livery = Livery.Itex,  },
                        new() { name = "CP4", carName = "9", livery = Livery.Rline,  },
                        new() { name = "CP5", carName = "8", livery = Livery.Mysuko,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "4"},
                    pavementType = (PavementType)5,
                },


                new() {
                    id = 12,
                    connections = new int[] { 7,8 },
                    color = F.I.orange,
                    size = 1,
                    coords = new Vector2(2,1),
                    trackName = "THE SANDWINDER",
                    laps = 4,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "11", livery = (Livery)6,  },
                        new() { name = "CP2", carName = "10", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "7", livery = (Livery)5,  },
                        new() { name = "CP4", carName = "6", livery = (Livery)6,  },
                        new() { name = "CP5", carName = "2", livery = (Livery)7,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
                    pavementType = (PavementType)1,
                },
                new() {
                    id = 13,
                    connections = new int[] { 8,9 },
                    color = Color.green,
                    size = 1,
                    coords = new Vector2(4,1),
                    trackName = "SNAKE STORM",
                    laps = 4,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "15", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "4", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "3", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "14", livery = (Livery)5,  },
                        new() { name = "CP5", carName = "12", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
                    pavementType = (PavementType)0,
                },
                new() {
                    id = 14,
                    connections = new int[] { 9,10 },
                    color = Color.green,
                    size = 1,
                    coords = new Vector2(6,1),
                    trackName = "THE LOOPBACK",
                    laps = 4,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "15", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "4", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "3", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "14", livery = (Livery)5,  },
                        new() { name = "CP5", carName = "12", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
                    pavementType = (PavementType)2,
                },
                new() {
                    id = 15,
                    connections = new int[] { 10,11 },
                    color = Color.blue,
                    size = 1,
                    coords = new Vector2(8,1),
                    trackName = "CURBED HEIGHTS",
                    laps = 4,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "9", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "1", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "8", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "9", livery = (Livery)6,  },
                        new() { name = "CP5", carName = "8", livery = (Livery)5,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "3"},
                    pavementType = (PavementType)2,
                },
                new() {
                    id = 7,
                    connections = new int[] { 1,2 },
                    color = F.I.orange,
                    size = 1,
                    coords = new Vector2(1,2),
                    trackName = "SUNKEN SIGHTS",
                    laps = 5,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "11", livery = (Livery)7,  },
                        new() { name = "CP2", carName = "10", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "7", livery = (Livery)5,  },
                        new() { name = "CP4", carName = "6", livery = (Livery)6,  },
                        new() { name = "CP5", carName = "2", livery = (Livery)4,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
                    pavementType = (PavementType)2,
                },
                new() {
                    id = 8,
                    connections = new int[] { 2,3 },
                    color = F.I.orange,
                    size = 1,
                    coords = new Vector2(3,2),
                    trackName = "HELIPAD HEIGHTS",
                    laps = 5,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "1", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "2", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "3", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "4", livery = (Livery)5,  },
                        new() { name = "CP5", carName = "5", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
                    pavementType = (PavementType)2,
                },
                new() {
                    id = 9,
                    connections = new int[] { 3,4 },
                    color = Color.green,
                    size = 1,
                    coords = new Vector2(5,2),
                    trackName = "FREEFALL FREEWAY",
                    laps = 5,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "15", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "4", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "3", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "14", livery = (Livery)5,  },
                        new() { name = "CP5", carName = "12", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
                    pavementType = (PavementType)2,
                },
                    new() {
                    id = 10,
                    connections = new int[] { 4,5 },
                    color = Color.blue,
                    size = 1,
                    coords = new Vector2(7,2),
                    trackName = "HIGH ROLLER",
                    laps = 5,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "9", livery = (Livery)3,  },
                        new() { name = "CP2", carName = "1", livery = (Livery)7,  },
                        new() { name = "CP3", carName = "8", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "9", livery = (Livery)2,  },
                        new() { name = "CP5", carName = "8", livery = (Livery)5,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
                    pavementType = (PavementType)0,
                },
                    new() {
                    id = 11,
                    connections = new int[] {5,6},
                    color = Color.blue,
                    size = 1,
                    coords = new Vector2(9,2),
                    trackName = "CRAZY STRAIGHTS",
                    laps = 5,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "9", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "1", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "8", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "9", livery = (Livery)6,  },
                        new() { name = "CP5", carName = "8", livery = (Livery)5,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "2"},
                    pavementType = (PavementType)5,
                },
                    new() {
                    id = 1,
                    connections = null,
                    color = F.I.orange,
                    size = 1,
                    coords = new Vector2(0,3),
                    trackName = "ROUGHDUST FLATS",
                    laps = 6,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "11", livery = (Livery)5,  },
                        new() { name = "CP2", carName = "10", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "7", livery = (Livery)5,  },
                        new() { name = "CP4", carName = "6", livery = (Livery)4,  },
                        new() { name = "CP5", carName = "2", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
                    prizeSetup = new Prize(){name = "car2",condition = Prize.Condition.PositionAtLeast, conditionArgument = "1"},
                    pavementType = (PavementType)1,
                },
                    new() {
                    id = 2,
                    connections = null,
                    color = F.I.orange,
                    size = 1,
                    coords = new Vector2(2,3),
                    trackName = "FLYING FINISH",
                    laps = 6,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "1", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "2", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "3", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "4", livery = (Livery)5,  },
                        new() { name = "CP5", carName = "5", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
                    prizeSetup = new Prize(){name = "car6",condition = Prize.Condition.PositionAtLeast, conditionArgument = "1"},
                    pavementType = (PavementType)0,
                },
                    new() {
                    id = 3,
                    connections = null,
                    color = Color.green,
                    size = 1,
                    coords = new Vector2(4,3),
                    trackName = "HIGHFLY CLEARWAY",
                    laps = 6,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "15", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "4", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "3", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "13", livery = (Livery)5,  },
                        new() { name = "CP5", carName = "12", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
                    prizeSetup = new Prize(){name = "car3",condition = Prize.Condition.PositionAtLeast, conditionArgument = "1"},
                    pavementType = (PavementType)1,
                },
                    new() {
                    id = 4,
                    connections = null,
                    color = Color.green,
                    size = 1,
                    coords = new Vector2(6,3),
                    trackName = "TWIN LOOP CIRCUIT",
                    laps = 6,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "15", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "4", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "3", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "13", livery = (Livery)5,  },
                        new() { name = "CP5", carName = "12", livery = (Livery)6,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
                    prizeSetup = new Prize(){name = "car14",condition = Prize.Condition.PositionAtLeast, conditionArgument = "1"},
                    pavementType = (PavementType)5,
                },
                    new() {
                    id = 5,
                    connections = null,
                    color = Color.blue,
                    size = 1,
                    coords = new Vector2(8,3),
                    trackName = "WATERFRONT DASH",
                    laps = 6,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "9", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "1", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "8", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "9", livery = (Livery)6,  },
                        new() { name = "CP5", carName = "8", livery = (Livery)5,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
                    prizeSetup = new Prize(){name = "car16",condition = Prize.Condition.PositionAtLeast, conditionArgument = "1"},
                    pavementType = (PavementType)6,
                },
                    new() {
                    id = 6,
                    connections = null,
                    color = Color.blue,
                    size = 1,
                    coords = new Vector2(10,3),
                    trackName = "INTERSECTOR",
                    laps = 6,
                    cars = new CarPlacement[]
                    {
                        new() { name = "CP1", carName = "9", livery = (Livery)2,  },
                        new() { name = "CP2", carName = "1", livery = (Livery)3,  },
                        new() { name = "CP3", carName = "8", livery = (Livery)4,  },
                        new() { name = "CP4", carName = "9", livery = (Livery)6,  },
                        new() { name = "CP5", carName = "8", livery = (Livery)5,  },
                    },
                    continuationReq = new Prize(){condition = Prize.Condition.PositionAtLeast,conditionArgument = "1"},
                    prizeSetup = new Prize(){name = "car5",condition = Prize.Condition.PositionAtLeast, conditionArgument = "1"},
                    pavementType = (PavementType)6,
                },
            }
        };
        return defaultVariant;
    }
}
