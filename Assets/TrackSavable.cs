using System;
using Vector3 = UnityEngine.Vector3;
using Vector2Int = UnityEngine.Vector2Int;
using Quaternion = UnityEngine.Quaternion;
using System.Collections.Generic;

[Serializable]
internal class TrackSavable
{
	// heightmap - track_h.png,  picture - track.png
	public string desc;
	public string author;
	public int unlocked;
	public int difficulty;
	public Info.Envir envir;
	public Info.CarGroup prefCarGroup;
	public Vector3 windExternal;
	public Vector3 windRandom;
	public int valid;
	public Vector3[] Lpath;
	public Vector3[] Rpath;
	public List<string> tileNames;
	public List<TileSavable> tiles;
	public Vector3[] replayCams;
	public int[] icons;
	public float[,] heights;
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
	/// Example: (30;2) - connected to 30th tile's 2nd anchor
	/// </summary>
	public Vector2Int connectionData; 
	public int cameraID; // connected to id'th camera in camerasContainer
	public bool isStuntZone;
}

