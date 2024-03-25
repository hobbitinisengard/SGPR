using UnityEngine;

public class Dyndak : Sfxable
{
	float posy;
	RectTransform rt;
	float begintime = 0;
	new void Awake()
	{
		base.Awake();
		rt = GetComponent<RectTransform>();
		posy = rt.anchoredPosition.y;
		begintime = Time.time;
	}
	private void OnEnable()
	{
		PlaySFX("fe-iconappear");
	}

	void Update()
	{
		Vector3 pos = rt.anchoredPosition;
		pos.y = posy + 2 * 40 * Mathf.Abs(Mathf.Sin((Time.time - begintime) * 0.8f * Mathf.PI));
		rt.anchoredPosition = pos;
	}
}
