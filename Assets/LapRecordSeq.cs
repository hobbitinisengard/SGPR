using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LapRecordSeq : Sfxable
{
	Image lapImg;
	Image recImg;
	float initPosLap;
	float initPosRec;
	public AnimationCurve transpCurve;
	Coroutine animCo;
	void Awake()
	{
		lapImg = transform.GetChild(0).GetComponent<Image>();
		recImg = transform.GetChild(1).GetComponent<Image>();
		initPosLap = transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition.x;
		initPosRec = transform.GetChild(1).GetComponent<RectTransform>().anchoredPosition.x;
	}
	private void OnEnable()
	{
		if (animCo != null)
			StopCoroutine(animCo);
		animCo = StartCoroutine(PlayAnim());
	}
	IEnumerator PlayAnim()
	{
		PlaySFX("swoosh2");
		float animDuration = 3;
		float timer = 0;
		float lapTarget = Mathf.Abs(initPosLap) + transform.parent.GetComponent<RectTransform>().rect.width;
		float recTarget = -(initPosRec + transform.parent.GetComponent<RectTransform>().rect.width);
		while (timer < animDuration)
		{
			Vector2 p = lapImg.rectTransform.anchoredPosition;
			p.x = Mathf.Lerp(initPosLap, lapTarget, timer / animDuration);
			lapImg.rectTransform.anchoredPosition = p;

			p = recImg.rectTransform.anchoredPosition;
			p.x = Mathf.Lerp(initPosRec, recTarget, timer / animDuration);
			recImg.rectTransform.anchoredPosition = p;

			var c = lapImg.color;
			c.a = transpCurve.Evaluate(timer / animDuration);
			lapImg.color = c;
			recImg.color = c;

			timer += Time.deltaTime;
			yield return null;
		}
		gameObject.SetActive(false);
	}
}
