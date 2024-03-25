using Unity.Netcode;

public class ZippedTrackDataObject : NetworkBehaviour
{
	public NetworkVariable<byte[]> zippedTrack = new(new byte[0]);
	/// <summary>
	/// SHA of the currently stored track. If null, no track is stored
	/// </summary>
	public NetworkVariable<string> zippedTrackSHA = new("");

	public void Spawn()
	{
		GetComponent<NetworkObject>().Spawn();
	}
}
