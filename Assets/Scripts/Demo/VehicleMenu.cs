using UnityEngine;

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
		public SGP_HUD hud;

		void Update()
		{
			//cam.stayFlat = camToggle.isOn;
			chaseCarSpawnTime = Mathf.Max(0, chaseCarSpawnTime - Time.deltaTime);
		}
	}
}