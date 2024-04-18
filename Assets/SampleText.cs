using RVP;
using System;
using UnityEngine;

public class SampleText : MonoBehaviour
{
	static Transform mainCamera;
	[NonSerialized]
	public TextMesh textMesh;
	VehicleParent vp;
	float heightOverCar = 2;
	const int textDistanceFromCamera = 5;
	const int DistanceWhenTextInvisible = 60;
	const int DistanceWhenTextTotallyVisible = 40;
	private void Awake()
	{
		textMesh = GetComponent<TextMesh>();
		vp = transform.parent.GetComponent<VehicleParent>();
		heightOverCar = transform.localPosition.y;
		if (mainCamera == null)
			mainCamera = GameObject.Find("MainCamera").transform;
	}
	void Start()
	{
		textMesh.color = F.ReadColor(transform.parent.GetComponent<VehicleParent>().sponsor);
		if (F.I.gameMode == MultiMode.Singleplayer)
			gameObject.SetActive(false);
	}

	void LateUpdate()
	{
		Vector3 vec = (vp.rb.position + Vector3.up * heightOverCar - mainCamera.position).normalized;
		transform.SetPositionAndRotation(mainCamera.position + textDistanceFromCamera * vec, Quaternion.LookRotation(vec));
		SetTransp(Mathf.InverseLerp(DistanceWhenTextInvisible, DistanceWhenTextTotallyVisible, Vector3.Distance(transform.parent.position, mainCamera.position)));
	}
	void SetTransp(float a)
	{
		var c = textMesh.color;
		c.a = a;
		textMesh.color = c;
	}
}
