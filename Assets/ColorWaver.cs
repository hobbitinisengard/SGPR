using UnityEngine;

public class ColorWaver : MonoBehaviour
{
	MeshRenderer mr;
	Color baseColor;
	void Start()
	{
		mr = GetComponent<MeshRenderer>();
		baseColor = mr.material.color;
	}
	void Update()
	{
		mr.material.color = new Color(baseColor.r, baseColor.g, baseColor.b,
			0.5f + Mathf.Sin(Time.time * 2 * Mathf.PI) / 2f);
	}
}
