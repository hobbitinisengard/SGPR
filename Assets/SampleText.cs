using RVP;
using UnityEngine;

public class SampleText : MonoBehaviour
{
	static Transform mainCamera;
	TextMesh textMesh;
	VehicleParent vp;
	float heightOverCar = 2;
	const int textDistanceFromCamera = 5;
	const int DistanceWhenTextInvisible = 60;
	const int DistanceWhenTextTotallyVisible = 40;
	void Start()
	{
		vp = transform.parent.GetComponent<VehicleParent>();
		heightOverCar = transform.localPosition.y;
		if (mainCamera == null)
			mainCamera = GameObject.Find("MainCamera").transform;
		textMesh = GetComponent<TextMesh>();
		textMesh.text = transform.parent.name;
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
