
using RVP;
using System.Linq;
using Unity.Collections;
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
	float heightOffset = 1;
	static int[] rbIds = new int[2];
	static AnimationCurve multCurve;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
		if (multCurve == null)
		{
			Keyframe[] kf = new Keyframe[]
			{
				new (Mathf.Cos((90 + 30)*Mathf.Deg2Rad),0,      0, 1/15f), // cos 90+30 = -0.5f
				new (Mathf.Cos((90 + 45)*Mathf.Deg2Rad),1, -1/15f, -1/15f), // cos 90+45 = -0.7f
				new (Mathf.Cos((90 + 60)*Mathf.Deg2Rad),0,  1/15f, 0), // cos 90+60 = -0.8f
			};
			multCurve = new AnimationCurve(kf);
		}
		for (int i = 0; i < vp.ghost.colliders.Length; i++)
		{
			vp.ghost.colliders[i].hasModifiableContacts = true;
			rbIds[i] = vp.ghost.colliders[i].GetInstanceID();
		}
		Physics.ContactModifyEvent += OnContactModify;
	}
	void OnContactModify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
	{
		foreach (var pair in pairs)
		{
			// if the contact point is not on our object skip it
			if (rbIds.Any(id => id == pair.bodyInstanceID) || rbIds.Any(id => id == pair.otherBodyInstanceID))
			{
				if(vp.velMag < pair.otherBodyVelocity.magnitude)
				{
					for(int i=0; i<pair.contactCount; ++i)
					{
						Vector3 normal = pair.GetNormal(i);
						float angle = Vector3.SignedAngle(normal, 
							(vp.rb.worldCenterOfMass - pair.GetPoint(i)).normalized, vp.upDir);
						normal = Quaternion.AngleAxis(angle, vp.upDir) * normal;
						pair.SetNormal(i, normal);
					}
				}
			}
		}
	}
	void BounceCars(Collision col)
	{
		// bounce cars apart
		if (Time.time - lastCarCarBounceTime > lastCarCarBounceTime)
		{
			lastCarCarBounceTime = Time.time;
			var c = col.GetContact(0);
			//Vector3 dir = (c.otherCollider.attachedRigidbody.worldCenterOfMass
			//	- c.thisCollider.attachedRigidbody.worldCenterOfMass).normalized;
			//if (vp.velMag < c.otherCollider.attachedRigidbody.velocity.magnitude)
			//	dir *= -1;

			Vector3 collisionForce = Vector3.ProjectOnPlane(-col.impulse, Vector3.up);
			// Apply this collision force to the center of mass
			rb.AddForceAtPosition(collisionForce, c.point, 
				ForceMode.VelocityChange);
		}

		//rb.AddForce(mult * col.GetContact(0).otherCollider.attachedRigidbody.mass * col.relativeVelocity.magnitude
		//	* -col.relativeVelocity.normalized);
		// tilt car
	}
	private void OnCollisionEnter(Collision collision)
	{
		ContactPoint contact = collision.GetContact(0);
		if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
			return;
		if (CountDownSeq.Countdown > 0)
			return;
		if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
			return;
		if (contact.otherCollider.gameObject.name.Contains("slope"))
			return;
		if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
		{
			BounceCars(collision);
			return;
		}

		vp.colliding = true;
		Vector3 norm = contact.normal;
		float upNormDot = Vector3.Dot(vp.tr.up, norm);

		if (upNormDot < .1f && upNormDot > -.5f) // angle between 84d and 135d
		{
			if (collision.relativeVelocity.magnitude < 40)
				return;
			if (Time.time - lastSideBounceTime < debounceTime)
				return;

			lastSideBounceTime = Time.time;
			float mult = multCurve.Evaluate(Vector3.Dot(norm, vp.tr.forward));
			//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
			Vector3 addForce = collision.relativeVelocity;
			//Vector3 direction = Vector3.ProjectOnPlane(-collision.impulse, Vector3.up);
			Vector3 direction = (vp.tr.forward + norm + vp.tr.up).normalized;
			rb.AddForceAtPosition(mult * direction * addForce.magnitude,
			collision.GetContact(0).point,//vp.transform.position
			ForceMode.VelocityChange);
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}

