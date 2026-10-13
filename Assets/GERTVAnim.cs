using UnityEngine;

public class GERTVAnim : MonoBehaviour
{
	// Three consecutive materials per set, ordered a, b, c.
	public Material[] animMaterials;

	const float SourceTickDuration = 1f / 60f;
	readonly Renderer[] panels = new Renderer[3];
	readonly Material[] originalMaterials = new Material[3];
	readonly Material[] panelMaterials = new Material[3];
	readonly Vector2[] offsets = new Vector2[3];
	readonly Vector2[] steps = new Vector2[3];
	float accumulator;

	void OnEnable()
	{
		accumulator = 0;
		int setCount = Mathf.Min(3, (animMaterials?.Length ?? 0) / 3);
		if (setCount == 0 || transform.childCount < 3)
		{
			Debug.LogWarning("GERTVAnim requires three child panels and complete a/b/c material sets.", this);
			return;
		}

		int selectedSet = Random.Range(0, setCount);
		for (int panel = 0; panel < 3; panel++)
		{
			Material source = animMaterials[selectedSet * 3 + panel];
			Renderer renderer = transform.GetChild(panel).GetComponentInChildren<Renderer>(true);
			if (!source || !renderer) continue;

			panels[panel] = renderer;
			originalMaterials[panel] = renderer.sharedMaterial;
			panelMaterials[panel] = new Material(source);
			renderer.sharedMaterial = panelMaterials[panel];
			offsets[panel] = Vector2.zero;
			// The GER prefab contains the sets in different orders on its two TVs.
			int track = source.name.Contains("tv16") ? 16 : source.name.Contains("tv21") ? 21 :
				source.name.Contains("tv22") ? 22 : selectedSet == 0 ? 16 : selectedSet == 1 ? 21 : 22;
			steps[panel] = SourceUvStep(track, panel);
			ApplyOffset(panel);
		}
	}

	// Original track PMD motion records (kind 1), advanced by 4258A0.
	// Panel order a/b/c differs from the material table's order in each PMD.
	static Vector2 SourceUvStep(int track, int panel)
	{
		if (track == 16)
			return panel == 0 ? new Vector2(.0075f, .0075f) :
				panel == 1 ? new Vector2(0, -.0075f) : new Vector2(-.01f, -.01f);
		if (track == 21)
			return panel == 0 ? new Vector2(.05f, .005f) :
				panel == 1 ? new Vector2(0, -.0075f) : Vector2.zero;
		return panel == 0 ? new Vector2(.015f, -.015f) :
			panel == 1 ? new Vector2(-.015f, .015f) : new Vector2(0, -.01f);
	}

	void Update()
	{
		if (F.I != null && F.I.gamePaused) return;
		accumulator += Time.deltaTime;
		bool advanced = false;
		while (accumulator >= SourceTickDuration)
		{
			accumulator -= SourceTickDuration;
			for (int panel = 0; panel < 3; panel++)
			{
				offsets[panel] += steps[panel];
				offsets[panel].x = WrapSourceOffset(offsets[panel].x);
				offsets[panel].y = WrapSourceOffset(offsets[panel].y);
			}
			advanced = true;
		}
		if (advanced)
			for (int panel = 0; panel < 3; panel++)
				ApplyOffset(panel);
	}

	static float WrapSourceOffset(float value)
	{
		if (value > 1) value -= 1;
		if (value < -1) value += 1;
		return value;
	}

	void ApplyOffset(int panel)
	{
		Material material = panelMaterials[panel];
		if (!material) return;
		// Convert the original Direct3D downward V axis to Unity's upward V axis.
		Vector2 unityOffset = new Vector2(offsets[panel].x, -offsets[panel].y);
		material.mainTextureOffset = unityOffset;
	}

	void OnDisable()
	{
		for (int panel = 0; panel < 3; panel++)
		{
			if (!panelMaterials[panel]) continue;
			if (panels[panel]) panels[panel].sharedMaterial = originalMaterials[panel];
			Destroy(panelMaterials[panel]);
			panelMaterials[panel] = null;
			steps[panel] = Vector2.zero;
		}
	}
}
