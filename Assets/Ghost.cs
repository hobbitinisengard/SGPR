using RVP;
using System.Collections;
using UnityEngine;

public class Ghost : MonoBehaviour
{
	public bool hittable { get; private set; }
	public Collider[] colliders;
	public MeshRenderer[] ghostableParts;
	VehicleParent vp;
	public Shader transpShader;
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
				r.materials[i] = isHittable ? ToOpaqueMode(r.materials[i]) : ToFadeMode(r.materials[i]);
			}
		}
	}
	public Material ToOpaqueMode(Material material)
	{
		material.shader = transpShader;
		material.SetInt("_ZWrite", 1);
		material.SetFloat("_IntensityTransparentMap", material.name.Contains("Roof") ? 0.2f : 0);

		material.SetFloat("_Glossiness", 1);
		material.SetFloat("_SpecularIntensity", .1f);
		material.SetFloat("_Parallax", 0);
		//material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
		//material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
		//material.SetInt("_ZWrite", 1);
		//material.DisableKeyword("_ALPHATEST_ON");
		//material.DisableKeyword("_ALPHABLEND_ON");
		//material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		//material.renderQueue = -1; // Set it back to the default opaque render queue

		return material;
	}

	public Material ToFadeMode(Material material)
	{
		material.shader = transpShader;
		material.SetInt("_ZWrite", 1);
		material.SetFloat("_IntensityTransparentMap", 0.7f);

		material.SetFloat("_Glossiness", 1);
		material.SetFloat("_SpecularIntensity", .1f);
		material.SetFloat("_Parallax", 0);
		//var c = material.color;
		//c.a = 0.5f;
		//material.color = c;

		// Set the rendering mode to transparent
		//material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		//material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		//material.SetInt("_ZWrite", 0);
		//material.DisableKeyword("_ALPHATEST_ON");
		//material.EnableKeyword("_ALPHABLEND_ON");
		//material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		//material.renderQueue = 3500;

		return material;
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
