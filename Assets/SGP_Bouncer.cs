using RVP;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	Rigidbody rb;
	VehicleParent vp;
	float lastBounceTime;
	Vector3 rampVel;
	float debounceTime = .3f;
	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
	}
	private void Update()
	{
		if (vp.groundedWheels == 0 && rampVel == Vector3.zero && Time.time - lastBounceTime > debounceTime)
			rampVel = vp.rb.velocity;
	}
	void VehicleVehicleBouncer(Collision collision)
	{
		if (collision.GetContact(0).otherCollider.gameObject.layer != Info.vehicleLayer)
			return;
			rb.AddForceAtPosition(-0.5f*collision.relativeVelocity,
				collision.GetContact(0).point,
				ForceMode.VelocityChange);
	}
	private void OnCollisionEnter(Collision collision)
	{
		if (!collision.GetContact(0).thisCollider.CompareTag("Underside"))
			VehicleVehicleBouncer(collision);
		if (collision.gameObject.layer != Info.roadLayer)
			return;
		if (Time.time - lastBounceTime < debounceTime)
			return;
		lastBounceTime = Time.time;
		//Debug.Log(Time.time);
		//Debug.DrawRay(collision.GetContact(0).point, colNormal, Color.red, 5);
		vp.colliding = true;

		var norm = collision.GetContact(0).normal;
		float velMagn=0;
		if(norm.y <0.1f) // sideways force based on car's velocity
		{
			//Debug.Log("sideways");
			Vector3 addForce = ProjectOnVector(collision.relativeVelocity, norm);
			velMagn = addForce.magnitude;
		}
		else
		{ // vertical force based on car's previous ramp speed
			
			if(rampVel.magnitude>0)
			{
				//Debug.Log("vertical");
				Vector3 addForce = ProjectOnVector(rampVel, norm);
				velMagn = addForce.magnitude;
				rampVel = Vector3.zero;
			}
			
		}
		Vector3 direction = Quaternion.AngleAxis(90, vp.rightDir) * norm;//new(vp.forwardDir.x, 0, vp.forwardDir.z);//
		rb.AddForceAtPosition(direction * velMagn,
			vp.transform.position,//collision.GetContact(0).point,
			ForceMode.VelocityChange);
	}
	public static Vector3 ProjectOnVector(in Vector3 force, in Vector3 direction)
	{
		float dot = Vector3.Dot(force.normalized, direction.normalized);
		return  dot * force;
	}
	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}
