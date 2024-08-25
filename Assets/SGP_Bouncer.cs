using RVP;
using System.Collections;
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
	Coroutine deCo;

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
	private void OnCollisionEnter(Collision col)
	{
		
		ContactPoint contact = col.GetContact(0);
		if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
		{
			BounceCars(col);
			return;
		}
		if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
			return;
		if (CountDownSeq.Countdown > 0)
			return;
		if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
			return;
		if (contact.otherCollider.transform.parent.name.Contains("slope"))
			return;
		if (deCo != null)
			StopCoroutine(deCo);

		// accelerate when landing on road 
		Vector3 addForce = Project(col.relativeVelocity, col.GetContact(0).normal);
		if(addForce.magnitude > 10 && Time.time - lastRampBounceTime > debounceTime)
		{
			Debug.Log("rampBounce");
			lastRampBounceTime = Time.time;
			vp.colliding = true;
			Vector3 velRight = Vector3.Cross(-vp.rb.velocity.normalized, col.GetContact(0).normal);
			Vector3 direction = Vector3.Cross(velRight, col.GetContact(0).normal);
			//Vector3 direction = Quaternion.AngleAxis(90, velRight) * collision.GetContact(0).normal;

			rb.AddForceAtPosition(direction * addForce.magnitude,//addForce.magnitude,
				rb.worldCenterOfMass,//collision.GetContact(0).point,
				ForceMode.VelocityChange);

			vp.PlaySparks(col);
		}


		// Bouncing from walls (you should be able to perform this just with the code above. 
		float upNormDot = Vector3.Dot(Vector3.up, contact.normal);
		if (upNormDot < .18f && upNormDot > -.7f) // angle between 80d and 135d
		{
			if (col.relativeVelocity.magnitude < 40)
				return;
			if (Time.time - lastSideBounceTime < debounceTime)
				return;
			float mult = multCurve.Evaluate(Vector3.Dot(contact.normal, vp.tr.forward));
			//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
			addForce = mult * col.relativeVelocity;
			Vector3 direction = (vp.tr.forward + contact.normal + vp.tr.up).normalized;
			lastSideBounceTime = Time.time;
			rb.AddForceAtPosition(direction * addForce.magnitude,
			col.GetContact(0).point,//vp.transform.position
			ForceMode.VelocityChange);
			vp.PlaySparks(col);
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		if (deCo != null)
			StopCoroutine(deCo);
		deCo = StartCoroutine(DebouncingColliding());
	}
	IEnumerator DebouncingColliding()
	{
		yield return new WaitForSeconds(.5f);
		vp.colliding = false;
	}
	static Vector3 Project(Vector3 force, Vector3 direction)
	{
		// Calculate the dot product of force and direction
		float dotProduct = Vector3.Dot(force, direction);

		// Calculate the projection
		Vector3 projection = direction * (dotProduct / direction.sqrMagnitude);

		return projection;
	}
}
