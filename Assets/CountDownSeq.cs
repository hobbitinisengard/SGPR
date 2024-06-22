using RVP;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CountDownSeq : Sfxable
{
	public Sprite[] countdownSprites;
	Image img;
	Coroutine seq;
	public const float startRaceCountdownSecs = 5;

	static float timer;
	/// <summary>
	/// raceStart countdown
	/// </summary>
	public static float Countdown {
		get { return timer-1; }
		set { if(value > 0) timer = value+1; } // dimming of "GO" takes 1 sec
	}
	void OnEnable()
	{
		img = transform.GetChild(0).GetComponent<Image>();
		img.color = new Color(1, 1, 1, 0);
		Countdown = (float)(F.I.raceStartDate - DateTime.UtcNow).TotalSeconds;
		if (seq != null)
			StopCoroutine(seq);
		seq = StartCoroutine(CountdownSeq());
	}

	IEnumerator CountdownSeq()
	{
		int lastTime = Mathf.FloorToInt(timer);
		img.sprite = countdownSprites[3];

		while (timer > 0)
		{
			if (timer < 4)
			{
				if (timer % 1f > 0.5f)
					img.color = Color.white;
				else
					img.color = new Color(1, 1, 1, 2 * timer % 1f);
				if(Mathf.FloorToInt(timer) != lastTime)
				{
					lastTime = Mathf.FloorToInt(timer);
					img.sprite = countdownSprites[lastTime];
					PlaySFX((Mathf.FloorToInt(timer) == 0) ? "start2" : "start1");
				}
			}
			timer -= Time.deltaTime;
			yield return null;
		}
		gameObject.SetActive(false);
		timer = 0;

		if(ServerC.I.AmHost)
			Online.I.raceAlreadyStarted.Value = true;
	}
}
