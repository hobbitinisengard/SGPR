using RVP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Tile : MonoBehaviour
{
	[NonSerialized]
	public EditorPanel panel;
	/// <summary>
	/// Only one tile at a time may be not 'placed': the one that the player is currently selecting in track editor
	/// </summary>
	public bool placed { get; private set; }
	public bool mirrored { get; private set; }

	public string url;

	public MeshCollider mc { get; private set; }

	GameObject lightObj;

	public MeshCollider[] Endings { get; private set; }

	public void UpdateLights()
	{
		if(lightObj)
		{
			for (int i = 0; i < lightObj.transform.childCount; ++i)
			{
				lightObj.transform.GetChild(i).gameObject.SetActive(F.I.s_isNight);
			}
		}
	}
	private void Awake()
	{
		// add mesh collider to 'main' mesh 
		if (transform.childCount == 0) // tile isn't a road
			mc = gameObject.AddComponent<MeshCollider>();
		else
		{ // tile is a road
			var childObj = transform.GetChild(0);
			if (childObj.name == "lights")
			{
				mc = gameObject.AddComponent<MeshCollider>();

				lightObj = childObj.gameObject;
				UpdateLights();
			}
			else
			{
				mc = childObj.gameObject.AddComponent<MeshCollider>();

				if(childObj.childCount > 0)
				{
					List<MeshCollider> endings = new();
					for (int i = 0; i < childObj.childCount; ++i)
					{ // each ending requires mesh collider
						GameObject mainMeshChild = childObj.GetChild(i).gameObject;
						if (mainMeshChild.name[..3] == "end")
						{
							var ending = mainMeshChild.AddComponent<MeshCollider>();
							endings.Add(ending);
						}
					}
					if(endings.Count > 0)
						Endings = endings.ToArray();
				}
			}
		}
		mc.enabled = true;
		if(F.I.s_roadType == PavementType.Random)
		{
			Debug.LogError("PavementType is random");
		}
		else if(F.I.s_roadType != PavementType.Arena)
		{
			var mr = mc.transform.GetComponent<MeshRenderer>();
			string replacementStr = "0" + ((int)F.I.s_roadType).ToString();
			var materials = mr.materials;
			for (int i = 0; i < materials.Length; ++i)
			{
				if (materials[i].name.Contains("00"))
				{
					var newName = materials[i].name.Replace("00", replacementStr).Split(' ')[0];
					materials[i] = Resources.Load<Material>("materials/" + newName);
				}
			}
			mr.materials = materials;
		}

		for (int i = 1; i < transform.childCount; ++i)
		{
			var connector = transform.GetChild(i).gameObject;
			var col = connector.AddComponent<SphereCollider>();
			col.radius = 3;
			col.isTrigger = true;
			var rb = connector.AddComponent<Rigidbody>();
			rb.useGravity = false;
			rb.isKinematic = true;
			connector.AddComponent<Connector>();
			connector.layer = F.I.connectorLayer;
			var mf = connector.AddComponent<MeshFilter>();
			var mr = connector.AddComponent<MeshRenderer>();
			mf.mesh = Resources.Load<Mesh>("sphere");
			mr.enabled = true;
			mr.material = Connector.blue;
		}
	}

	internal void SetPlaced()
	{
		placed = true;
		mc.gameObject.layer = F.I.roadLayer;
		if (name.Contains("dirt")) //= mud
			mc.gameObject.AddComponent<GroundSurfaceInstance>().surfaceType = 1;
		else if (name.Contains("sand")) // =dust
			mc.gameObject.AddComponent<GroundSurfaceInstance>().surfaceType = 2;
		else if (name.Contains("ice"))
			mc.gameObject.AddComponent<GroundSurfaceInstance>().surfaceType = 3;
		else
			mc.gameObject.AddComponent<GroundSurfaceInstance>().surfaceType = 0;

		var etp = GetComponent<EnergyTunnelPath>();
		if (etp)
			etp.CalculatePitsPath();
	}
	Mesh MirrorMesh(Mesh mesh)
	{
		Vector3[] verts = mesh.vertices;
		for (int i = 0; i < verts.Length; i++)
			verts[i] = new Vector3(-verts[i].x, verts[i].y, verts[i].z);
		mesh.vertices = verts;

		for (int i = 0; i < mesh.subMeshCount; i++) // Every material has to be assigned with triangle array
		{
			int[] trgs = mesh.GetTriangles(i);
			mesh.SetTriangles(trgs.Reverse().ToArray(), i);
		}
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		return mesh;
	}
	public bool MirrorTile()
	{
		// mirroring crossings is useless and breaks the code
		if (name.Contains("crossing"))
			return false;

		mirrored = !mirrored;
		
		var mf = mc.transform.GetComponent<MeshFilter>();
		mf.mesh = MirrorMesh(mf.mesh);
		if (mc)
			mc.sharedMesh = mf.mesh;

		if(Endings != null)
		{
			foreach (MeshCollider end in Endings)
			{ // mirror endings
				mf = end.GetComponent<MeshFilter>();
				mf.mesh = MirrorMesh(mf.mesh);
				end.sharedMesh = mf.mesh;
			}
		}

		if(transform.childCount>0)
		{
			if(transform.GetChild(0).name == "lights")
			{
				var lightsObj = transform.GetChild(0);
				for (int i=0; i< lightsObj.childCount; ++i)
				{
					var light = lightsObj.GetChild(i);
					Vector3 a = transform.InverseTransformPoint(light.position);
					a.x = -a.x;
					light.position = transform.TransformPoint(a);

					var lookVector = light.forward;
					lookVector.x = -lookVector.x;
					light.rotation = Quaternion.LookRotation(lookVector);
				}
			}
			var mainMeshTr = transform.GetChild(0);
			if (mainMeshTr.childCount > 0)
			{
				for (int i = 0; i < mainMeshTr.childCount; ++i)
				{
					var pos = mainMeshTr.GetChild(i).transform.localPosition;
					pos.x = -pos.x;
					mainMeshTr.GetChild(i).transform.localPosition = pos;
					var euler = mainMeshTr.GetChild(i).transform.localEulerAngles;
					euler.y = -euler.y;
					euler.z = -euler.z;
					mainMeshTr.GetChild(i).transform.localRotation = Quaternion.Euler(euler);
				}
			}
		}
		
		for (int i = 1; i < transform.childCount; ++i)
		{
			Transform connector = transform.GetChild(i);
			
			// mirror path positions relative to connector
			Transform[] paths = new Transform[connector.childCount];

			for (int j = 0; connector.childCount > 0; ++j)
			{
				Transform path = connector.GetChild(0); // incrementally disconnecting
				path.parent = null;
				paths[j] = path;
				for (int k = 0; k < path.childCount; ++k)
				{
					Vector3 lPos = path.GetChild(k).localPosition;// connector.InverseTransformPoint(path.GetChild(k).position); //
					lPos.x = -lPos.x;
					path.GetChild(k).localPosition = lPos;
					//path.GetChild(k).position = connector.TransformPoint(lPos);
				}
			}

			// mirror connector position and its rotation
			var p = connector.localPosition;
			p.x = -p.x;
			var euler = connector.localEulerAngles;
			euler.y = -euler.y;
			euler.z = -euler.z;
			connector.SetLocalPositionAndRotation(p, Quaternion.Euler(euler));

			foreach (var c in paths)
			{
				if (c != null)
					c.SetParent(connector,true);
			}
		}
		return mirrored;
	}
	public float Length()
	{
		return transform.localScale.z * mc.transform.GetComponent<MeshFilter>().mesh.bounds.size.y;
	}
	public void AdjustScale(float distance)
	{
		if (distance == 0)
			return;
		var mf = mc.transform.GetComponent<MeshFilter>();

		//if(scaled)
		//{
		//	if(!original)
		//	{
		//		Debug.LogError("No original UVs");
		//		return;
		//	}
		//	mf.mesh.uv = original.transform.GetChild(0).GetComponent<MeshFilter>().mesh.uv;
		//}
		//scaled = true;

		float scale = distance / mf.mesh.bounds.size.y;
		transform.localScale = new Vector3(1, 1, scale);
		{ // adjust UVs
			Vector2[] uvs = mf.mesh.uv;
			int submeshes = mf.mesh.subMeshCount;
			for (int i = 0; i < submeshes; ++i)
			{ // foreach material find max UV Y-coord
				int[] triangles = mf.mesh.GetTriangles(i);
				float maxUVY = 0;
				float minUVY = 999;
				for (int j = 0; j < triangles.Length; ++j)
				{
					if (uvs[triangles[j]].y > maxUVY)
						maxUVY = uvs[triangles[j]].y;
					if (uvs[triangles[j]].y < minUVY)
						minUVY = uvs[triangles[j]].y;
				}
				for (int j = 0; j < triangles.Length; ++j)
				{
					if (uvs[triangles[j]].y == maxUVY)
						uvs[triangles[j]].y = Mathf.LerpUnclamped(minUVY, maxUVY, scale);
				}
			}
			mf.mesh.uv = uvs;
		}
		// make connectors round again
		int scaleX = 1;// mirrored ? -1 : 1;
		for (int i = 1; i < transform.childCount; ++i)
		{
			var connector = transform.GetChild(i);
			
			if (connector.childCount > 0)
			{
				GameObject[] children = new GameObject[connector.childCount];
				for(int j = 0; j< children.Length; ++j)
				{ // every time you change parent of a prev child, you pick 0th child 
					children[j] = connector.GetChild(0).gameObject;
					children[j].transform.parent = connector.parent;
				}
				connector.localScale = new Vector3(scaleX, 1 / scale, 1);
				foreach (var c in children)
				{
					c.transform.parent = connector;
				}
			}
			else
				connector.localScale = new Vector3(scaleX, 1 / scale, 1);
		}
	}
}
