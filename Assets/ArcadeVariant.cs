using System;
using UnityEngine;

[Serializable]
public class ArcadeVariant
{
    public string name;
    public RaceNode[] raceNodes;
    public PrizeSetup[] globalPrizes;

    [Serializable]
    public class RaceNode
    {
        public int id;
        public int[] connections; // ids of other nodes
        public Color32 color;
        public float size;
        public Vector2 coords;

        public string trackName;
        public RaceType raceType;
        public PavementType pavementType = PavementType.Random;

        public CpuLevel cpuLevel = CpuLevel.Hard;
        public TimeOfDay timeOfDay = TimeOfDay.Day;

        public CarPlacement[] cars;

        public PrizeSetup prizeSetup;
        public PrizeSetup continuationReq;
    }

    [Serializable]
    public class PrizeSetup
    {
        public string nameOfPrize;
        public Condition condition;
        public string conditionValue; // e.g. "3" for position, "120" for time in seconds, "00:01:30.000" for fastest laptime, "1000" for stunt score, "5" for aero stars, etc.

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
            globalPrizes = new PrizeSetup[]
                {   new(){ nameOfPrize = "car12car13car17",condition = ArcadeVariant.PrizeSetup.Condition.AllPathsFound}
                },
            raceNodes = new RaceNode[]
                {
                    new() {
                        id = 20,
                        connections = new int[] { 7, 22 },
                        color = Color.green,
                        size = 1,
                        coords = new Vector2(3,0),
                        trackName = "DUST BUSTER",
                        cars = new CarPlacement[]
                        {
                            new() { name = "CP1", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP2", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP3", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP4", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP5", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP6", carName = "car", livery = Livery.Random,  },
                        },
                        continuationReq = new PrizeSetup(){condition = PrizeSetup.Condition.PositionAtLeast,conditionValue = "4"},
                        pavementType = PavementType.Japan,
                    },
                    new() {
                        id = 6,
                        connections = new int[] { 22, 9 },
                        color = Color.yellow,
                        size = 1,
                        coords = new Vector2(5,0),
                        trackName = "SECRET SIX",
                        cars = new CarPlacement[]
                        {
                            new() { name = "CP1", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP2", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP3", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP4", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP5", carName = "car", livery = Livery.Random,  },
                            new() { name = "CP6", carName = "car", livery = Livery.Random,  },
                        },
                        continuationReq = new PrizeSetup(){condition = PrizeSetup.Condition.PositionAtLeast,conditionValue = "4"},
                        pavementType = PavementType.Japan,
                    }

                }
        };
        return defaultVariant;
    }
}
