using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DescrRecordsSwitcher : MonoBehaviour
{
   public TextMeshProUGUI[] graphicsA;
   public Text[] graphicsB;
   public int visibleTime;
   public int dimmerTime;
	float timer;
	bool visibleA = true;
	private void OnEnable()
	{
		timer = 0;
		SetVisibility(0, graphicsA);
		SetVisibility(0, graphicsB);
	}
	private void Update()
	{
		float input = 1;
		if (timer < dimmerTime)
			input = timer / dimmerTime;
		else if (timer > dimmerTime)
			input = (visibleTime - timer) / dimmerTime;

		SetVisibility(Mathf.Clamp01(input), visibleA ? graphicsA: graphicsB);

		timer += Time.deltaTime;
		if(timer > visibleTime)
		{
			timer = 0;
			visibleA = !visibleA;
		}
	}
	void SetVisibility(float a, Graphic[] graphics)
	{
		foreach(var g in graphics)
		{
			var c = g.color;
			c.a = a;
			g.color = c;
		}
	}
}
