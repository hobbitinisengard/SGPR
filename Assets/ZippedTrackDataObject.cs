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
		while (Info.mpSelector == null)
			yield return null;
		Info.mpSelector.zippedTrackDataObject = this;
	}

	public void RequestTrackUpdate(string newTrackName)
	{
		receivedTrack.Clear();
		UpdateTrackRpc(newTrackName, RpcTarget.Server);
	}
	[Rpc(SendTo.Server, AllowTargetOverride = true)]
	void UpdateTrackRpc(string newTrackName, RpcParams rpcParams)
	{
		StartCoroutine(UpdateTrack(rpcParams.Receive.SenderClientId, newTrackName));
	}
	IEnumerator UpdateTrack(ulong clientId, string newTrackName)
	{
		Debug.Log("Server: Update track");
		if (!updatingCachedTrack)
		{
			if (trackName != newTrackName)
			{
				updatingCachedTrack = true;
				Debug.Log("writing");
				string zipPath = Info.documentsSGPRpath + newTrackName + ".zip";
				if (File.Exists(zipPath))
					File.Delete(zipPath);
				using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
				{
					zip.CreateEntryFromFile(Info.tracksPath + newTrackName + ".track", newTrackName + ".track");
					zip.CreateEntryFromFile(Info.tracksPath + newTrackName + ".png", newTrackName + ".png");
					zip.CreateEntryFromFile(Info.tracksPath + newTrackName + ".data", newTrackName + ".data");
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
		string zipPath = Info.documentsSGPRpath + trackName + ".zip";
		File.WriteAllBytes(zipPath, receivedTrack.ToArray());
		ZipFile.ExtractToDirectory(zipPath, Info.documentsSGPRpath);

		bool localTrackExists = File.Exists(Info.tracksPath + trackName + ".data");
		if (localTrackExists)
		{
			// rename local track to trackName+number
			string newName = trackName + "0";
			for (int i = 1; i < 1000; ++i)
			{
				newName = trackName + i.ToString();
				if (!File.Exists(Info.tracksPath + newName + ".png"))
				{
					break;
				}
			}
			File.Move(Info.tracksPath + trackName + ".png", Info.tracksPath + newName + ".png");
			File.Move(Info.tracksPath + trackName + ".data", Info.tracksPath + newName + ".data");
			File.Move(Info.tracksPath + trackName + ".track", Info.tracksPath + newName + ".track");
			var header = new TrackHeader(Info.tracks[trackName]);
			Info.tracks.Add(newName, header);

			File.Move(Info.documentsSGPRpath + trackName + ".png", Info.tracksPath + trackName + ".png");
			File.Move(Info.documentsSGPRpath + trackName + ".data", Info.tracksPath + trackName + ".data");
			File.Move(Info.documentsSGPRpath + trackName + ".track", Info.tracksPath + trackName + ".track");

			string trackJson = File.ReadAllText(Info.tracksPath + trackName + ".track");
			header = JsonConvert.DeserializeObject<TrackHeader>(trackJson);
			Info.tracks[trackName] = header;
		}
		else
		{
			File.Move(Info.documentsSGPRpath + trackName + ".png", Info.tracksPath + trackName + ".png");
			File.Move(Info.documentsSGPRpath + trackName + ".data", Info.tracksPath + trackName + ".data");
			File.Move(Info.documentsSGPRpath + trackName + ".track", Info.tracksPath + trackName + ".track");

			string trackJson = File.ReadAllText(Info.tracksPath + trackName + ".track");
			var header = JsonConvert.DeserializeObject<TrackHeader>(trackJson);
			Info.tracks.Add(trackName, header);
		}
		File.Delete(zipPath);

		Info.mpSelector.ZippedTrackDataObject_OnNewTrackArrived();
	}
}
