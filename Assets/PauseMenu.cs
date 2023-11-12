using RVP;
using System;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;

public class PauseMenu : Sfxable
{
	public RaceManager rm;
	public Button firstButton;
	public GameObject restartButton;
	public Image veil;
	Color startColor;
	public Color blackColor;
	public float duration = 3;
	float timeElapsed;
	private void Update()
	{
		if (timeElapsed < duration)
		{
			// fade background
			veil.color = Color.Lerp(startColor, blackColor, timeElapsed / duration);
			timeElapsed += Time.unscaledDeltaTime;
		}
	}
	private void OnEnable()
	{
		Time.timeScale = 0;
		restartButton.SetActive(Info.raceStartDate != DateTime.MinValue);
		startColor = veil.color;
		firstButton.Select();
	}
	private void OnDisable()
	{
		Time.timeScale = 1;
		PlaySFX("menublip2");
		veil.color = startColor;
		timeElapsed = 0;
	}
}