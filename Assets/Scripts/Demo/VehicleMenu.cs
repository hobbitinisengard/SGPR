using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Demo Scripts/Vehicle Menu", 0)]

	// Class for the menu in the demo
	public class VehicleMenu : MonoBehaviour
	{
		public CameraControl cam;
		public GameObject spawnPoint;
		public Vector3 spawnRot;
		public GameObject[] vehicles;
		public GameObject chaseVehicle;
		public GameObject chaseVehicleDamage;
		float chaseCarSpawnTime;
		GameObject newVehicle;
		//public Toggle autoShiftToggle;
		//public Toggle assistToggle;
		//public Toggle stuntToggle;
		//public Toggle camToggle;
		public SGP_HUD hud;

		void Update()
		{
			//cam.stayFlat = camToggle.isOn;
			chaseCarSpawnTime = Mathf.Max(0, chaseCarSpawnTime - Time.deltaTime);
		}

		// Spawns a vehicle from the vehicles array at the index
		public void SpawnVehicle(int vehicle)
		{
			newVehicle = Instantiate(vehicles[vehicle], spawnPoint.transform.position, spawnPoint.transform.rotation) as GameObject;
			cam.Connect(newVehicle.GetComponent<VehicleParent>());
			if (hud)
			{
				hud.Connect(newVehicle.GetComponent<VehicleParent>());
			}
		}
	}
}