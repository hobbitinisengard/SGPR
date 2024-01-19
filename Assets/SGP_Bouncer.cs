using RVP;
using System.Collections;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	Rigidbody rb;
	VehicleParent vp;
	public float lastBounceTime;
	public float lastSideBounceTime;
	public float lastVVBounceTime;
	float debounceTime = .5f;
	static AnimationCurve multCurve;
	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
		if(multCurve == null)
		{
			Keyframe[] kf = new Keyframe[]
			{
				new Keyframe(0,0),
				new Keyframe(45,1),
				new Keyframe(90,0),
			};
			multCurve = new AnimationCurve(kf);
		}
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
		if (Time.time - lastVVBounceTime < debounceTime)
			return;
		lastVVBounceTime = Time.time;
		Vector3 bounceDir;
		bounceDir = (/*collision.relativeVelocity.normalized + .1f **/ collision.transform.up).normalized;
		Debug.Log("VVB: " + collision.relativeVelocity.magnitude.ToString());
		rb.AddForceAtPosition(0.25f * collision.relativeVelocity.magnitude * bounceDir,
			vp.transform.position,//collision.GetContact(0).point,
			ForceMode.VelocityChange);
	}
	private void OnCollisionEnter(Collision collision)
	{
		ContactPoint contact = collision.GetContact(0);
		//if (
		//	contact.otherCollider.gameObject.layer == Info.vehicleLayer &&
		//	contact.thisCollider.gameObject.layer == Info.vehicleLayer)
		//{
		//	VehicleVehicleBouncer(collision);
		//	return;
		//}
		if (collision.gameObject.layer != Info.roadLayer)
			return;
		vp.colliding = true;
		Vector3 norm = contact.normal;
		Vector3 addForce;
		Vector3 direction;
		if (norm.y < 0.1f) // sideways force based on car's velocity
		{
			if (collision.relativeVelocity.magnitude < 40)
				return;
			if (Time.time - lastSideBounceTime < debounceTime)
				return;
			//Debug.Log("sideways");
			float mult = multCurve.Evaluate(Mathf.Abs(Vector3.Angle(-norm, vp.tr.forward)));
			addForce = mult * collision.relativeVelocity;
			direction = (norm + vp.tr.up).normalized;//Quaternion.AngleAxis(88, vp.tr.right) * norm;
			lastSideBounceTime = Time.time;
			rb.AddForceAtPosition(direction * addForce.magnitude,
			collision.GetContact(0).point,//vp.transform.position
			ForceMode.VelocityChange);
		}
		else
		{ // vertical force based on car's previous ramp speed
			if (lastBounceTime == 0 || Time.time - lastBounceTime < debounceTime)
				return;
			addForce = Vector3.Project(collision.relativeVelocity, -norm);
			Vector3 rightV = Vector3.Cross(-F.Vec3Flat(vp.rb.velocity),Vector3.up).normalized;
			direction = Vector3.Cross(rightV, norm).normalized;//(vp.rb.velocity - addForce).normalized;
			Debug.DrawRay(vp.tr.position, direction, Color.red, 4);
			lastBounceTime = 0;
			rb.AddForceAtPosition(direction * addForce.magnitude,
			vp.centerOfMassObj.position,//collision.GetContact(0).point,
			ForceMode.VelocityChange);
		}

	}
	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}
