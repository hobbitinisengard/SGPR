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
		Countdown = (float)(F.I.raceStartDate - DateTime.UtcNow).TotalMilliseconds / 1000f;
		if (seq != null)
			StopCoroutine(seq);
		seq = StartCoroutine(CountdownSeq());
	}

	IEnumerator CountdownSeq()
	{
		float lastTime = Time.time;
		while (timer > 0)
		{
			if (timer < 4)
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
		timer = 0;

		if(ServerC.I.AmHost)
			OnlineCommunication.I.raceAlreadyStarted.Value = true;
	}
}
