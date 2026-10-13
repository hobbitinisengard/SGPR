using PathCreation;
using RVP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotPitCheckEntry : MonoBehaviour
{
	private void OnTriggerEnter(Collider carCollider)
	{
		if (F.I.gameMode == GameMode.Skate || !carCollider.attachedRigidbody) return;
		var vp = carCollider.attachedRigidbody.GetComponent<VehicleParent>();
		if (!vp) return;
		vp.followAI.PitsTrigger();
	}
}
