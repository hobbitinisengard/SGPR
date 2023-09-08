using System.Collections.Generic;
using UnityEngine;
public static class Info
{

	public static Dictionary<string, Car> cars;
	public static void PopulateCarsData()
	{
		if (cars == null)
			cars = new Dictionary<string, Car>();

		cars.Add("car1", new Car(Car.Group.SpeedDemons,  false, "MEAN STREAK\n\nFast, light and agile, this racer offers much for those who wish to modify their vehicle."));
		cars.Add("car2", new Car(Car.Group.WildWheels,   false, "THE HUSTLER\n\nSturdy 4x4 pick-up truck with an eye for the outrageous!"));
		cars.Add("car3", new Car(Car.Group.AeroBlasters, true, "TWIN EAGLE\n\nTake flight with this light and speedy stuntcar."));
		cars.Add("car4", new Car(Car.Group.AeroBlasters, false, "SKY HAWK\n\nGet airborne with this very versatile stunt car."));
		cars.Add("car5", new Car(Car.Group.SpeedDemons,  false, "THE PHANTOM\n\nFast, sleek and tough to handle."));
		cars.Add("car6", new Car(Car.Group.WildWheels,   false, "ROAD HOG\n\nRock and Roll with the rough ridin' road hog."));
		cars.Add("car7", new Car(Car.Group.WildWheels,   false, "DUNE RAT\n\nDefy the laws of physics in this buggy."));
		cars.Add("car8", new Car(Car.Group.SpeedDemons,  false, "NITRO LIGHTNIN''\n\nSupercharged super speed. Easy does it!"));
		cars.Add("car9", new Car(Car.Group.SpeedDemons,  false, "ALLEY KAT\n\nSleek and powerful, this cat is ready to roar."));
		cars.Add("car10", new Car(Car.Group.WildWheels,  false, "SAND SHARK\n\nThis beachcomber is at home on any stunt circuit."));
		cars.Add("car11", new Car(Car.Group.WildWheels,  false, "THE BRUTE\n\nUnleash the Brute for no-nonsense on the road!"));
		cars.Add("car12", new Car(Car.Group.AeroBlasters,false, "WILD DART\n\nFly fast and true with this stuntcar."));
		cars.Add("car13", new Car(Car.Group.WildWheels,  false, "RAGING BULL\n\nPowerful and fast, this streetwise 4x4 is incredible."));
		cars.Add("car14", new Car(Car.Group.AeroBlasters,false, "FLYING MANTIS\n\nSuper light and very fast."));
		cars.Add("car15", new Car(Car.Group.AeroBlasters,false, "STUNT MONKEY\n\nMonkey see, monkey do! Go bananas with this wild ride!"));
		cars.Add("car16", new Car(Car.Group.SpeedDemons, false, "INFERNO\n\nThis speed demon is on fire!"));
		cars.Add("car17", new Car(Car.Group.TeamSpecials,false, "THE FORKSTER\n\n|Despite its looks, it moves like fork lightning!"));
		cars.Add("car18", new Car(Car.Group.TeamSpecials,false, "WORMS MOBILE\n\nSuper Speedy Buggy!"));
		cars.Add("car19", new Car(Car.Group.TeamSpecials,true, "FORMULA 17\n\nIncredibly fast racing car."));
		cars.Add("car20", new Car(Car.Group.TeamSpecials,false, "TEAM MACHINE\n\nThe ultimate, hugely versatile stock car."));
	}
}
public class Car
{
	public enum Group { WildWheels, AeroBlasters, SpeedDemons, TeamSpecials };
	public string desc;
	public Group carClass;
	public float[] sgpBars;
	public bool unlocked;
	public Car(Group carClass, bool unlocked, string desc, float[] sgpBars = null)
	{
		this.desc = desc;
		this.carClass = carClass;
		if(sgpBars==null)
		{
			sgpBars = new float[3];
			for(int i=0; i<3; ++i)
				sgpBars[i] = Random.value;
		}
		this.sgpBars = sgpBars;
		this.unlocked = unlocked;
	}
}

