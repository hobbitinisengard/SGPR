using RVP;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	float shockScale = 0.02f;
	public float rotationalFrictionScale = 0.01f;
	public float minShock = 1f;
	float maxShock = 4f;
	float maxRotShock = 1;
	ContactPoint[] contacts = new ContactPoint[20];
	VehicleParent vp;
	public float lastBounceTime;
	//public float shockThreshold = 0.25f;
	//public float timeDelay = .4f;
	int rbId;
	static AnimationCurve multCurve;
	public Collider[] bouncyCols;
	Coroutine rotEffectCo;
	readonly static Dictionary<int, VehicleParent> carRbs = new(10);
	static bool OnContactModifyRegistered = false;
	float widthLengthAvg = 0;
	bool rotEffectPlaying = false;
	void Awake()
	{
		vp = GetComponent<VehicleParent>();
		rbId = vp.rb.GetInstanceID();
		carRbs.Add(rbId, vp);

		if (multCurve == null)
		{
			Keyframe[] kf = new Keyframe[]
			{
								new (Mathf.Cos((-45)*Mathf.Deg2Rad),1),
								new (Mathf.Cos((0)*Mathf.Deg2Rad),0),
								new (Mathf.Cos((45)*Mathf.Deg2Rad),1),
			};
			multCurve = new AnimationCurve(kf);
		}
		for (int i = 0; i < bouncyCols.Length; i++)
		{
			bouncyCols[i].hasModifiableContacts = true;
		}

		if (!OnContactModifyRegistered)
		{
			OnContactModifyRegistered = true;
			Physics.ContactModifyEvent += OnContactModify;
		}
	}
	private void Start()
	{
		widthLengthAvg = (Vector3.Distance(vp.wheels[0].transform.position, vp.wheels[2].transform.position)
			+ Vector3.Distance(vp.wheels[2].transform.position, vp.wheels[3].transform.position)) / 2f;
	}
	static void OnContactModify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
	{
		foreach (var pair in pairs)
		{
			if (/*pair.bodyInstanceID != 0 && pair.otherBodyInstanceID != 0 &&*/
					carRbs.ContainsKey(pair.bodyInstanceID) && carRbs.ContainsKey(pair.otherBodyInstanceID)) // car-car collisions
			{
				if (pair.contactCount > 0)
				{
					pair.SetPoint(0, (carRbs[pair.otherBodyInstanceID].worldCOM + carRbs[pair.bodyInstanceID].worldCOM) / 2f);
					//pair.SetNormal(0, (pair.GetNormal(0) + Vector3.up) / 2f);
					for (int i = 1; i < pair.contactCount; ++i)
					{
						pair.IgnoreContact(i);
					}
				}
			}
		}
	}
	private void OnDestroy()
	{
		carRbs.Remove(rbId);
	}

	/*collision_energy_min,0.25,"Range(0,100) Min energy for a single impact"
collision_energy_max,0.5,"Range(0,100) Max energy for a single impact"
collision_energy_scale,0.01,Single impact energy scale
collision_energy_impact_limit,0.75,"Range(0,100) Max impact energy after multiple collisions per time"
collision_energy_impact_timedelay,0.4,"Range(0, 1) Time in Seconds"
 */
	void ApplyShock(Vector3 direction, float impactStrength)
	{
		float shockMagnitude = Mathf.Max(minShock, Mathf.Min(impactStrength * shockScale * impactStrength * shockScale, maxShock));
		Vector3 shockForce = direction * shockMagnitude;
		vp.rb.AddForce(shockForce, ForceMode.VelocityChange);
	}

	private void OnCollisionEnter(Collision col)
	{
		Bounce(col);
	}
	void Bounce(Collision col)
	{
		int contactsNr = col.GetContacts(contacts);
		if (contacts[0].otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
			return;
		if (CountDownSeq.Countdown > 0)
			return;
		Vector3 norm = contacts[0].normal;
		if (contacts[0].otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
		{
			//HandleCarToCarCollisionImpact__Fv

			//Vector3 collisionDir = (transform.position - col.body.transform.position).normalized;
			//collisionDir = Vector3.ProjectOnPlane(collisionDir, Vector3.up);
			//Vector3 dir = (vp.tr.forward + norm + vp.tr.up).normalized;
			Vector3 dir = Vector3.up;
			//float impactStrength = Mathf.Abs(Vector3.Dot(col.relativeVelocity, dir));
			float impactStrength = Mathf.Clamp(col.relativeVelocity.magnitude, minShock, maxShock);
			ApplyShock(dir, impactStrength);

			// rotational impulse
			//float rotationalImpulse = Mathf.Max(col.impulse.magnitude * rotationalFrictionScale, minShock);
			//vp.rb.AddTorque(-norm * rotationalImpulse, ForceMode.VelocityChange);

			if (!rotEffectPlaying)
				rotEffectCo = StartCoroutine(RotEffect(contacts[0].point));
		}
		else
		{
			// shooting bug
			float upNormDot = Vector3.Dot(vp.tr.up, norm);

			if (upNormDot < .1f && upNormDot > -.5f) // angle between 84d and 135d
			{
				if (col.impulse.magnitude < 30)
					return;
				if (Time.time - lastBounceTime < 0.3f)
					return;

				lastBounceTime = Time.time;
				float mult = multCurve.Evaluate(Vector3.Dot(-norm, vp.tr.forward));
				//Vector3 direction = Vector3.ProjectOnPlane(-collision.impulse, Vector3.up);
				Vector3 direction = (vp.tr.forward + norm + vp.tr.up).normalized;
				vp.rb.AddForceAtPosition(col.impulse.magnitude * mult * direction,
				contacts[0].point,//vp.transform.position
				ForceMode.VelocityChange);

			}
			//Vector3 collisionDir = (collisionNormal + Vector3.up) / 2f;
			//float impactStrength = Mathf.Abs(Vector3.Dot(col.relativeVelocity, collisionDir));
			//Debug.Log(impactStrength);
			//if(impactStrength > 15)
			//{
			//	ApplyShock(collisionDir, impactStrength);

			//	// rotational impulse
			//	float rotationalImpulse = Mathf.Min(impactStrength * rotationalFrictionScale, maxRotShock);
			//	vp.rb.AddTorque(-collisionNormal * rotationalImpulse, ForceMode.VelocityChange);
			//}






			// HandleParticleToSceneCollision__FUi

			// bounce
			//float impactStrength = Vector3.Dot(vp.rb.linearVelocity, collisionNormal);
			//if (Mathf.Abs(impactStrength) > bounceThres) // recreate bug of stunt gp where only strong impacts cause bounce
			//{
			//    float restitution = GetRestitution(impactStrength); // e.g., from a curve or table
			//    //vp.rb.AddForce(collisionNormal * impactStrength * restitution, ForceMode.VelocityChange);
			//    Vector3 reflectBugVector = Vector3.Reflect(vp.rb.linearVelocity, -collisionNormal) * restitution;
			//    vp.rb.AddForce(reflectBugVector, ForceMode.VelocityChange);
			//    //vp.rb.linearVelocity += Vector3.Reflect(vp.rb.linearVelocity, -collisionNormal) * restitution;
			//}

			//Debug.Log(impactStrength.ToString("F2"));
			// shock
			//if (Mathf.Abs(impactStrength) > shockThreshold)
			//{
			//	float shockMagnitude = Mathf.Min(impactStrength * shockScale * impactStrength * shockScale, maxShock);
			//	Vector3 shockForce = collisionNormal * shockMagnitude;
			//	vp.rb.AddForce(shockForce, ForceMode.VelocityChange);
			//}

		}
		vp.colliding = true;
	}
	IEnumerator RotEffect(Vector3 colPoint)
	{
		rotEffectPlaying = true;
		float timer = 0.5f;
		float totalTime = timer;
		SuspensionSavable sus = (SuspensionSavable)vp.carConfig.GetPartReadonly(PartType.Suspension);

		while (timer > 0)
		{
			//float step = Easing.OutCubic(timer);
			foreach(var w in vp.wheels)
			{
				float d = Vector3.Distance(colPoint, w.transform.position);
				if (d > widthLengthAvg)
					d = widthLengthAvg;
				w.susParent.springForce = sus.RearSpringForce + timer / totalTime * sus.RearSpringForce * (widthLengthAvg - 2*d) / widthLengthAvg;
				w.susParent.springForce = Mathf.Clamp(w.susParent.springForce, 0.1f * sus.RearSpringForce, 2*sus.RearSpringForce);
			}
			timer -= Time.fixedDeltaTime;
			yield return null;
		}
		vp.wheels[0].susParent.springForce = sus.frontSpringForce;
		vp.wheels[1].susParent.springForce = sus.frontSpringForce;
		vp.wheels[2].susParent.springForce = sus.RearSpringForce;
		vp.wheels[3].susParent.springForce = sus.RearSpringForce;
		rotEffectPlaying = false;
	}
	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}

//private void OnCollisionEnter(Collision col)
//{
//    ContactPoint contact = col.GetContact(0);
//    if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
//        return;
//    if (CountDownSeq.Countdown > 0)
//        return;
//    //if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
//    //	return;
//    //if (contact.otherCollider.gameObject.name.Contains("slope"))
//    //	return;


//    vp.colliding = true;
//    Vector3 norm = contact.normal;
//    float upNormDot = Vector3.Dot(vp.tr.up, norm);

//    {
//        //collision_energy_min,0.25,"Range(0,100) Min energy for a single impact"
//        //collision_energy_max,0.5,"Range(0,100) Max energy for a single impact"
//        //collision_energy_scale,0.01,Single impact energy scale
//        //collision_energy_impact_limit,0.75,"Range(0,100) Max impact energy after multiple collisions per time"
//        //collision_energy_impact_timedelay,0.4,"Range(0, 1) Time in Seconds"

//        //if (col.relativeVelocity.magnitude < 0.25f || col.relativeVelocity.magnitude > 0.5)
//        //	return;

//        //if (Time.time - lastSideBounceTime < timeDelay)
//        //	return;

//        //lastSideBounceTime = Time.time;
//        Vector3 collisionDir = (col.body.transform.position - transform.position).normalized;
//        float impactStrength = Vector3.Dot(col.relativeVelocity, collisionDir);

//        // speed-up on collision
//        //vp.rb.AddForceAtPosition(Vector3.ProjectOnPlane(-col.impulse, Vector3.up),
//        //	col.GetContact(0).point,
//        //	ForceMode.VelocityChange);

//        // up force on collision
//        //vp.rb.AddForceAtPosition(-col.relativeVelocity.magnitude * vecGain * vp.upDir,
//        //	col.GetContact(0).point,
//        //	ForceMode.Acceleration);

//        if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
//        {
//            // collision car to car - project on plane 
//            // + force up
//            //vec = Vector3.ProjectOnPlane(-col.relativeVelocity, Vector3.up);
//            //vec += Vector3.up * col.relativeVelocity.magnitude;
//            //            vp.rb.AddForceAtPosition(vec,
//            //	vp.rb.worldCenterOfMass,//col.GetContact(0).point,
//            //	ForceMode.VelocityChange);

//            //mult = 1f;
//            //direction = Vector3.ProjectOnPlane(-collision.impulse.normalized, Vector3.up);
//            ////direction = norm;
//            //rb.AddForceAtPosition(collision.impulse.magnitude * mult * direction,
//            //collision.GetContact(0).point,//vp.transform.position
//            //ForceMode.VelocityChange);
//        }
//        else
//        {
//            //vp.rb.mass = Mathf.Lerp(vp.originalMass,minimumMass, vp.velMag / 60);

//            //	if (col.impulse.magnitude < 40)
//            //		return;

//            //	// workaround for unity slingshot
//            //if (upNormDot < .1f && upNormDot > -.5f) // angle between 84d and 135d
//            {
//                //mult = multCurve.Evaluate(Vector3.Dot(norm, vp.tr.forward));

//                //mult = vp.rb.mass / vp.originalMass;
//                //vec = Vector3.ProjectOnPlane(-col.impulse / mult, Vector3.up);
//                //vp.rb.AddForceAtPosition(vec,
//                //	col.GetContact(0).point,
//                //	ForceMode.Impulse);

//                ////Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
//                //Vector3 addForce = mult * col.relativeVelocity;
//                //Vector3 direction = (vp.tr.forward + norm + vp.tr.up).normalized;
//                //lastSideBounceTime = Time.time;
//                //vp.rb.AddForceAtPosition(direction * addForce.magnitude,
//                //col.GetContact(0).point,//vp.transform.position
//                //ForceMode.VelocityChange);
//            }
//        }

//        //Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
//    }
//}
