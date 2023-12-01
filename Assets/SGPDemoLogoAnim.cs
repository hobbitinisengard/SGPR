using UnityEngine;
using UnityEngine.UI;

public class SGPDemoLogoAnim : MonoBehaviour
{
	Image image;
	const float showHideDuration = 5;
	const float visibleDuration = 5;
	const float hiddenDuration = 30;
	float timer;
	enum Visibility { showing, visible, hiding, hidden }
	Visibility vis = Visibility.showing;
	private void OnEnable()
	{
		image = GetComponent<Image>();
		timer = 0;
		vis = Visibility.hidden;
		SetVisibility(0);
	}
	void FixedUpdate()
	{
		switch (vis)
		{
			case Visibility.showing:
				SetVisibility(timer/showHideDuration);
				if (timer >= showHideDuration)
				{
					vis = Visibility.visible;
					timer = 0;
				}
				break;
			case Visibility.visible:
				if (timer >= visibleDuration)
				{
					vis = Visibility.hiding;
					timer = 0;
				}
				break;
			case Visibility.hiding:
				SetVisibility(1 - timer/showHideDuration);
				if (timer >= showHideDuration)
				{
					vis = Visibility.hidden;
					timer = 0;
				}
				break;
			case Visibility.hidden:
				if (timer >= hiddenDuration)
				{
					vis = Visibility.showing;
					timer = 0;
				}
				break;
		}
		timer += Time.fixedDeltaTime;
	}
	void SetVisibility(float val)
	{
		var c = image.color;
		c.a = val;
		image.color = c;
	}
}