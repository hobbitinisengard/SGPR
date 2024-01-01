using RVP;
using System.Collections;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	Rigidbody rb;
	VehicleParent vp;
	public float lastBounceTime;
	public float lastSideBounceTime;
	float debounceTime = .5f;
	static AnimationCurve multCurve;
	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
		Keyframe[] kf = new Keyframe[] 
		{
			new Keyframe(0,0),
			new Keyframe(45,1),
			new Keyframe(90,0),
		};
		multCurve = new AnimationCurve(kf);
	}
	private void FixedUpdate()
	{
		if (vp.reallyGroundedWheels == 0)
		{
			if (lastBounceTime == 0)
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
			Vector3 bounceDir = (-collision.relativeVelocity.normalized + collision.transform.up).normalized;
			Debug.Log("VVB: " + collision.relativeVelocity.magnitude.ToString());
			rb.AddForceAtPosition(0.1f * collision.relativeVelocity.magnitude * bounceDir,
				collision.GetContact(0).point,
				ForceMode.VelocityChange);
	}
	private void OnCollisionEnter(Collision collision)
	{
		ContactPoint contact = collision.GetContact(0);
		//if (contact.thisCollider.transform.parent != contact.otherCollider.transform.parent &&
		//	contact.otherCollider.gameObject.layer == Info.vehicleLayer && 
		//	contact.thisCollider.gameObject.layer == Info.vehicleLayer)
		//	VehicleVehicleBouncer(collision);
		if (collision.gameObject.layer != Info.roadLayer)
			return;
		vp.colliding = true;
		var norm = contact.normal;
		Vector3 addForce;
		Vector3 direction;
		if (norm.y < 0.1f) // sideways force based on car's velocity
		{
			if (Time.time - lastSideBounceTime < debounceTime)
				return;
			//Debug.Log("sideways");
			float mult = multCurve.Evaluate(Mathf.Abs(Vector3.Angle(-norm, vp.tr.forward)));
			Debug.Log(mult);
			addForce = mult * collision.relativeVelocity;
			direction = (norm + vp.tr.up).normalized;//Quaternion.AngleAxis(88, vp.tr.right) * norm;
			lastSideBounceTime = Time.time;
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
