using RVP;
using System.Collections;
using UnityEngine;

public class Ghost : MonoBehaviour
{
	public bool hittable { get; private set; }
	public Collider[] colliders;
	public MeshRenderer[] ghostableParts;
	VehicleParent vp;
	private void Awake()
	{
		vp = GetComponent<VehicleParent>();
	}
	public void SetHittable(bool isHittable, bool updateColliders = true)
	{
		if (updateColliders)
		{
			hittable = isHittable;
			foreach (var c in colliders)
			{
				c.gameObject.layer = hittable ? Info.vehicleLayer : Info.ghostLayer;
			}
		}
		foreach (var r in ghostableParts)
		{
			for(int i=0; i<r.materials.Length; ++i)
			{
				r.materials[i] = isHittable ? F.ToOpaqueMode(r.materials[i]) : F.ToFadeMode(r.materials[i]);
			}
		}
	}
	public IEnumerator ResetSeq()
	{
		float timer = 5;
		float prev = 9;
		SetHittable(false);
		while (timer > 0)
		{
			if (timer < 1)
			{
				if ((int)(timer * 10) != prev)
				{
					prev = (int)(timer * 10);
					SetHittable(prev % 2 == 0, false);
				}
				if(!Physics.CheckSphere(transform.position, 1.5f, 1<<Info.vehicleLayer))
					timer -= Time.deltaTime;
			}
			else
			{
				timer -= Time.deltaTime;
			}
			
			yield return null;
		}
		if(!vp.raceBox.Finished())
			SetHittable(true);
	}
}
