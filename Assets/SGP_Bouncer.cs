using RVP;
using UnityEngine;
public class SGP_Bouncer : MonoBehaviour
{
	Rigidbody rb;
	VehicleParent vp;
	float lastBounceTime;
	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
	}
	private void OnCollisionEnter(Collision collision)
	{
		if (collision.GetContact(0).thisCollider.tag != "Underside")
			return;
		if (collision.gameObject.layer != Info.roadLayer)
			return;
		if (Time.time - lastBounceTime < 0.1f)
			return;
		lastBounceTime = Time.time;
		vp.colliding = true;

		//Vector3 colNormal = collision.GetContact(0).normal;
		//Debug.DrawRay(collision.GetContact(0).point, colNormal, Color.red, 5);
		Vector3 addForce = ProjectOnVector(collision.relativeVelocity, collision.GetContact(0).normal);
		float velocityMagn = addForce.magnitude;
		Vector3 direction = Quaternion.AngleAxis(90, vp.rightDir) * collision.GetContact(0).normal;
		rb.AddForceAtPosition(direction * velocityMagn,
			collision.GetContact(0).point,
			ForceMode.VelocityChange);
	}
	public static Vector3 ProjectOnVector(Vector3 force, Vector3 direction)
	{
		// Calculate the dot product of force and direction
		float dotProduct = Vector3.Dot(force, direction);

		// Calculate the magnitude of the direction vector squared
		float directionMagnitudeSquared = direction.sqrMagnitude;

		// Calculate the projection
		Vector3 projection = direction * (dotProduct / directionMagnitudeSquared);

		return projection;
	}
	private void OnCollisionExit(Collision collision)
	{
		vp.colliding = false;
	}
}
