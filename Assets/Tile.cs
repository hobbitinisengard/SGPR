using System;
using System.Linq;
using UnityEngine;

public class Tile : MonoBehaviour
{
	[NonSerialized]
	public EditorPanel panel;
	public bool placed { get; private set; }
	public bool mirrored { get; private set; }
	public bool hasConnectors { get; private set; }
	private void Awake()
	{
		// add mesh collider to 'main' mesh 
		transform.GetChild(0).gameObject.AddComponent<MeshCollider>().enabled = false;
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
			hasConnectors = true;
		}
	}
	internal void SetPlaced()
	{
		placed = true;
		transform.GetChild(0).gameObject.layer = Info.roadLayer;
		transform.GetChild(0).GetComponent<MeshCollider>().enabled = true;
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
		var mf = transform.GetChild(0).GetComponent<MeshFilter>();
		var mc = transform.GetChild(0).GetComponent<MeshCollider>();
		mf.mesh = MirrorMesh(mf.mesh);
		if(mc)
			mc.sharedMesh = mf.mesh;
		if(hasConnectors)
		{
			Debug.Log("hasConnectors=true");
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
		}
		return mirrored;
	}
	
}
