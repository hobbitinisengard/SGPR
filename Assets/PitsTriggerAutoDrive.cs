using PathCreation;
using RVP;
using UnityEngine;

public class PitsTriggerAutoDrive : MonoBehaviour
{
	public GameObject cameraPos;
	private void OnTriggerEnter(Collider carCollider)
	{
		var vp = carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>();
		PathCreator pitsPathCreator = transform.parent.parent.GetComponent<EnergyTunnelPath>().pitsPathCreator;
		vp.customCam = cameraPos;
		vp.followAI.DriveThruPits(pitsPathCreator);
	}
}
