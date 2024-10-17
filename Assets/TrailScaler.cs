using RVP;
using System.Collections;
using UnityEngine;

public class TrailScaler : MonoBehaviour
{
	VehicleParent vp;
	Transform trailMaterial;
	Vector3 initScale;
	int children;
	private void Start()
	{
		vp = transform.GetTopmostParentComponent<VehicleParent>();
		children = transform.childCount;
		if (children>0)
		{
			initScale = transform.GetChild(0).localScale;
			StartCoroutine(Works());
		}
	}
	private void OnDestroy()
	{
		StopCoroutine(Works());
	}
	IEnumerator Works()
	{
		transform.GetChild(0).localScale = initScale * (Mathf.Clamp(vp.velMag, 0, 110) / 110);
		for (int i = 1; i < 3; ++i)
			transform.GetChild(i).localScale = Vector3.one * (0.5f * Mathf.Clamp(vp.velMag, 0, 110) / 110);
		yield return null;
	}
}
