// Class for tire mark instances
using UnityEngine;

public class TireMark : MonoBehaviour
{
	[System.NonSerialized]
	public float fadeTime = -1;
	bool fading;
	float alpha = 1;
	[System.NonSerialized]
	public Mesh mesh;
	[System.NonSerialized]
	public Color[] colors;

	// Fade the tire mark and then destroy it
	void Update()
	{
		if (fading)
		{
			if (alpha <= 0)
			{
				Destroy(mesh);
				Destroy(gameObject);
			}
			else
			{
				alpha -= Time.deltaTime;

				for (int i = 0; i < colors.Length; i++)
				{
					colors[i].a -= Time.deltaTime;
				}

				mesh.colors = colors;
			}
		}
		else
		{
			if (fadeTime > 0)
			{
				fadeTime = Mathf.Max(0, fadeTime - Time.deltaTime);
			}
			else if (fadeTime == 0)
			{
				fading = true;
			}
		}
	}
}