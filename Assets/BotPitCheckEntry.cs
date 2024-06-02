using PathCreation;
using RVP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotPitCheckEntry : MonoBehaviour
{
	private void OnTriggerEnter(Collider carCollider)
	{
		var vp = carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.followAI.PitsTrigger();
	}
}
