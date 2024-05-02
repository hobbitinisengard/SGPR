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
	AnimationCurve anim;
	private void Awake()
	{
		anim = new AnimationCurve(new Keyframe[]
		{
			new (0,0),
			new (dimmerTime,1),
			new (visibleTime-dimmerTime,1),
			new (visibleTime,0)
		});
	}
	private void OnEnable()
	{
		timer = 0;
		SetVisibility(0, graphicsA);
		SetVisibility(0, graphicsB);
	}
	private void Update()
	{
		SetVisibility(anim.Evaluate(timer), visibleA ? graphicsA: graphicsB);

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
