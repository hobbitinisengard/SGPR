
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
	float heightOffset = 1;
	int rbId;
	static AnimationCurve multCurve;
	
	void Awake()
	{
		vp = GetComponent<VehicleParent>();
		rb = GetComponent<Rigidbody>();
		rbId = vp.rb.GetInstanceID();
		F.I.carRbs.Add(rbId, vp);

		if (multCurve == null)
		{
			Keyframe[] kf = new Keyframe[]
			{
				new (Mathf.Cos((90 + 20)*Mathf.Deg2Rad),0f),
				new (Mathf.Cos((90 + 45)*Mathf.Deg2Rad),1),
				new (Mathf.Cos((90 + 70)*Mathf.Deg2Rad),0f),
			};
			multCurve = new AnimationCurve(kf);
		}
		for (int i = 0; i < vp.ghost.colliders.Length; i++)
		{
			vp.ghost.colliders[i].hasModifiableContacts = true;
		}	
	}
	private void OnDestroy()
	{
		F.I.carRbs.Remove(rbId);
	}
	private void OnCollisionEnter(Collision collision)
	{
		ContactPoint contact = collision.GetContact(0);
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

		if (upNormDot < .1f && upNormDot > -.5f) // angle between 84d and 135d
		{
			if (Time.time - lastSideBounceTime < debounceTime)
				return;

			lastSideBounceTime = Time.time;
			float mult;
			Vector3 direction;
			//if (contact.otherCollider.gameObject.layer == F.I.carCarCollisionLayer)
			//{
			//	mult = 0.01f;
			//	//direction = Vector3.ProjectOnPlane(-collision.impulse, Vector3.up);
			//	direction = (vp.tr.forward + norm + vp.tr.up).normalized;
			//	rb.AddForceAtPosition(collision.impulse.magnitude * mult * direction,
			//	collision.GetContact(0).point,//vp.transform.position
			//	ForceMode.VelocityChange);
			//}
			//else
			{
				mult = multCurve.Evaluate(Vector3.Dot(norm, vp.tr.forward));
				//Debug.Log(mult);
				direction = (vp.tr.forward + norm + vp.tr.up).normalized;
				rb.AddForceAtPosition(collision.relativeVelocity.magnitude * mult * direction,
				collision.GetContact(0).point,//vp.transform.position
				ForceMode.VelocityChange);
			}
			
			//Debug.Log("B: " + Vector3.Dot(norm, vp.tr.forward));
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}

