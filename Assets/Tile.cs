using RVP;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class Tile : MonoBehaviour
{
	private void OnDestroy()
	{ // restore turned off connections of other tiles when destroying this tile
		for(int i=0; i<4; ++i)
		{
			if (disabledConnectors[i] != null)
				disabledConnectors[i].enabled = true;
		}
	}
	[DllImport("user32.dll")]
	static extern bool SetCursorPos(int X, int Y);
	[NonSerialized]
	public EditorPanel panel;
	Collider[] disabledConnectors = new Collider[4];

	public bool placed { get; private set; }
	public bool mirrored { get; private set; }
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
		return mesh;
	}
	public void MirrorTile()
	{
		mirrored = !mirrored;
		var mf = transform.GetChild(0).GetComponent<MeshFilter>();
		var mc = transform.GetChild(0).GetComponent<MeshCollider>();
		mf.mesh = MirrorMesh(mf.mesh);
		mc.sharedMesh = mf.mesh;
		for (int i = 1; i < transform.childCount; ++i)
		{
			var p = transform.GetChild(i).position;
			p.x = -p.x;
			transform.GetChild(i).position = p;
		}
	}
	private void OnTriggerStay(Collider other)
	{
		if(!placed)
		{
			panel.placedConnector = other.transform.position;
		}
		else
		{
			if (other.transform.FindParentComponent<Tile>().placed)
			{ // both connectors are placed, disable other one
				other.enabled = false;

				// add connector to array
				for (int i = 0; i<4; ++i)
				{
					if (disabledConnectors[i] == null)
					{
						disabledConnectors[i] = other;
						break;
					}
				}
			}
			else
				panel.floatingConnector = other.transform.position;
		}
	}
	private void OnTriggerExit(Collider other)
	{
		if (!placed)
		{
			panel.placedConnector = null;
		}
		else
		{
			panel.floatingConnector = null;
		}
	}
}
