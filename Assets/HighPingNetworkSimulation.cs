#if UNITY_EDITOR
using System.Linq;
using Unity.Multiplayer.PlayMode;
using Unity.Multiplayer.Tools.NetworkSimulator.Runtime;
using UnityEngine;

// Automatic, Editor-only network conditions for one Multiplayer Play Mode instance.
// Player tags belong to each process; other players and production builds are untouched.
public static class HighPingNetworkSimulation
{
    const string PlayerTag = "HighPing";
    const int PacketDelayMs = 150;
    const int PacketJitterMs = 30;
    const int PacketLossPercent = 2;
    static NetworkSimulator simulator;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => simulator = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Configure()
    {
        if (simulator || !CurrentPlayer.ReadOnlyTags().Contains(PlayerTag)) return;

        var holder = new GameObject("[MPPM] HighPing network simulation");
        holder.SetActive(false);
        Object.DontDestroyOnLoad(holder);
        simulator = holder.AddComponent<NetworkSimulator>();
        simulator.ConnectionPreset = NetworkSimulatorPreset.Create(
            name: "HighPing - 300 ms RTT + jitter + loss",
            description: "150 ms per direction, +/-30 ms jitter, 2% random packet loss. Actual ping also includes Relay latency.",
            packetDelayMs: PacketDelayMs,
            packetJitterMs: PacketJitterMs,
            packetLossInterval: 0,
            packetLossPercent: PacketLossPercent);
        holder.SetActive(true);
        Debug.Log("[HighPing] Network simulation enabled for this MPPM player: " +
            "150 ms delay per direction (~300 ms additional RTT), +/-30 ms jitter, 2% packet loss. " +
            "Adjust or disable the Network Simulator on '[MPPM] HighPing network simulation' during Play Mode.", simulator);
    }
}
#endif
