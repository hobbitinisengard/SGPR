using RVP;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RailbarLogic : MonoBehaviour
{
	[Min(0)] public float lateralDamping = 18;
	[Min(0)] public float rotationDamping = 24;
	BoxCollider supportTrigger;

	void Awake() => supportTrigger = GetComponent<BoxCollider>();

	void OnTriggerEnter(Collider carCollider) => RegisterSupport(carCollider);
	void OnTriggerStay(Collider carCollider) => RegisterSupport(carCollider);

	void RegisterSupport(Collider carCollider)
	{
		Rigidbody body = carCollider.attachedRigidbody;
		if (!body || body.isKinematic || !body.TryGetComponent(out VehicleParent vehicle))
			return;
		vehicle.originalVehiclePhysics?.RegisterRailbarSupport(this);
	}

	public bool TryGetSupport(VehicleParent vehicle, out Vector3 axis, out Vector3 normal)
	{
		axis = transform.up.normalized; // The authored trigger's local Y follows the rail.
		normal = Vector3.ProjectOnPlane(-Physics.gravity.normalized, axis).normalized;
		if (!isActiveAndEnabled || !supportTrigger || !supportTrigger.enabled ||
			!RaceManager.I || normal.sqrMagnitude < 0.5f || vehicle.rb.isKinematic ||
			Vector3.Dot(vehicle.rb.rotation * Vector3.up, normal) < 0.5f ||
			Vector3.Dot(vehicle.rb.linearVelocity, normal) > 2 ||
			Mathf.Abs(Vector3.Dot(vehicle.rb.linearVelocity, axis)) < 2)
			return false;

		// A trigger overlap alone is not support: recheck the solid Unity rail.
		// This prevents capturing cars flying over or driving beside the rail.
		if (!Physics.Raycast(vehicle.rb.position + normal * 2, -normal, out RaycastHit hit,
			4, RaceManager.I.wheelCastMask, QueryTriggerInteraction.Ignore) ||
			Vector3.Dot(hit.normal, normal) < 0.65f ||
			(supportTrigger.ClosestPoint(hit.point) - hit.point).sqrMagnitude > 0.04f)
			return false;
		normal = hit.normal.normalized;
		axis = Vector3.ProjectOnPlane(axis, normal).normalized;
		return axis.sqrMagnitude > 0.5f;
	}
}
