using RVP;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndraceCountdownTimer : MonoBehaviour
{
	Coroutine cntdown;
	AudioSource snd;
	public Color beginColor;
	public Color endColor;
	public Image[] overlayImages123;
	public Sprite[] overlaySpritesArray;
	private void Awake()
	{
		snd = GetComponent<AudioSource>();
	}
	public void OnEnable()
	{
		if (cntdown != null)
			StopCoroutine(cntdown);
		cntdown = StartCoroutine(Countdown());
	}
	public void OnDisable()
	{
		gameObject.SetActive(false);
	}
	void SetNumber(int number)
	{
		number = Mathf.Clamp(number, 0, 999);
		for (int i = 2; i >= 0; --i)
		{
			int letter = number % 10;
			overlayImages123[i].sprite = overlaySpritesArray[letter];
			number /= 10;
		}
	}
	IEnumerator Countdown()
	{
		float timer;
		if (F.I.gameMode == GameMode.Skate)
		{
			timer = F.I.s_SkateTimeLimit;
		}
		else
			timer = Info.AfterMultiPlayerRaceWaitForPlayersSeconds;

		int round = Mathf.RoundToInt(timer);
		SetNumber(round);
		while (timer > 0)
		{
			timer = Mathf.Max(0f, timer - Time.deltaTime);
			if (round != Mathf.FloorToInt(timer))
			{
				if (F.I.gameMode == GameMode.Multiplayer && ResultsView.FinishedPlayers >= ServerC.I.lobby.Players.Count)
				{
					gameObject.SetActive(false);
					yield break;
				}
				round = Mathf.FloorToInt(timer);
				SetNumber(round);
				if (round < 10)
					snd.Play();
			}
			yield return null;
		}
		RaceManager.I.TimeForRaceEnded();
		gameObject.SetActive(false);
	}
}
