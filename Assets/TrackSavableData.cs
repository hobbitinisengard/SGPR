using Vector3 = UnityEngine.Vector3;
using Vector2Int = UnityEngine.Vector2Int;
using Quaternion = UnityEngine.Quaternion;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using Unity.Collections;

[Serializable]
internal class TrackSavableData
{
	public Vector3 windExternal;
	public Vector3 windRandom;
	public List<string> tileNames;
	public List<TileSavable> tiles;
	public Vector3[] replayCams;
	public float[,] heights;
	public int initialRotation;
}

[Serializable]
public class ObjSavable
{
	public Vector3 position;
	public Quaternion rotation;
}

[Serializable]
public class TileSavable : ObjSavable
{
	public int name_id;
	public string url;
	public bool mirrored;
	public float length; // = tile distance
	public ConnectorSavable[] connectors;
}
[Serializable]
public class ConnectorSavable
{
	/// <summary>
	/// Example: (30;2) - connected to 30th tile's 2nd child
	/// </summary>
	public Vector2Int connectionData; 
	public int cameraID; // connected to id'th camera in camerasContainer
	public bool isStuntZone;
}

public class ByteWrapper : INetworkSerializable
{
	public byte data;
	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		if (serializer.IsWriter)
		{
			serializer.GetFastBufferWriter().WriteValueSafe(data);
		}
		else
		{
			serializer.GetFastBufferReader().ReadValueSafe(out data);
		}
	}
}



