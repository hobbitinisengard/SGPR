using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Dimmer : MonoBehaviour
{
	public Image blackness;
	public AnimationCurve dimCurve;
	public BackgroundTiles background;
	float timer = 0;
	float duration;
	GameObject viewA;
	GameObject viewB;
	private void Start()
	{
		duration = dimCurve.keys[dimCurve.length - 1].time;
	}
	public void SwitchBackgroundTo(in Sprite sprite) => background.SwitchBackgroundTo(sprite);
	IEnumerator Play()
	{
		while(true)
		{
			if (timer >= duration)
			{
				blackness.gameObject.SetActive(false);
				yield break;
			}
			else if (viewA.activeSelf && timer >= 0.5f * duration)
			{
				//Debug.Log("switch");
				viewA.SetActive(false);
				viewB.SetActive(true);
			}
			var c = blackness.color;
			c.a = dimCurve.Evaluate(timer);
			blackness.color = c;
			timer += Time.deltaTime;
			yield return null;
		}
	}
	public void PlayDimmer(GameObject viewA, GameObject viewB)
	{
		this.viewA = viewA;
		this.viewB = viewB;
		if (!viewB.GetComponent<MainMenuView>().prevView)
			viewB.GetComponent<MainMenuView>().prevView = viewA;
		blackness.gameObject.SetActive(true);
		timer = 0;
		StartCoroutine(Play());
	}
}
