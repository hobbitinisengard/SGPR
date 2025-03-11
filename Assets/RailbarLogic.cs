using RVP;
using UnityEngine;

public class RailbarLogic : MonoBehaviour
{
	void OnTriggerEnter(Collider carCollider)
	{
		var vp = carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.rb.angularVelocity = Vector3.zero;
		vp.rb.linearVelocity = Vector3.Project(vp.rb.linearVelocity, transform.up);
	}
	private void OnTriggerStay(Collider carCollider)
	{
		var vp = carCollider.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.rb.angularVelocity = Vector3.zero;
		vp.rb.linearVelocity = Vector3.Project(vp.rb.linearVelocity, transform.up);
	}
}
