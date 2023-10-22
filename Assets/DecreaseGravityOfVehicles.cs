using UnityEngine;

public class DecreaseGravityOfVehicles : MonoBehaviour
{
	Rigidbody rb;
	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}
	private void OnTriggerEnter(Collider car)
	{
		car.attachedRigidbody.useGravity = false;
	}
	private void OnTriggerStay(Collider car)
	{
		car.attachedRigidbody.AddForce(new Vector3(0.0f, -rb.drag, 0.0f));
	}
	private void OnTriggerExit(Collider car)
	{
		car.attachedRigidbody.useGravity = true;
	}
}
