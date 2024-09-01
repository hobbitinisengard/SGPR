using RVP;
using System.Collections;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	public Transform roadColliderParent;
	Rigidbody rb;
	VehicleParent vp;
	const float debounceTime = .1f;

	//static AnimationCurve multCurve;
	Coroutine deCo;
	float lastRampValidBounceTime;
	int rbID;
	public float mult = 1;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
		//Physics.ContactModifyEvent += Physics_ContactModifyEvent;
		rbID = rb.GetInstanceID();

		//if(multCurve == null)
		//{
		//	Keyframe[] kf = new Keyframe[]
		//	{
		//		new (Mathf.Cos((90 + 30)*Mathf.Deg2Rad),0,      0, 1/15f), // cos 90+30 = -0.5f
		//		new (Mathf.Cos((90 + 45)*Mathf.Deg2Rad),1, -1/15f, -1/15f), // cos 90+45 = -0.7f
		//		new (Mathf.Cos((90 + 60)*Mathf.Deg2Rad),0,  1/15f, 0), // cos 90+60 = -0.8f
		//	};
		//	multCurve = new AnimationCurve(kf);
		//}
	}
	//private void OnDestroy()
	//{
	//	Physics.ContactModifyEvent -= Physics_ContactModifyEvent;
	//}

	//private void Physics_ContactModifyEvent(PhysicsScene scene, Unity.Collections.NativeArray<ModifiableContactPair> pairs)
	//{
	//	for (int i = 0; i < pairs.Length; ++i)
	//	{

	//		if (pairs[i].bodyInstanceID == rbID)
	//		{
	//			var pair = pairs[i];
	//			var mp = pairs[i].massProperties;
	//			mp.inverseMassScale = 100f;
	//			mp.inverseInertiaScale = 100f;
	//			pair.massProperties = mp;
	//			pairs[i] = pair;

	//			for (int j = 0; j < pairs[i].contactCount; ++j)
	//			{
	//				//pairs[i].SetSeparation(j, pairs[i].GetSeparation(j) / 2);
	//				//pairs[i].SetNormal(j, pairs[i].GetNormal(j) * 2);

	//			}
	//		}
	//		else if(pairs[i].otherBodyInstanceID == rbID)
	//		{
	//			var pair = pairs[i];
	//			var mp = pair.massProperties;
	//			mp.inverseMassScale = 100f;
	//			mp.inverseInertiaScale = 100f;
	//			pair.massProperties = mp;
	//			pairs[i] = pair;

	//			for (int j = 0; j < pairs[i].contactCount; ++j)
	//			{
	//				//pairs[i].SetSeparation(j, pairs[i].GetSeparation(j) / 2);
	//				//pairs[i].SetNormal(j, pairs[i].GetNormal(j) * 2);


	//			}
	//		}
	//	}
	//}


	//void BounceCars(Collision collision)
	//{
	//	var cPoint = collision.GetContact(0);
	//	int dir = cPoint.thisCollider.attachedRigidbody.velocity.magnitude > cPoint.otherCollider.attachedRigidbody.velocity.magnitude ? -1 : 1;
	//	lastCarCarBounceTime = Time.time;
	//	// bounce cars apart
	//	if (Time.time - lastCarCarBounceTime > .2f)
	//		rb.AddForce(collision.GetContact(0).otherCollider.attachedRigidbody.mass * collision.relativeVelocity.magnitude * dir * collision.relativeVelocity.normalized);

	//	// tilt car
	//	rb.AddForceAtPosition(0.5f * rb.mass * transform.up, cPoint.point, ForceMode.Force);
	//}
	//private void OnCollisionStay(Collision collision)
	//{
	//	//if (collision.GetContact(0).otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
	//	//	BounceCars(collision);
	//}
	private void OnCollisionExit(Collision collision)
	{
		//rb.angularDrag = vp.initAngularDrag;
		if (deCo != null)
			StopCoroutine(deCo);
		deCo = StartCoroutine(DebouncingColliding());
	}
	IEnumerator DebouncingColliding()
	{
		yield return new WaitForSeconds(.1f);
		vp.rb.mass = vp.originalMass;
		vp.colliding = false;
		vp.StopSparks();
	}

	private void OnCollisionEnter(Collision col)
	{
		//if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
		//	return;


		// ForceMode.VelocityChange - it's impossible to slingshot in one frame without this setting.
		// On the other hand, adding velocity change every frame will shot the car to the sky
		// Creators of SGP probably wanted to make the cars bounce off of each other when colliding
		// All the other physics quirks like slingshot and accelerating on slopes are sideeffects of the above
		// Recreate car 2 car collisions to recreate quirks
		var otherRb = col.rigidbody;
		if(otherRb == null || vp.velMag < otherRb.velocity.magnitude)
		{
			for (int i = 0; i < col.contactCount; ++i)
			{
				var c = col.GetContact(i);
				//Debug.DrawRay(c.point, c.normal, Color.red, 3);
				Vector3 impulse = c.impulse;
				impulse.y = 0;
				//Vector3 velRight = Vector3.Cross(-vp.rb.velocity.normalized, Vector3.up/*contact.normal*/);
				//Vector3 Up = Vector3.Cross(velRight, Vector3.up/*contact.normal*/);
				float mult = 1;// - Mathf.Abs(Vector3.Dot(Vector3.up, impulse.normalized));
				var dir = c.impulse.normalized;
				rb.AddForceAtPosition(c.impulse.magnitude * mult * dir, c.point, ForceMode.VelocityChange);
				//rb.angularDrag = 1 - mult;
			}
			vp.steeringControl.collisionWheelMult = 0.5f;
		}

		//if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
		//{
		//	BounceCars(col);
		//	return;
		//}

		//if (CountDownSeq.Countdown > 0)
		//	return;
		//if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
		//	return;
		//if (deCo != null)
		//	StopCoroutine(deCo);

		//Vector3 addForce = Vector3.Project(col.relativeVelocity, contact.normal);
		// accelerate when landing on road
		//
		//if(/*addForce.magnitude > 10 && */Time.time - lastRampBounceTime > debounceTime)
		//{
		//	vp.colliding = true;
		//	Vector3 velRight = Vector3.Cross(-vp.rb.velocity.normalized, Vector3.up/*contact.normal*/);
		//	Vector3 direction = Vector3.Cross(velRight, Vector3.up/*contact.normal*/);
		//	Debug.DrawRay(vp.tr.position + 2 * Vector3.up, direction, Color.red, 3);
		//	float angle = Vector3.Angle(vp.tr.forward, contact.normal);
		//	float mult = Mathf.Abs(Mathf.Sin(2 * angle));
		//	//Debug.Log(mult);
		//	rb.AddForceAtPosition(mult * addForce.magnitude * direction,
		//		contact.point,//rb.worldCenterOfMass
		//		ForceMode.VelocityChange);

		//	vp.PlaySparks(col);
		//}
		//lastRampBounceTime = Time.time;
		// accelerate when landing on road
		//Vector3 addForce = Vector3.Project(col.relativeVelocity, contact.normal);
		////if(addForce.magnitude > 10 && Time.time - lastRampBounceTime > debounceTime)
		//{
		//	vp.colliding = true;
		//	Vector3 velRight = Vector3.Cross(-vp.rb.velocity.normalized, Vector3.up/*contact.normal*/);
		//	Vector3 direction = Vector3.Cross(velRight, Vector3.up/*contact.normal*/);
		//	Debug.DrawRay(vp.tr.position + 2 * Vector3.up, direction, Color.red, 3);
		//	lastRampBounceTime = Time.time;
		//	rb.AddForceAtPosition(addForce.magnitude * direction,
		//		contact.point,//rb.worldCenterOfMass
		//		ForceMode.Acceleration);

		//	vp.PlaySparks(col);
		//}

		//Bouncing from walls (you should be able to perform this just with the code above. 
		//float upNormDot = Vector3.Dot(Vector3.up, contact.normal);
		//if (upNormDot < .18f && upNormDot > -.7f) // angle between 80d and 135d
		//{
		//	if (col.relativeVelocity.magnitude < 40)
		//		return;
		//	if (Time.time - lastSideBounceTime < debounceTime)
		//		return;
		//	float mult = multCurve.Evaluate(Vector3.Dot(contact.normal, vp.tr.forward));
		//	//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
		//	addForce = mult * col.relativeVelocity;
		//	Vector3 direction = (vp.tr.forward + contact.normal + vp.tr.up).normalized;
		//	lastSideBounceTime = Time.time;
		//	rb.AddForceAtPosition(direction * addForce.magnitude,
		//	contact.point,//vp.transform.position
		//	ForceMode.VelocityChange);
		//	vp.PlaySparks(col);
		//}
	}

	
}
