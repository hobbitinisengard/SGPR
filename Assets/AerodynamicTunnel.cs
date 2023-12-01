using RVP;
using UnityEngine;

public class AerodynamicTunnel : MonoBehaviour
{
	VehicleParent vp;
	BoxCollider box;
	float range;
	void Start()
	{
		vp = transform.parent.GetComponent<VehicleParent>();
		box = GetComponent<BoxCollider>();
	}

	void Update()
	{
		range = vp.velMag;
		box.center = new Vector3(0, -range/2, 0);
		var size = box.size;
		size.y = range;
		box.size = size;
		//transform.localScale = Vector3.one * (0.5f * Mathf.Clamp(vp.velMag, 0, 110) / 110);
	}
}
