using RVP;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	Rigidbody rb;
	VehicleParent vp;
	public float lastBounceTime;
	public float lastCarCarBounceTime;
	public float lastSideBounceTime;
	public float lastVVBounceTime;
	float debounceTime = .5f;
	float mult = 5;

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
		lastCarCarBounceTime = Time.time;
		// move body up
		//int dir = vp.tr.InverseTransformPoint(contact.point).x > 0 ? 1 : -1;
		//rb.AddRelativeTorque(dir * Vector3.forward * vp.rb.mass * carCarUpCoeff);

		// bounce cars apart
		if (Time.time - lastCarCarBounceTime > lastCarCarBounceTime)
			rb.AddForce(mult * collision.GetContact(0).otherCollider.attachedRigidbody.mass * collision.relativeVelocity.magnitude * -(collision.relativeVelocity.normalized + Vector3.up).normalized);

		// tilt car
		rb.AddForceAtPosition(0.5f * rb.mass * transform.up, collision.GetContact(0).point, ForceMode.Force);
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
		vp.colliding = true;
		Vector3 norm = contact.normal;
		float upNormDot = Vector3.Dot(vp.tr.up, norm);
		if (upNormDot < .1f && upNormDot > -.5f) // angle between 84d and 135d
		{
			if (collision.relativeVelocity.magnitude < 40)
				return;
			if (Time.time - lastSideBounceTime < debounceTime)
				return;
			float mult = multCurve.Evaluate(Vector3.Dot(norm, vp.tr.forward));
			//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
			Vector3 addForce = mult * collision.relativeVelocity;
			Vector3 direction = (vp.tr.forward + norm + vp.tr.up).normalized;
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
