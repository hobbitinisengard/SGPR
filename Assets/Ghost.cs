using RVP;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Ghost : NetworkBehaviour
{
	public bool hittable { get; private set; }
	public bool justResetted { get; private set; }
	public Collider[] colliders;
	public Renderer[] ghostableParts;
	VehicleParent vp;
	Coroutine ghostCo;
	static Shader opaqueShader;
	private void Awake()
	{
		vp = GetComponent<VehicleParent>();
		if (opaqueShader == null)
			opaqueShader = Shader.Find("HDRP/Lit");
	}
	public void SetHittable(bool isHittable, bool updateColliders = true)
	{
		if (updateColliders)
		{
			hittable = isHittable;
			foreach (var c in colliders)
			{
				c.gameObject.layer = isHittable ? F.I.carCarCollisionLayer : F.I.ghostLayer;
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
		//float specularIntensity = material.GetFloat("_SpecularIntensity");
		//float smoothness = material.GetFloat("_Glossiness");
		material.shader = opaqueShader;
		if(!material.name.Contains("tyre"))
		{
			material.SetFloat("_Metallic", 1);
			material.SetFloat("Smoothness", .5f);
		}
		material.SetFloat("_SurfaceType", 0);//SurfaceType.Opaque
		
		return material;
	}

	public Material ToFadeMode(Material material)
	{
		material.shader = F.I.transpShader;
		// Set Rendering Mode to Fade
		material.SetFloat("_Mode", 2); // 2 corresponds to Fade mode
		material.SetOverrideTag("RenderType", "Transparent");
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		material.SetInt("_ZWrite", 0);
		material.DisableKeyword("_ALPHATEST_ON");
		material.EnableKeyword("_ALPHABLEND_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.SetColor("_Color", new Color(1,1,1,.25f));
		// Set Color Mode to Multiply
		material.SetFloat("_ColorMode", 0); // 0 corresponds to Multiply mode
		material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		return material;
	}

	public void SetGhostPermanently()
	{
		if (F.I.gameMode == MultiMode.Multiplayer && vp.Owner)
			SetHittableRpc(false, true);
		else
			SetHittable(false, true);
	}
	[Rpc(SendTo.Everyone)]
	void SetHittableRpc(bool isHittable, bool updateColliders)
	{
		SetHittable(isHittable, updateColliders);
	}
	public void StartGhostResetting()
	{
		if (F.I.gameMode == MultiMode.Multiplayer)
			StartGhostResettingRpc();
		else
		{
			if (ghostCo != null)
				StopCoroutine(ghostCo);
			ghostCo = StartCoroutine(ResetSeq());
		}
	}
	[Rpc(SendTo.Everyone)]
	void StartGhostResettingRpc()
	{
		if (ghostCo != null)
			StopCoroutine(ghostCo);
		ghostCo = StartCoroutine(ResetSeq());
	}
	IEnumerator ResetSeq()
	{
		float timer = 5;
		float prev = 9;
		justResetted = true;
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
			}

			if (!F.I.gamePaused)
				timer -= Time.deltaTime;

			if (Physics.CheckSphere(transform.position, 1.5f, 1 << F.I.carCarCollisionLayer) && timer <= 0)
				timer = 1;
			yield return null;
			justResetted = false;
		}
		SetHittable(vp.raceBox.enabled && F.I.s_raceType != RaceType.TimeTrial);
	}
}
