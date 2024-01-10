using RVP;
using UnityEngine;

public class DecreaseGravityOfVehicles : MonoBehaviour
{
	// used only in loops
	private void OnTriggerStay(Collider car)
	{
		car.attachedRigidbody.AddRelativeForce(Vector3.down * 10);
	}
	private void OnTriggerExit(Collider carCollider)
	{
		carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>().customCam = null;
	}
}
