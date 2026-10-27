using RVP;
using System.Collections;
using UnityEngine;

public class DecreaseGravityOfVehicles : MonoBehaviour
{
	Coroutine waitCo;
	private void OnTriggerExit(Collider carCollider)
	{
		if (waitCo != null)
			StopCoroutine(waitCo);
		waitCo = StartCoroutine(Wait(carCollider));
	}
	private void OnTriggerEnter(Collider carCollider)
	{
		VehicleParent vp = carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.followAI.looping = true;
	}
	IEnumerator Wait(Collider carCollider)
	{
		yield return new WaitForSeconds(0.1f);
		carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>().customCam = null;
		carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>().followAI.looping = false;
	}
}
