using RVP;
using UnityEngine;

public class TrailScaler : MonoBehaviour
{
	VehicleParent vp;
	Vector3 initScale;
	int children;
	Renderer[] trailRenderers;
	bool[] rendererEnabled;
	bool visibilityInitialized;
	bool visible;

	void Start()
	{
		vp = transform.GetTopmostParentComponent<VehicleParent>();
		children = transform.childCount;
		if (children > 0) initScale = transform.GetChild(0).localScale;
		// Include renderers on this object: trail_color prefabs have no children.
		trailRenderers = GetComponentsInChildren<Renderer>(true);
		rendererEnabled = new bool[trailRenderers.Length];
		for (int i = 0; i < trailRenderers.Length; i++)
			rendererEnabled[i] = trailRenderers[i].enabled;
		UpdateVisibility();
	}

	void UpdateVisibility()
	{
		if (!vp) return;
		bool show = vp.AerodynamicTrailVisible;
		if (visibilityInitialized && show == visible) return;
		visibilityInitialized = true;
		visible = show;
		for (int i = 0; i < trailRenderers.Length; i++)
		{
			Renderer renderer = trailRenderers[i];
			if (!renderer) continue;
			renderer.enabled = show && rendererEnabled[i];
			if (renderer is TrailRenderer trail)
			{
				trail.emitting = show && rendererEnabled[i];
				if (!show) trail.Clear();
			}
		}
	}

	void LateUpdate()
	{
		UpdateVisibility();
		if (!vp || !visible || children == 0) return;
		float speedScale = Mathf.Clamp(vp.velMag, 0, 110) / 110;
		transform.GetChild(0).localScale = initScale * speedScale;
		for (int i = 1; i < Mathf.Min(children, 3); i++)
			transform.GetChild(i).localScale = Vector3.one * (0.5f * speedScale);
	}
}
