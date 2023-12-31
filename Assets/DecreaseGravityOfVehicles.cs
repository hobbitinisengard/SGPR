using UnityEngine;

public class DecreaseGravityOfVehicles : MonoBehaviour
{
	//private void OnTriggerEnter(Collider car)
	//{
	//	car.attachedRigidbody.useGravity = false;
	//}
	private void OnTriggerStay(Collider car)
	{
		car.attachedRigidbody.AddRelativeForce(Vector3.down * 10);
	}
	//private void OnTriggerExit(Collider car)
	//{
	//	car.attachedRigidbody.useGravity = true;
	//}
}
