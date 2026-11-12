using RVP;
using Unity.Netcode;
using UnityEngine;

// Keep network position corrections out of the vehicle's physical velocity.
// Run before Smooth Sync and VehicleParent in both update loops.
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class NetworkVehicleSyncGuard : MonoBehaviour
{
    Smooth.SmoothSyncNetcode sync;
    VehicleParent vehicle;
    bool velocityDriven;
    float resumeVelocityTime;
    float originalBackTime, originalExtrapolationTime, originalExtrapolationDistance;
    bool originalTimeLimit, originalDistanceLimit;
    Smooth.SmoothSyncNetcode.ExtrapolationMode originalExtrapolationMode;
    float nextLatencySample, roundTripSeconds;
    Smooth.SmoothSyncNetcode.validateStateDelegate previousValidation;
    void Awake()
    {
        vehicle = GetComponent<VehicleParent>();
        sync = GetComponent<Smooth.SmoothSyncNetcode>();
        if (sync)
        {
            velocityDriven = sync.setVelocityInsteadOfPositionOnNonOwners;
            originalBackTime = sync.interpolationBackTime;
            originalExtrapolationMode = sync.extrapolationMode;
            originalTimeLimit = sync.useExtrapolationTimeLimit;
            originalDistanceLimit = sync.useExtrapolationDistanceLimit;
            originalExtrapolationTime = sync.extrapolationTimeLimit;
            originalExtrapolationDistance = sync.extrapolationDistanceLimit;
            previousValidation = sync.validateStateMethod;
            sync.validateStateMethod = ValidateState;
        }
    }
    void Update()
    {
        PrepareSync();
        if (!IsRemote) return;
        if (Time.unscaledTime >= nextLatencySample)
        {
            nextLatencySample = Time.unscaledTime + .5f;
            var network = NetworkManager.Singleton;
            if (network && network.IsListening && network.NetworkConfig.NetworkTransport != null)
            {
                // Clients measure their Relay route to the host; the host measures
                // the remote owner's route. No distance-based culling is applied.
                ulong peer = network.IsServer ? vehicle.OwnerClientId : NetworkManager.ServerClientId;
                roundTripSeconds = network.NetworkConfig.NetworkTransport.GetCurrentRtt(peer) * .001f;
            }
        }
        float sendInterval = 1f / Mathf.Max(1f, sync.sendRate);
        float targetBackTime = Mathf.Max(originalBackTime,
            Mathf.Min(.75f, roundTripSeconds + 2f * sendInterval));
        // Move playback time gradually; abrupt buffer changes can replay old poses.
        sync.interpolationBackTime = Mathf.MoveTowards(sync.interpolationBackTime,
            targetBackTime, .25f * Time.unscaledDeltaTime);
    }
    void FixedUpdate()
    {
        PrepareSync();
        if (!IsRemote || !vehicle.rb || vehicle.rb.isKinematic || Time.timeScale <= 0 ||
            Time.unscaledTime < resumeVelocityTime || sync.stateCount == 0 ||
            sync.stateBuffer == null || sync.stateBuffer.Length == 0) return;
        var latest = sync.stateBuffer[0];
        // Collisions and source contacts need the owner's actual velocity, not
        // (interpolation target - local position) / frame time. Keep remote bodies
        // dynamic so the existing vehicle collision response remains available.
        if (latest != null && OriginalVehiclePhysics.IsFinite(latest.velocity))
        {
            float receiveAge = sync.localTime - latest.receivedTimestamp;
            bool stale = receiveAge > sync.interpolationBackTime + sync.extrapolationTimeLimit;
            vehicle.rb.linearVelocity = stale || latest.atPositionalRest ? Vector3.zero : latest.velocity;
        }
    }
    bool IsRemote => sync && vehicle && vehicle.IsSpawned && !vehicle.IsOwner;
    void PrepareSync()
    {
        if (!sync) return;
        if (!IsRemote)
        {
            RestoreSyncSettings();
            return;
        }
        sync.setVelocityInsteadOfPositionOnNonOwners = false;
        sync.extrapolationMode = Smooth.SmoothSyncNetcode.ExtrapolationMode.Limited;
        sync.useExtrapolationTimeLimit = true;
        sync.extrapolationTimeLimit = Mathf.Min(Mathf.Max(.01f, originalExtrapolationTime), .15f);
        sync.useExtrapolationDistanceLimit = true;
        sync.extrapolationDistanceLimit = Mathf.Min(Mathf.Max(.1f, originalExtrapolationDistance), 15f);
    }
    void RestoreSyncSettings()
    {
        sync.setVelocityInsteadOfPositionOnNonOwners = velocityDriven;
        sync.interpolationBackTime = originalBackTime;
        sync.extrapolationMode = originalExtrapolationMode;
        sync.useExtrapolationTimeLimit = originalTimeLimit;
        sync.useExtrapolationDistanceLimit = originalDistanceLimit;
        sync.extrapolationTimeLimit = originalExtrapolationTime;
        sync.extrapolationDistanceLimit = originalExtrapolationDistance;
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
        if (sync) RestoreSyncSettings();
    }
}
