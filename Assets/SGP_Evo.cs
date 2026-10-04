using RVP;
using UnityEngine;

public class SGP_Evo : MonoBehaviour
{
	public AudioSource evoBloorp;
	VehicleParent vehicle;
	bool previousStuntState;

	public bool stunting => vehicle && vehicle.originalVehiclePhysics != null &&
		vehicle.originalVehiclePhysics.StuntActive;

	void Awake()
	{
		vehicle = GetComponent<VehicleParent>();
	}

	void FixedUpdate()
	{
		bool active = stunting;
		if (active && !previousStuntState && evoBloorp)
			evoBloorp.Play();
		previousStuntState = active;
	}

	internal void Reset()
	{
		vehicle?.originalVehiclePhysics?.CancelStunt();
		previousStuntState = false;
	}
}
