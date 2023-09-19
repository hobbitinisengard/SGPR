using System;
using UnityEngine;

public class TrackCamera : MonoBehaviour
{
	internal void SetMaterial(Material mat)
	{
		GetComponent<MeshRenderer>().material = mat;
	}
}