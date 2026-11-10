using RVP;
using UnityEngine;

// Smooth Sync's velocity-driven mode divides by Time.deltaTime. Run before it
// so paused frames cannot turn a valid teleport into 0/0 or infinite velocity.
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class NetworkVehicleSyncGuard : MonoBehaviour
{
    Smooth.SmoothSyncNetcode sync;
    VehicleParent vehicle;
    bool velocityDriven;
    float resumeVelocityTime;
    Smooth.SmoothSyncNetcode.validateStateDelegate previousValidation;
    void Awake()
    {
        vehicle = GetComponent<VehicleParent>();
        sync = GetComponent<Smooth.SmoothSyncNetcode>();
        if (sync)
        {
            velocityDriven = sync.setVelocityInsteadOfPositionOnNonOwners;
            previousValidation = sync.validateStateMethod;
            sync.validateStateMethod = ValidateState;
        }
    }
    void Update() => PrepareSync();
    void FixedUpdate() => PrepareSync();
    void PrepareSync()
    {
        if (!sync) return;
        bool remote = vehicle.IsSpawned && !vehicle.IsOwner;
        sync.setVelocityInsteadOfPositionOnNonOwners = velocityDriven &&
            (!remote || (Time.timeScale > 0 && Time.deltaTime > .000001f && Time.unscaledTime >= resumeVelocityTime));
    }
    public void ResetReceived()
    {
        // Synchronize the discontinuity as a pose, never as a velocity inferred
        // from the distance to a pre-reset interpolation target.
        resumeVelocityTime = Time.unscaledTime + .15f;
        PrepareSync();
    }
    bool ValidateState(Smooth.StateNetcode state, Smooth.StateNetcode previous)
    {
        if (state == null || !OriginalVehiclePhysics.IsFinite(state.position) ||
            !OriginalVehiclePhysics.IsFinite(state.velocity) || !OriginalVehiclePhysics.IsFinite(state.angularVelocity) ||
            !OriginalVehiclePhysics.IsFinite(state.scale) ||
            !Finite(state.rotation.x) || !Finite(state.rotation.y) || !Finite(state.rotation.z) || !Finite(state.rotation.w) ||
            float.IsNaN(state.ownerTimestamp) ||
            float.IsInfinity(state.ownerTimestamp)) return false;
        return previousValidation == null || previousValidation(state, previous);
    }
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    void OnDestroy()
    {
        if (sync) sync.validateStateMethod = previousValidation;
    }
    void OnDisable()
    {
        if (sync) sync.setVelocityInsteadOfPositionOnNonOwners = velocityDriven;
    }
}
