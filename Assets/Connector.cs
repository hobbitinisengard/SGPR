using RVP;
using System;
using UnityEngine;

public class Connector : MonoBehaviour
{
	[NonSerialized]
	public Connector connection;
	[NonSerialized]
	public TrackCamera trackCamera;
	Tile tile;
	public static Material blue;
	public static Material red;
	public static Material green;
	public static Material pink;
	public bool visible { get; private set; }
	void Start()
	{
		if(!blue)
		{
			blue = Resources.Load<Material>("materials/blue");
			red = Resources.Load<Material>("materials/red");
			green = Resources.Load<Material>("materials/green");
			pink = Resources.Load<Material>("materials/pink");
		}
		tile = transform.parent.GetComponent<Tile>();
	}
	public void SetCamera(TrackCamera cam)
	{
		if(trackCamera)
			trackCamera.SetMaterial(pink);
		trackCamera = cam;
		trackCamera.SetMaterial(green);
	}
	public Connector Opposite()
	{
		// first element in other's tile hierarchy is always main mesh
		// main
		// connectorA		<-- 0
		//		LeftLine (from A to B)
		//			waypoint1
		//			waypoint2
		//		RightLine
		//			waypoint1
		//			waypoint2
		// connectorB		<-- 1			(always no children)  
		// connectorC		<-- 2
		//		LeftLine (from C to D)
		//			waypoint1
		//			waypoint2
		//		RightLine
		//			waypoint1
		//			waypoint2
		// connectorD		<-- 3			(always no children) 
		int nextIdx = NextConnectorIndex(transform.GetSiblingIndex());
		return transform.parent.GetChild(nextIdx).GetComponent<Connector>();
	}
	int NextConnectorIndex(int siblingIdx)
	{
		return siblingIdx + (((siblingIdx - 1) % 2 == 0) ? 1 : -1);
	}
	public void Show(in Material mat)
	{
		MeshRenderer mr;
		MeshFilter mf;
		bool success = transform.TryGetComponent(out mf);
		if (!success)
		{
			mf = transform.gameObject.AddComponent<MeshFilter>();
			mf.mesh = Resources.Load<Mesh>("sphere");
			mr = transform.gameObject.AddComponent<MeshRenderer>();
		}
		else
			mr = transform.GetComponent<MeshRenderer>();
		mr.enabled = true;
		visible = true;
		mr.material = mat;
	}
	public void Hide()
	{
		bool success = transform.TryGetComponent(out MeshRenderer mr);
		if (success)
			mr.enabled = false;
		visible = false;
	}
	public void Paths(out Vector3[] Lpath, out Vector3[] Rpath)
	{
		int idxWithPaths;
		bool reverse = (transform.GetSiblingIndex()-1) % 2 == 1;
		if (reverse)
			idxWithPaths = transform.GetSiblingIndex() - 1;
		else
			idxWithPaths = transform.GetSiblingIndex();

		//if (!tile.mirrored)
		//{
			Lpath = ListPositions(transform.parent.GetChild(idxWithPaths).GetChild(0), reverse);
			Rpath = ListPositions(transform.parent.GetChild(idxWithPaths).GetChild(1), reverse);
		//}
		//else
		//{
		//	Rpath = ListPositions(transform.parent.GetChild(idxWithPaths).GetChild(0), reverse);
		//	Lpath = ListPositions(transform.parent.GetChild(idxWithPaths).GetChild(1), reverse);
		//}
	}
	Vector3[] ListPositions(Transform node, bool reverse)
	{
		Vector3[] positions = new Vector3[node.childCount];
		for(int i=0; i<positions.Length; ++i)
		{
			positions[reverse ? positions.Length - 1 - i : i] = node.GetChild(i).position;
		}
		return positions;
	}
	private void OnDestroy()
	{ // restore turned off connections of other tiles when destroying this tile
		if(connection)
			connection.GetComponent<Collider>().enabled = true;
		if (trackCamera)
			trackCamera.SetMaterial(pink);
	}
	private void OnTriggerStay(Collider otherCollider)
	{
		if (!tile.placed)
		{
			tile.panel.placedConnector = otherCollider.transform.position;
		}
		else
		{
			if (otherCollider.transform.FindParentComponent<Tile>().placed)
			{ // both connectors are placed, disable other one
				otherCollider.enabled = false;
				connection = otherCollider.GetComponent<Connector>();
			}
			else
				tile.panel.floatingConnector = otherCollider.transform.position;
		}
	}
	private void OnTriggerExit(Collider other)
	{
		if (!tile.placed)
		{
			tile.panel.placedConnector = null;
		}
		else
		{
			tile.panel.floatingConnector = null;
		}
	}
}
