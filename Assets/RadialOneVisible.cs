using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.UIElements.Experimental;
using TMPro;

public class RadialOneVisible : MonoBehaviour
{
	enum Type { Image, TMP};
	Type type;
	RadialLayout rad;
	public float targetValue;
	float initPos;
	float targetPos;
	Coroutine radialCo;
	Transform prevSelected;
	Transform selected;

	void Awake()
	{
		rad = GetComponent<RadialLayout>();
		type = transform.GetChild(0).GetComponent<Image>() ? Type.Image : Type.TMP;
		for (int i = 0; i < transform.childCount; ++i)
			SetColor(transform.GetChild(i),0);
	}
	private void OnDisable()
	{
		if (radialCo != null)
			StopCoroutine(radialCo);
		if (rad.gameObject.activeSelf)
			SetVisibility(1);
	}
	void SetColor(Transform child, float a)
	{
		if(type == Type.Image)
			child.GetComponent<Image>().color = new Color(255, 255, 255, a);
		else
			child.GetComponent<TextMeshProUGUI>().color = new Color(255, 255, 255, a);
	}
	float GetColor(Transform child)
	{
		if (type == Type.Image)
			return child.GetComponent<Image>().color.a;
		else
			return child.GetComponent<TextMeshProUGUI>().color.a;
	}
	public void SetChildrenActive(Transform imageContent)
	{
		for(int i=0; i<transform.childCount; ++i)
		{
			transform.GetChild(i).gameObject.SetActive(imageContent.GetChild(i).gameObject.activeSelf);
		}
	}
	public void SetAnimTo(int childIndex)
	{
		Transform child = transform.GetChild(childIndex);
		if (!child.gameObject.activeSelf)
		{
			Debug.LogError("Specified child is not active");
			return;
		}
		if (child == selected)
		{
			SetColor(selected, 1);
			return;
		}

		if (!selected)
			prevSelected = child;
		else
			prevSelected = selected;

		selected = child;

		if (radialCo != null)
			StopCoroutine(radialCo);
		if(rad.gameObject.activeSelf)
			radialCo = StartCoroutine(Set());
	}
	IEnumerator Set(float timer = 0)
	{
		yield return null;//gameobject's states of active/not active are refreshed after one frame
		initPos = rad.StartAngle / 360;
		targetPos = 1 - transform.PosAmongstActive(selected);
		
		while (timer <= 1.1f)
		{
			SetVisibility(timer);
			timer += Time.deltaTime;
			yield return null;
		}
	}
	void SetVisibility(float timer)
	{
		float ppTimer = Easing.OutCubic(timer);
		float value = Mathf.Lerp(initPos, targetPos, ppTimer);
		rad.StartAngle = 360 * (value);
		rad.CalculateLayoutInputHorizontal();
		for (int i = 0; i < transform.childCount; ++i)
		{
			Transform child = transform.GetChild(i);
			if (child == selected)
				SetColor(selected, Mathf.Clamp01(ppTimer));
			else if (child == prevSelected)
				SetColor(prevSelected, Mathf.Clamp01(1 - ppTimer));
			else
			{
				float curA = GetColor(child);
				SetColor(child, Mathf.Lerp(curA, 0, ppTimer));
			}
		}
	}
}

