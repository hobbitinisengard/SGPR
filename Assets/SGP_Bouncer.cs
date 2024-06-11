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
				new (30,0,      0, 1/15f),
				new (45,1, -1/15f, -1/15f),
				new (60,0,  1/15f, 0),
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
		vp.raceBox.evoModule.stunting = false;

		ContactPoint contact = collision.GetContact(0);
		if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
		{
			BounceCars(collision);
			return;
		}
		if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
		{
			return;
		}
		if (CountDownSeq.Countdown > 0)
			return;
		if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
			return;
		vp.colliding = true;
		Vector3 norm = contact.normal;
		Vector3 velMagUp = -Vector3.Cross(vp.tr.right, rb.velocity.normalized);
		if (Mathf.Abs(Vector2.Dot(velMagUp, norm)) < .5f) // sideways force based on car's velocity
		{
			if (collision.relativeVelocity.magnitude < 40)
				return;
			if (Time.time - lastSideBounceTime < debounceTime)
				return;

			//Debug.Log("sideways");
			float mult = multCurve.Evaluate(Mathf.Abs(Vector3.Angle(-norm, vp.tr.forward)));
			Vector3 addForce = mult * collision.relativeVelocity;
			Vector3 direction = (norm + vp.tr.up).normalized;//Quaternion.AngleAxis(88, vp.tr.right) * norm;
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
