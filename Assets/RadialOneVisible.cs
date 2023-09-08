using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.UIElements.Experimental;
using RVP;

public class RadialOneVisible : MonoBehaviour
{
	RadialLayout rad;
	AnimationCurve visibilityCurve = new AnimationCurve();
	public float targetValue;
	float initPos;
	float targetPos;
	Coroutine radialCo;
	Transform prevSelected;
	Transform selected;
	void Awake()
	{
		rad = GetComponent<RadialLayout>();
		visibilityCurve.AddKey(-1, 0);
		visibilityCurve.AddKey(0, 1);
		visibilityCurve.AddKey(1, 0);
		for (int i = 0; i < transform.childCount; ++i)
		{
			Image img = transform.GetChild(i).GetComponent<Image>();
			img.color = new Color(255, 255, 255, 0);
		}
	}
	public void SetChildrenActive(in bool[] isActive)
	{
		if(isActive.Length != transform.childCount)
		{
			Debug.LogError("wrong len");
			return;
		}
		for(int i=0; i<transform.childCount; ++i)
		{
			transform.GetChild(i).gameObject.SetActive(isActive[i]);
		}
	}
	public void SetAnimTo(int childIndex)
	{
		Transform child = transform.GetChild(childIndex);//(transform.childCount - childIndex) % transform.childCount);
		if (!child.gameObject.activeSelf)
		{
			Debug.LogError("Specified child is not active");
			return;
		}
		if (child == selected)
			return;

		if (!selected)
			prevSelected = child;
		else
			prevSelected = selected;

		selected = child;

		if (radialCo != null)
			StopCoroutine(radialCo);
		radialCo = StartCoroutine(Set());
	}
	IEnumerator Set()
	{
		yield return null;
		initPos = rad.StartAngle / 360;
		targetPos = 1 - transform.PosAmongstActive(selected);
		Debug.Log(targetPos);

		float timer = 0;
		
		while (timer <= 1.1f)
		{
			float ppTimer = Easing.OutCubic(timer);
			float value = Mathf.Lerp(initPos, targetPos, ppTimer);
			rad.StartAngle = 360 * (value);
			rad.CalculateLayoutInputHorizontal();

			prevSelected.GetComponent<Image>().color = new Color(255, 255, 255, Mathf.Clamp01(1 - ppTimer));
			selected.GetComponent<Image>().color = new Color(255, 255, 255, Mathf.Clamp01(ppTimer));
			
			timer += Time.deltaTime;
			yield return null;
		} 
	}
}

