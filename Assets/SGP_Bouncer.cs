using RVP;
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
	private void OnCollisionEnter(Collision collision)
	{
		ContactPoint contact = collision.GetContact(0);
		if (vp.countdownTimer > 0)
			return;
		if (contact.otherCollider.gameObject.layer != Info.roadLayer)
			return;
		vp.colliding  = true;
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
			if (vp.brakeInput > 0)
				return;
			if (Time.time - lastBounceTime < debounceTime)
				return;
			addForce = Vector3.Project(collision.relativeVelocity, -norm);
			Vector3 rightV = Vector3.Cross(-vp.rb.velocity.normalized,norm).normalized;
			direction = Vector3.Cross(rightV, norm).normalized;//(vp.rb.velocity - addForce).normalized;
			//Debug.DrawRay(vp.centerOfMassObj.position, direction, Color.magenta, 4);
			//Debug.DrawRay(vp.centerOfMassObj.position, -norm, Color.white, 4);
			//Debug.Log(addForce.magnitude);
			lastBounceTime = Time.time;
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
