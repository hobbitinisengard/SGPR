using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountDownSeq : Sfxable
{
	public Sprite[] countdownSprites;
	Image img;
	Coroutine seq;
	void OnEnable()
	{
		img = transform.GetChild(0).GetComponent<Image>();
		img.color = new Color(1,1,1,0);
		if (seq != null)
			StopCoroutine(seq);
		seq = StartCoroutine(CountdownSeq());
	}

	IEnumerator CountdownSeq()
	{ 
		float timer = 5;
		float lastTime = Time.time;
		while(timer>0)
		{
			if(timer < 4)
			{
				if (timer % 1f > 0.5f)
					img.color = Color.white;
				else
					img.color = new Color(1, 1, 1, 2 * timer % 1f);

				img.sprite = countdownSprites[Mathf.FloorToInt(timer)];
				if (Time.time - lastTime >= 1)
				{
					PlaySFX((Mathf.FloorToInt(timer) == 0) ? "start2" : "start1");
					lastTime = Time.time;
				}
			}
			timer -= Time.deltaTime;
			yield return null;
		}
		gameObject.SetActive(false);
	}
}
