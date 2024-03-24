using Unity.Netcode;

public class ZippedTrackDataObject : NetworkBehaviour
{
	public MultiPlayerSelector mpSelector;
	public NetworkVariable<byte[]> zippedTrack;
	/// <summary>
	/// SHA of the currently stored track. If null, no track is stored
	/// </summary>
	public NetworkVariable<string> zippedTrackSHA;
	public NetworkVariable<int> reading;

	[Rpc(SendTo.Server)]
	public void UpdateCachedTrack()
	{
		reading.Value++;

		mpSelector.UpdateCachedTrack();
	}
}
