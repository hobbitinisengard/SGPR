using System.Collections;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class SelectorBaloon : MonoBehaviour
{
   public RectTransform border;
   public RectTransform MaskObj;
	Vector2 targetBorder;
	Vector2 targetMaskObj;
	Vector2 initBorder;
	Vector2 initMaskObj;
	Coroutine animCo;
	private void Awake()
	{
		targetBorder = border.sizeDelta;
		targetMaskObj = MaskObj.sizeDelta;
		initBorder = 0.1f*border.sizeDelta;
		initMaskObj = 0.1f*MaskObj.sizeDelta;
	}
	private void OnEnable()
	{
		if (animCo != null)
			StopCoroutine(animCo);
		animCo = StartCoroutine(Baloonment());
	}

	IEnumerator Baloonment()
	{
		float timer = 0;
		
		while(timer < 1.1f)
		{
			border.sizeDelta = Vector2.Lerp(initBorder, targetBorder, Easing.OutCubic(timer));
			MaskObj.sizeDelta = Vector2.Lerp(initMaskObj, targetMaskObj, Easing.OutCubic(timer));
			timer += Time.deltaTime;
			yield return null;
		}
	}
}
