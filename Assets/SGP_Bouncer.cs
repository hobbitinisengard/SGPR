using RVP;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	Rigidbody rb;
	VehicleParent vp;
	float lastCarCarBounceTime;
	float lastRampBounceTime;
	float lastSideBounceTime;
	const float debounceTime = .5f;

	static AnimationCurve multCurve;
	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
		if(multCurve == null)
		{
			Keyframe[] kf = new Keyframe[]
			{
				new (Mathf.Cos((90 + 30)*Mathf.Deg2Rad),0,      0, 1/15f), // cos 90+30 = -0.5f
				new (Mathf.Cos((90 + 45)*Mathf.Deg2Rad),1, -1/15f, -1/15f), // cos 90+45 = -0.7f
				new (Mathf.Cos((90 + 60)*Mathf.Deg2Rad),0,  1/15f, 0), // cos 90+60 = -0.8f
			};
			multCurve = new AnimationCurve(kf);
		}
	}
	void BounceCars(Collision collision)
	{
		var cPoint = collision.GetContact(0);
		int dir = cPoint.thisCollider.attachedRigidbody.velocity.magnitude > cPoint.otherCollider.attachedRigidbody.velocity.magnitude ? -1 : 1;
		lastCarCarBounceTime = Time.time;
		// bounce cars apart
		if (Time.time - lastCarCarBounceTime > .2f)
			rb.AddForce(collision.GetContact(0).otherCollider.attachedRigidbody.mass * collision.relativeVelocity.magnitude * dir * collision.relativeVelocity.normalized);

		// tilt car
		rb.AddForceAtPosition(0.5f * rb.mass * transform.up, cPoint.point, ForceMode.Force);
	}
	private void OnCollisionStay(Collision collision)
	{
		if (collision.GetContact(0).otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
			BounceCars(collision);
	}
	private void OnCollisionEnter(Collision collision)
	{
		ContactPoint contact = collision.GetContact(0);
		if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
		{
			BounceCars(collision);
			return;
		}
		if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
			return;
		if (CountDownSeq.Countdown > 0)
			return;
		if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
			return;
		if (contact.otherCollider.gameObject.name.Contains("slope"))
			return;
		

		// accelerate when landing on road 
		Vector3 addForce = .5f * Vector3.Project(collision.relativeVelocity, collision.GetContact(0).normal);
		if(addForce.magnitude > 10 && Time.time - lastRampBounceTime > debounceTime)
		{
			lastRampBounceTime = Time.time;
			vp.colliding = true;
			Vector3 velRight = Vector3.Cross(-vp.rb.velocity.normalized, collision.GetContact(0).normal);
			Vector3 direction = Vector3.Cross(velRight, collision.GetContact(0).normal);
			//Vector3 direction = Quaternion.AngleAxis(90, velRight) * collision.GetContact(0).normal;

			rb.AddForceAtPosition(direction * addForce.magnitude,//addForce.magnitude,
				rb.worldCenterOfMass,//collision.GetContact(0).point,
				ForceMode.VelocityChange);
		}
		

		// Bouncing from walls
		float upNormDot = Vector3.Dot(vp.tr.up, contact.normal);
		if (upNormDot < .18f && upNormDot > -.7f) // angle between 80d and 135d
		{
			if (collision.relativeVelocity.magnitude < 40)
				return;
			if (Time.time - lastSideBounceTime < debounceTime)
				return;
			float mult = multCurve.Evaluate(Vector3.Dot(contact.normal, vp.tr.forward));
			//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
			addForce = mult * collision.relativeVelocity;
			Vector3 direction = (vp.tr.forward + contact.normal + vp.tr.up).normalized;
			lastSideBounceTime = Time.time;
			rb.AddForceAtPosition(direction * addForce.magnitude,
			collision.GetContact(0).point,//vp.transform.position
			ForceMode.VelocityChange);
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}
