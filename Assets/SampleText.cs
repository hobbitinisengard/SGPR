using RVP;
using UnityEngine;

public class SampleText : MonoBehaviour
{
	static Transform mainCamera;
	TextMesh textMesh;
	ClientNetworkTransform cnt;
	float heightOverCar = 2;
	const int textDistanceFromCamera = 5;
	const int DistanceWhenTextInvisible = 60;
	const int DistanceWhenTextTotallyVisible = 40;
	Vector3 lastPos;
	void Start()
	{
		cnt = transform.parent.GetComponent<ClientNetworkTransform>();
		heightOverCar = transform.localPosition.y;
		if(mainCamera == null)
			mainCamera = GameObject.Find("MainCamera").transform;
		textMesh = GetComponent<TextMesh>();
		textMesh.text = transform.parent.name;
		textMesh.color = F.ReadColor(transform.parent.GetComponent<VehicleParent>().sponsor);

		if (F.I.gameMode == MultiMode.Singleplayer)
			gameObject.SetActive(false);
	}

	void Update()
	{
		lastPos = Vector3.Lerp(lastPos, transform.parent.position, 10 * Time.deltaTime);
		Vector3 vec = (lastPos + Vector3.up * heightOverCar - mainCamera.position).normalized;
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
