using PathCreation;
using RVP;
using UnityEngine;

public class PitsTriggerAutoDrive : MonoBehaviour
{
	public GameObject cameraPos;
	public bool allowPlayerAutoDrive = true;
	private void OnTriggerEnter(Collider carCollider)
	{
		if (!carCollider || !carCollider.attachedRigidbody)
			return;

		var vp = carCollider.attachedRigidbody.GetComponent<VehicleParent>();
		if (!vp || (!allowPlayerAutoDrive && vp == RaceManager.I.playerCar))
			return;

		PathCreator pitsPathCreator = transform.parent.parent.GetComponent<EnergyTunnelPath>().pitsPathCreator;
		vp.customCam = cameraPos;
		vp.followAI.DriveThruPits(pitsPathCreator);
	}
}
