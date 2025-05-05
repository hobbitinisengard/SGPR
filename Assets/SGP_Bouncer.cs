using RVP;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
/* 
 * 1. There is added force on every body collision projected flat
*/
public class SGP_Bouncer : MonoBehaviour
{
	VehicleParent vp;
	public float lastBounceTime;
	public float lastCarCarBounceTime;
	public float lastSideBounceTime;
	public float lastVVBounceTime;
	float debounceTime = .5f;
	int rbId;
	static AnimationCurve multCurve;
	public Collider[] bouncyCols;
	
	static Dictionary<int, VehicleParent> carRbs = new(10);
	static bool OnContactModifyRegistered = false;
	void Awake()
	{
		vp = GetComponent<VehicleParent>();
		rbId = vp.rb.GetInstanceID();
		carRbs.Add(rbId, vp);

		if (multCurve == null)
		{
			Keyframe[] kf = new Keyframe[]
			{
				new (Mathf.Cos((-90)*Mathf.Deg2Rad),1),
				new (Mathf.Cos((0)*Mathf.Deg2Rad),0),
				new (Mathf.Cos((90)*Mathf.Deg2Rad),1),
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
	static void OnContactModify(PhysicsScene scene, NativeArray<ModifiableContactPair> pairs)
	{
		foreach (var pair in pairs)
		{
			if (/*pair.bodyInstanceID != 0 && pair.otherBodyInstanceID != 0 &&*/
				carRbs.ContainsKey(pair.bodyInstanceID) && carRbs.ContainsKey(pair.otherBodyInstanceID)) // car-car collisions
			{
				if (pair.contactCount > 0)
				{
					//newPoint -= carRbs[pair.bodyInstanceID].upDir * carRbs[pair.bodyInstanceID].rb.centerOfMass.y;
					pair.SetPoint(0, carRbs[pair.bodyInstanceID].worldCOM);
					var normal = pair.GetNormal(0);
					//float upNormDot = Mathf.Abs(Vector3.Dot(carRbs[pair.bodyInstanceID].rightDir, normal));

					pair.SetBounciness(0, 1);

					pair.SetNormal(0, Vector3.ProjectOnPlane(normal, Vector3.up));

					//pair.SetNormal(0, mult * Vector3.ProjectOnPlane(pair.GetNormal(0)/*(carRbs[pair.bodyInstanceID].worldCOM - oldPoint).normalized*/, Vector3.up));
					//pair.SetBounciness(0, 1);
					//Vector3 n = pair.rotation * Vector3.up;
					//pair.SetTargetVelocity(0, n * 10f);

					for (int i = 1; i < pair.contactCount; ++i)
					{
						pair.IgnoreContact(i);
					}
				}
			}
			//Vector3 normal = pair.GetNormal(i);
			//float angle = Vector3.SignedAngle(normal, 
			//	(vp.rb.worldCenterOfMass - pair.GetPoint(i)).normalized, vp.upDir);
			//normal = Quaternion.AngleAxis(angle, vp.upDir) * normal;

			//Vector3 normal = Vector3.ProjectOnPlane(pair.GetNormal(i), Vector3.up);
			//pair.SetNormal(i, normal);
			//pair.SetBounciness(i, 1);

		}
	}
	private void OnDestroy()
	{
		carRbs.Remove(rbId);
	}
	private void OnCollisionEnter(Collision col)
	{
		ContactPoint contact = col.GetContact(0);
		if (contact.otherCollider.gameObject.layer == F.I.ignoreWheelCastLayer)
			return;
		if (CountDownSeq.Countdown > 0)
			return;
		//if (contact.otherCollider.gameObject.layer != F.I.roadLayer)
		//	return;
		//if (contact.otherCollider.gameObject.name.Contains("slope"))
		//	return;
		

		vp.colliding = true;
		Vector3 norm = contact.normal;
		float upNormDot = Vector3.Dot(vp.tr.up, norm);

		
		{
			if (Time.time - lastSideBounceTime < debounceTime)
				return;

			lastSideBounceTime = Time.time;
			float mult;
			Vector3 vec;

			if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
			{
				// original sgp effect (tushkan)
				vec = Vector3.ProjectOnPlane(-col.impulse, Vector3.up);
				vp.rb.AddForceAtPosition(vec,
					col.GetContact(0).point, //col.GetContact(0).point,//vp.rb.worldCenterOfMass + Vector3.up * vp.rb.centerOfMass.y//vp.transform.position
					ForceMode.VelocityChange);
				//mult = 1f;
				//direction = Vector3.ProjectOnPlane(-collision.impulse.normalized, Vector3.up);
				////direction = norm;
				//rb.AddForceAtPosition(collision.impulse.magnitude * mult * direction,
				//collision.GetContact(0).point,//vp.transform.position
				//ForceMode.VelocityChange);
			}
			else
			{
				if (col.impulse.magnitude < 40)
					return;

				// workaround for unity slingshot
				if (upNormDot < .1f && upNormDot > -.5f) // angle between 84d and 135d
				{
					mult = multCurve.Evaluate(Vector3.Dot(norm, vp.tr.forward));
					//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
					Vector3 addForce = mult * col.relativeVelocity;
					Vector3 direction = (vp.tr.forward + norm + vp.tr.up).normalized;
					lastSideBounceTime = Time.time;
					vp.rb.AddForceAtPosition(direction * addForce.magnitude,
					col.GetContact(0).point,//vp.transform.position
					ForceMode.VelocityChange);
				}
			}
			
			//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}

