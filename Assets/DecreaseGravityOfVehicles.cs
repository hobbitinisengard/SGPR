using RVP;
using System.Collections;
using UnityEngine;

public class DecreaseGravityOfVehicles : MonoBehaviour
{
	Coroutine waitCo;
	// used only in loops
	private void OnTriggerStay(Collider car)
	{
		
		car.attachedRigidbody.AddRelativeForce(Vector3.down * 10);
	}
	private void OnTriggerExit(Collider carCollider)
	{
		if (waitCo != null)
			StopCoroutine(waitCo);
		StartCoroutine(Wait(carCollider));
		
	}
	private void OnTriggerEnter(Collider carCollider)
	{
		carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>().followAI.looping = true;
	}
	IEnumerator Wait(Collider carCollider)
	{
		yield return new WaitForSeconds(0.5f);
		carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>().customCam = null;
		carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>().followAI.looping = false;
	}
}
