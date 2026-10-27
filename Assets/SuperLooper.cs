using System.Collections.Generic;
using RVP;
using UnityEngine;

public class SuperLooper : MonoBehaviour
{
	readonly Dictionary<VehicleParent, HashSet<Collider>> overlappingVehicles = new();

	private void OnTriggerEnter(Collider carCollider)
	{
		var body = carCollider.attachedRigidbody;
		if (!body || !body.TryGetComponent<VehicleParent>(out var vp)) return;

		if (name == "AddLooper")
		{
			if (!overlappingVehicles.TryGetValue(vp, out var colliders))
			{
				colliders = new HashSet<Collider>();
				overlappingVehicles.Add(vp, colliders);
			}
			// Only the first collider entering awards the stunt for this vehicle.
			if (colliders.Add(carCollider) && colliders.Count == 1 && vp.raceBox)
				vp.raceBox.AddLooper();
		}
		else if (vp.customCam == null)
		{
			vp.customCam = transform.GetChild(0).gameObject;
		}
	}

	private void OnTriggerExit(Collider carCollider)
	{
		var body = carCollider.attachedRigidbody;
		if (!body || !body.TryGetComponent<VehicleParent>(out var vp)) return;
		if (!overlappingVehicles.TryGetValue(vp, out var colliders)) return;

		colliders.Remove(carCollider);
		// Re-arm only after the entire vehicle leaves, not after one collider exits.
		if (colliders.Count == 0) overlappingVehicles.Remove(vp);
	}

	private void OnDisable()
	{
		overlappingVehicles.Clear();
	}
}
