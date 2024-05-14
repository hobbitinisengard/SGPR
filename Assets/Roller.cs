using System.Collections;
using UnityEngine;

public class Roller : MonoBehaviour
{
	RectTransform rt;
	float height;
	public float target = 0;
	public float current = 0;
	float coeff = 10f;
	bool clockWork = true;
	float pos0;
	float scale;
	Coroutine playCo;
	void Awake()
	{
		rt = transform.GetComponent<RectTransform>();
		height = rt.sizeDelta.y;
		pos0 = rt.anchoredPosition.y;
		scale = rt.localScale.y;
	}
	private void OnEnable()
	{
		PlayAnim();
	}

	IEnumerator Move()
	{
		while(Mathf.Abs(current - target) > Mathf.Epsilon)
		{
			if (!clockWork)
			{
				current = target;
			}
			else if (Mathf.Abs(current - target) > Mathf.Epsilon) // current != target
			{
				if (current >= 0.99f && target > 0 && target < 1f)
				{
					current = 0;
				}
				float step = Time.deltaTime * coeff;
				current = Mathf.Lerp(current, target > current ? target : 1, step);
			}
			Vector3 pos = rt.anchoredPosition;
			pos.y = Mathf.Lerp(pos0, scale * height - pos0, current);
			rt.anchoredPosition = pos;

			yield return null;
		}
	}
	public void SetActive(bool value)
	{
		gameObject.SetActive(value);
	}
	/// <summary>
	/// Sets roller to targetValue with centering effect
	/// </summary>
	public void SetValue(float newTarget)
	{
		newTarget = Mathf.Clamp(newTarget, 0, 9);
		clockWork = true;

		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		newTarget /= 10f;
		if (Mathf.Abs(newTarget - target) > Mathf.Epsilon)
		{
			target = newTarget;
			PlayAnim();
		}
	}
	/// <summary>
	/// Sets roller to fraction without centering effect
	/// </summary>
	public void SetFrac(float frac)
	{
		clockWork = false;

		if (!gameObject.activeSelf)
			gameObject.SetActive(true);

		target = frac;
		PlayAnim();
	}
	void PlayAnim()
	{
		if(enabled)
		{
			if (playCo != null)
				StopCoroutine(playCo);
			playCo = StartCoroutine(Move());
		}
	}
}
