using RVP;
using UnityEngine;

public class TrailScaler : MonoBehaviour
{
	VehicleParent vp;
	Transform trailMaterial;
	void Awake()
	{
		vp = transform.GetTopmostParentComponent<VehicleParent>();
		//trailMaterial = transform.GetChild(transform.childCount - 1);
	}

	// Update is called once per frame
	void Update()
	{
		transform.localScale = Vector3.one * (0.5f * Mathf.Clamp(vp.velMag, 0, 110) / 110);
		//trailMaterial.localScale = 1 / transform.localScale.x * Vector3.one;
	}
}
