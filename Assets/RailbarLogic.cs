using System.Collections.Generic;
using RVP;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class RailbarLogic : MonoBehaviour
{
    // Keep serialized fields from existing rails.
    [Min(0)] public float lateralDamping = 18;
    [Min(0)] public float rotationDamping = 24;
    BoxCollider supportTrigger;
    readonly List<Slider> sliders = new();

    sealed class Slider
    {
        public VehicleParent vehicle;
        public Vector3 localPosition;
        public Quaternion rotation;
        public float speed;
    }

    void Awake() => supportTrigger = GetComponent<BoxCollider>();
    void OnTriggerStay(Collider other)
    {
        RegisterSupport(other);
        var body = other.attachedRigidbody;
        if (body && body.TryGetComponent(out VehicleParent vehicle) && vehicle.ActiveRailbar == this)
            vehicle.RailbarContactTime = Time.fixedTime;
    }

    void RegisterSupport(Collider other)
    {
        var body = other.attachedRigidbody;
        if (!body || body.isKinematic || !body.TryGetComponent(out VehicleParent vehicle) ||
            !vehicle.Owner || vehicle.ActiveRailbar || CountDownSeq.Countdown > 0 ||
            Time.fixedTime - vehicle.RailbarReleaseTime < 0.2f ||
            vehicle.originalVehiclePhysics == null || !TryGetSupport(vehicle, out var axis, out var normal))
            return;

        var slider = new Slider
        {
            vehicle = vehicle,
            localPosition = transform.InverseTransformPoint(body.position),
            rotation = body.rotation,
            speed = Vector3.Dot(body.linearVelocity, axis)
        };
        // Preserve the heading, including sideways grinds, while removing landing tilt.
        Vector3 forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, normal);
        if (forward.sqrMagnitude > 0.0001f)
            slider.rotation = Quaternion.LookRotation(forward.normalized, normal);
        vehicle.originalVehiclePhysics.SuspendForRailbar();
        vehicle.ActiveRailbar = this;
        vehicle.RailbarVelocity = axis * slider.speed;
        body.isKinematic = true;
        sliders.Add(slider);
    }

    void FixedUpdate()
    {
        for (int i = sliders.Count - 1; i >= 0; --i)
        {
            var slider = sliders[i];
            var vehicle = slider.vehicle;
            if (!vehicle || vehicle.ActiveRailbar != this)
            {
                sliders.RemoveAt(i);
                continue;
            }
            if (!supportTrigger.enabled || !vehicle.isActiveAndEnabled)
            {
                Release(vehicle);
                continue;
            }
            Vector3 axis = transform.up.normalized;
            Vector3 position = transform.TransformPoint(slider.localPosition) + axis * slider.speed * Time.fixedDeltaTime;
            slider.localPosition = transform.InverseTransformPoint(position);
            vehicle.RailbarVelocity = axis * slider.speed;
            vehicle.rb.MovePosition(position);
            vehicle.rb.MoveRotation(slider.rotation);
            float end = supportTrigger.size.y * 0.5f;
            if (Mathf.Abs(slider.localPosition.y - supportTrigger.center.y) >= end)
            {
                // Commit the last step before restoring dynamic motion.
                vehicle.rb.position = position;
                Release(vehicle);
            }
        }
    }

    public void Release(VehicleParent vehicle)
    {
        if (!vehicle || vehicle.ActiveRailbar != this) return;
        Vector3 velocity = vehicle.RailbarVelocity;
        vehicle.ActiveRailbar = null;
        vehicle.RailbarContactTime = float.NegativeInfinity;
        vehicle.RailbarVelocity = Vector3.zero;
        vehicle.RailbarReleaseTime = Time.fixedTime;
        if (vehicle.rb)
        {
            vehicle.rb.isKinematic = false;
            vehicle.rb.linearVelocity = velocity;
            vehicle.rb.angularVelocity = Vector3.zero;
            vehicle.originalVehiclePhysics?.ResetSourceProbeHistory();
        }
        for (int i = sliders.Count - 1; i >= 0; --i)
            if (sliders[i].vehicle == vehicle) sliders.RemoveAt(i);
    }

    void OnDisable()
    {
        for (int i = sliders.Count - 1; i >= 0; --i)
        {
            if (sliders[i].vehicle) Release(sliders[i].vehicle);
            else sliders.RemoveAt(i);
        }
    }

    public bool TryGetSupport(VehicleParent vehicle, out Vector3 axis, out Vector3 normal)
    {
        axis = transform.up.normalized;
        Vector3 up = Physics.gravity.sqrMagnitude > 0.0001f ? -Physics.gravity.normalized : Vector3.up;
        normal = Vector3.ProjectOnPlane(up, axis).normalized;
        if (!isActiveAndEnabled || !supportTrigger || !supportTrigger.enabled || !RaceManager.I ||
            normal.sqrMagnitude < 0.5f || Vector3.Dot(vehicle.rb.linearVelocity, normal) > 2f)
            return false;
        // Capture only a landing on the solid rail, not a car passing through its trigger.
        if (!Physics.Raycast(vehicle.rb.position + normal * 2, -normal, out var hit,
            4, RaceManager.I.wheelCastMask, QueryTriggerInteraction.Ignore) ||
            Vector3.Dot(hit.normal, normal) < 0.65f ||
            (supportTrigger.ClosestPoint(hit.point) - hit.point).sqrMagnitude > 0.04f)
            return false;
        normal = hit.normal.normalized;
        return true;
    }
}
