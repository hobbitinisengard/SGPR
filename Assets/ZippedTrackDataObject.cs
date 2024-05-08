using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Unity.Netcode;
using UnityEngine;

struct SerializableByteArray : INetworkSerializable
{
	public int length;
	public byte[] data;
	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		int length = 0;
		if(!serializer.IsReader) // serializer writes its own data
		{
			length = data.Length;
		}
		serializer.SerializeValue(ref length);

		if(serializer.IsReader) // serializer reads its own data
		{
			data = new byte[length];
		}
		for (int i = 0; i < length; ++i)
		{
			serializer.SerializeValue(ref data[i]);
		}
	}
	public SerializableByteArray(byte[] data)
	{
		this.data = data;
		length = this.data.Length;
	}
}
public class ZippedTrackDataObject : NetworkBehaviour
{
	byte[] zippedTrack;
	List<byte> receivedTrack = new(1500000);
	string trackName = "";
	bool updatingCachedTrack = false;
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		StartCoroutine(Initialize());
	}
	IEnumerator Initialize()
	{
		while (MultiPlayerSelector.I == null)
			yield return null;
		MultiPlayerSelector.I.zippedTrackDataObject = this;
	}

	public void RequestTrackUpdate()
	{
		receivedTrack.Clear();
		Debug.Log("RequestTrackUpdate");
		UpdateTrackRpc(RpcTarget.Server);
	}
	[Rpc(SendTo.Server, AllowTargetOverride = true)]
	void UpdateTrackRpc(RpcParams rpcParams)
	{
		StartCoroutine(UpdateTrack(rpcParams.Receive.SenderClientId, F.I.s_trackName));
	}
	IEnumerator UpdateTrack(ulong clientId, string newTrackName)
	{
		Debug.Log("Server: Update track " + newTrackName);
		if (!updatingCachedTrack)
		{
			if (trackName != newTrackName)
			{
				updatingCachedTrack = true;
				Debug.Log("writing");
				string zipPath = F.I.documentsSGPRpath + newTrackName + ".zip";
				if (File.Exists(zipPath))
					File.Delete(zipPath);
				using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
				{
					zip.CreateEntryFromFile(F.I.tracksPath + newTrackName + ".track", newTrackName + ".track");
					zip.CreateEntryFromFile(F.I.tracksPath + newTrackName + ".png", newTrackName + ".png");
					zip.CreateEntryFromFile(F.I.tracksPath + newTrackName + ".data", newTrackName + ".data");
				}
				zippedTrack = File.ReadAllBytes(zipPath);
				trackName = newTrackName;
				updatingCachedTrack = false;
			}
		}
		else
		{
			while (updatingCachedTrack)
				yield return null;
		}

		Debug.Assert(zippedTrack != null);

		// max amount of data you can send at once is 1400 bytes
		int maxB = 1024;
		for (int i = 0; i < zippedTrack.Length; i += maxB)
		{
			int Lastindex = ((i + maxB) > zippedTrack.Length) ? zippedTrack.Length : i + maxB;
			SerializableByteArray data = new(zippedTrack[i..Lastindex]);
			SendTrackPacketToClientRpc(data, RpcTarget.Single(clientId, RpcTargetUse.Persistent));
		}
		FullTrackSentToClientRpc(newTrackName, RpcTarget.Single(clientId, RpcTargetUse.Persistent));
	}

	[Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
	void SendTrackPacketToClientRpc(SerializableByteArray zippedTrackSBA, RpcParams ps)
	{
		receivedTrack.AddRange(zippedTrackSBA.data);
	}

	[Rpc(SendTo.SpecifiedInParams, Delivery = RpcDelivery.Reliable)]
	void FullTrackSentToClientRpc(string trackName, RpcParams ps)
	{
		string zipPath = F.I.documentsSGPRpath + trackName + ".zip";
		File.WriteAllBytes(zipPath, receivedTrack.ToArray());
		ZipFile.ExtractToDirectory(zipPath, F.I.documentsSGPRpath);

		bool localTrackExists = File.Exists(F.I.tracksPath + trackName + ".data");
		if (localTrackExists)
		{
			// rename local track to trackName+number
			string newName = trackName + "0";
			for (int i = 1; i < 1000; ++i)
			{
				newName = trackName + i.ToString();
				if (!File.Exists(F.I.tracksPath + newName + ".png"))
				{
					Debug.LogWarning($"Local track already exists. Saving as {newName}");
					break;
				}
			}
			File.Move(F.I.tracksPath + trackName + ".png", F.I.tracksPath + newName + ".png");
			File.Move(F.I.tracksPath + trackName + ".data", F.I.tracksPath + newName + ".data");
			File.Move(F.I.tracksPath + trackName + ".track", F.I.tracksPath + newName + ".track");
			var header = new TrackHeader(F.I.tracks[trackName]);
			F.I.tracks.Add(newName, header);

			File.Move(F.I.documentsSGPRpath + trackName + ".png", F.I.tracksPath + trackName + ".png");
			File.Move(F.I.documentsSGPRpath + trackName + ".data", F.I.tracksPath + trackName + ".data");
			File.Move(F.I.documentsSGPRpath + trackName + ".track", F.I.tracksPath + trackName + ".track");

			string trackJson = File.ReadAllText(F.I.tracksPath + trackName + ".track");
			header = JsonConvert.DeserializeObject<TrackHeader>(trackJson);
			F.I.tracks[trackName] = header;
		}
		else
		{
			Debug.Log($"Downloaded new track {trackName}");
			File.Move(F.I.documentsSGPRpath + trackName + ".png", F.I.tracksPath + trackName + ".png");
			File.Move(F.I.documentsSGPRpath + trackName + ".data", F.I.tracksPath + trackName + ".data");
			File.Move(F.I.documentsSGPRpath + trackName + ".track", F.I.tracksPath + trackName + ".track");

			string trackJson = File.ReadAllText(F.I.tracksPath + trackName + ".track");
			var header = JsonConvert.DeserializeObject<TrackHeader>(trackJson);
			F.I.tracks.Add(trackName, header);
		}
		File.Delete(zipPath);

		MultiPlayerSelector.I.ZippedTrackDataObject_OnNewTrackArrived();
	}
}
