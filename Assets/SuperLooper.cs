using RVP;
using UnityEngine;

public class SuperLooper : MonoBehaviour
{
	private void OnTriggerStay(Collider carCollider)
	{
		VehicleParent vp = carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>();
		if(vp.customCam == null)
		{
			vp.raceBox.AddLooper();
			vp.customCam = transform.GetChild(0).gameObject;
		}
	}
}
