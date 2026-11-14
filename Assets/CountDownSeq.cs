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
	public static event Action OnRaceStarted;
	static float timer;
	/// <summary>
	/// raceStart countdown
	/// </summary>
	public static float Countdown {
		get { return timer-1; }
		set { timer = value > 0 ? value + 1 : 0; } // dimming of "GO" takes 1 sec
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
		bool startSignalled = false;
		img.sprite = countdownSprites[3];

		while (timer > 0)
		{
			// Read the synchronized deadline every frame. Late date corrections
			// and slow frames cannot leave a detached static timer blocking input.
			timer = Mathf.Max(0, (float)(F.I.raceStartDate - DateTime.UtcNow).TotalSeconds + 1);
			if (timer < 4)
			{
				if (timer % 1f > 0.5f)
					img.color = Color.white;
				else
					img.color = new Color(1, 1, 1, 2 * timer % 1f);
				int currentTime = Mathf.Clamp(Mathf.FloorToInt(timer), 0, 3);
				if (currentTime != lastTime)
				{
					lastTime = currentTime;
					img.sprite = countdownSprites[currentTime];
					if (currentTime > 0) PlaySFX("start1");
				}
			}
			if (timer <= 1 && !startSignalled)
			{
				startSignalled = true;
				PlaySFX("start2");
				OnRaceStarted?.Invoke();
			}
			yield return null;
		}
		// A late join or slow load can start this coroutine after GO. The
		// race-start event still needs to run exactly once for this sequence.
		if (!startSignalled) OnRaceStarted?.Invoke();
		timer = 0;
		if (F.I.gameMode == GameMode.Multiplayer && Online.I && Online.I.IsSpawned && Online.I.IsServer)
			Online.I.raceAlreadyStarted.Value = true;
		gameObject.SetActive(false);
	}
}
