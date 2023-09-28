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
	public bool scaled { get; private set; }
	MeshCollider mc;
	private void Awake()
	{
		// add mesh collider to 'main' mesh 
		if (transform.childCount == 0)
			mc = transform.gameObject.AddComponent<MeshCollider>();
		else
			mc = transform.GetChild(0).gameObject.AddComponent<MeshCollider>();

		mc.enabled = true;

		for (int i=1; i< transform.childCount; ++i)
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
		if(mc)
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
		return mc.transform.GetComponent<MeshFilter>().mesh.bounds.size.y;
	}
	Mesh CopyMesh(in Mesh mesh)
	{
		var newMesh = new Mesh()
		{
			vertices = mesh.vertices,
			triangles = mesh.triangles,
			normals = mesh.normals,
			tangents = mesh.tangents,
			bounds = mesh.bounds,
			uv = mesh.uv
		};
		return newMesh;
	}
	public void AdjustScale(float distance)
	{
		if (distance == 0)
			return;
		var mf = mc.transform.GetComponent<MeshFilter>();

		if(scaled)
		{
			mf.mesh = CopyMesh(mf.sharedMesh);
		}
		scaled = true;
		
		float scale = distance / mf.mesh.bounds.size.y;
		transform.localScale = new Vector3(1, 1, scale);

		{ // adjust UVs
			Vector2[] uvs = mf.mesh.uv;
			int submeshes = mf.mesh.subMeshCount;
			float[] maxUVYs = new float[submeshes];

			for (int i = 0; i < submeshes; ++i)
			{ // foreach material find max UV Y-coord
				int[] triangles = mf.mesh.GetTriangles(i);
				for (int j = 0; j < triangles.Length; ++j)
				{
					if (uvs[triangles[j]].y > maxUVYs[i])
						maxUVYs[i] = uvs[triangles[j]].y;
				}
				for (int j = 0; j < triangles.Length; ++j)
				{
					if (uvs[triangles[j]].y == maxUVYs[i])
						uvs[triangles[j]].y *= scale;
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
