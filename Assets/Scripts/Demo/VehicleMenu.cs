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
            cam.Initialize(newVehicle);

            //if (newVehicle.GetComponent<VehicleAssist>())
            //{
            //    newVehicle.GetComponent<VehicleAssist>().enabled = assistToggle.isOn;
            //}

            //Transmission trans = newVehicle.GetComponentInChildren<Transmission>();
            //if (trans)
            //{
                //trans.automatic = autoShiftToggle.isOn;
                //newVehicle.GetComponent<VehicleParent>().brakeIsReverse = autoShiftToggle.isOn;

                //if (trans is ContinuousTransmission && !autoShiftToggle.isOn)
                //{
                //    newVehicle.GetComponent<VehicleParent>().brakeIsReverse = true;
                //}
            //}

            //if (newVehicle.GetComponent<FlipControl>() && newVehicle.GetComponent<StuntDetect>())
            //{
            //    newVehicle.GetComponent<FlipControl>().flipPower = stuntToggle.isOn && assistToggle.isOn ? new Vector3(10, 10, -10) : Vector3.zero;
            //    newVehicle.GetComponent<FlipControl>().rotationCorrection = stuntToggle.isOn ? Vector3.zero : (assistToggle.isOn ? new Vector3(5, 1, 10) : Vector3.zero);
            //    newVehicle.GetComponent<FlipControl>().stopFlip = assistToggle.isOn;
            //}

            if (hud)
            {
                hud.Connect(newVehicle);
            }
        }

        // Spawns a chasing vehicle
        public void SpawnChaseVehicle()
        {
            if (chaseCarSpawnTime == 0)
            {
                chaseCarSpawnTime = 1;
                GameObject chaseCar = Instantiate(chaseVehicle, spawnPoint.transform.position, Quaternion.LookRotation(spawnRot, GlobalControl.worldUpDir)) as GameObject;
                chaseCar.GetComponent<FollowAI>().target = newVehicle.transform;
            }
        }

        // Spawns a damageable chasing vehicle
        public void SpawnChaseVehicleDamage()
        {
            if (chaseCarSpawnTime == 0)
            {
                chaseCarSpawnTime = 1;
                GameObject chaseCar = Instantiate(chaseVehicleDamage, spawnPoint.transform.position, Quaternion.LookRotation(spawnRot, GlobalControl.worldUpDir)) as GameObject;
                chaseCar.GetComponent<FollowAI>().target = newVehicle.transform;
            }
        }
    }
}