using System;
using UnityEngine;

public class TrackCamera : MonoBehaviour
{
	[NonSerialized]
	public Connector connector;
	internal void SetMaterial(Material mat)
	{
		GetComponent<MeshRenderer>().material = mat;
	}
}