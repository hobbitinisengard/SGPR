using RVP;
using System.Collections;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	Rigidbody rb;
	VehicleParent vp;
	public float lastBounceTime;
	float debounceTime = .5f;
	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
	}
	private void FixedUpdate()
	{
		if (vp.reallyGroundedWheels == 0)
		{
			if(lastBounceTime == 0)
				lastBounceTime = Time.time;
		}
		else
		{
			if (Time.time - lastBounceTime > 0.5f)
				StartCoroutine(ZeroizeLastBounceTime());
		}
	}
	IEnumerator ZeroizeLastBounceTime()
	{
		yield return new WaitForFixedUpdate();
		lastBounceTime = 0;
	}
	void VehicleVehicleBouncer(Collision collision)
	{
		if (collision.GetContact(0).otherCollider.gameObject.layer != Info.vehicleLayer)
			return;
			rb.AddForceAtPosition(-0.5f*collision.relativeVelocity,
				collision.GetContact(0).point,
				ForceMode.VelocityChange);
	}
	private void OnCollisionEnter(Collision collision)
	{
		//if (!collision.GetContact(0).thisCollider.CompareTag("Underside"))
		//	VehicleVehicleBouncer(collision);
		if (collision.gameObject.layer != Info.roadLayer)
			return;
		vp.colliding = true;
		var norm = collision.GetContact(0).normal;
		Vector3 addForce;
		Vector3 direction;
		if(norm.y <0.1f) // sideways force based on car's velocity
		{
			//Debug.Log("sideways");
			addForce = Vector3.Project(collision.relativeVelocity, -norm);
			direction = Quaternion.AngleAxis(88, vp.tr.right) * norm;
		}
		else
		{ // vertical force based on car's previous ramp speed
			if (lastBounceTime == 0 || Time.time - lastBounceTime < debounceTime)
				return;
			addForce = Vector3.Project(collision.relativeVelocity, -norm);
			direction = (vp.rb.velocity - addForce).normalized;
			lastBounceTime = 0;
		}
		rb.AddForceAtPosition(direction * addForce.magnitude,
			vp.transform.position,//collision.GetContact(0).point,
			ForceMode.VelocityChange);
	}
	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}
