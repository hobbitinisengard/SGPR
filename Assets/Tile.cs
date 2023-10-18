using RVP;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static EditorPanel;

[DisallowMultipleComponent]
public class Tile : MonoBehaviour
{
	[NonSerialized]
	public EditorPanel panel;
	public bool placed { get; private set; }
	public bool mirrored { get; private set; }

	public string url;

	MeshCollider mc;

	private void Awake()
	{
		// add mesh collider to 'main' mesh 
		if (transform.childCount == 0)
			mc = gameObject.AddComponent<MeshCollider>();
		else
			mc = transform.GetChild(0).gameObject.AddComponent<MeshCollider>();

		mc.enabled = true;

		for (int i = 1; i < transform.childCount; ++i)
		{
			var connector = transform.GetChild(i).gameObject;
			var col = connector.AddComponent<SphereCollider>();
			col.radius = 5;
			col.isTrigger = true;
			var rb = connector.AddComponent<Rigidbody>();
			rb.useGravity = false;
			rb.isKinematic = true;
			connector.AddComponent<Connector>();
			connector.layer = Info.connectorLayer;
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
		mc.gameObject.layer = Info.roadLayer;
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
		mirrored = !mirrored;
		var mf = mc.transform.GetComponent<MeshFilter>();
		mf.mesh = MirrorMesh(mf.mesh);
		if (mc)
			mc.sharedMesh = mf.mesh;

		for (int i = 1; i < transform.childCount; ++i)
		{
			Transform connector = transform.GetChild(i);
			var p = connector.localPosition;
			p.x = -p.x;
			connector.localPosition = p;
			for (int j = 0; j < connector.childCount; ++j)
			{
				Transform path = connector.GetChild(j);
				for (int k = 0; k < path.childCount; ++k)
				{
					Vector3 a = connector.InverseTransformPoint(path.GetChild(k).position);
					a.x = -a.x;
					a = connector.TransformPoint(a);
					path.GetChild(k).position = a;
				}
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
		for (int i = 1; i < transform.childCount; ++i)
		{
			var connector = transform.GetChild(i);
			connector.transform.localScale = new Vector3(1, 1 / scale, 1);
			//for (int j = 0; j < connector.childCount; ++j)
			//{
			//	var pathParent = connector.GetChild(j);
			//	for (int k = 0; k < pathParent.childCount; ++k)
			//	{
			//		pathParent.GetChild(k).transform.localScale = new Vector3(1, 1, newScale);
			//	}
			//}
		}
	}
}
