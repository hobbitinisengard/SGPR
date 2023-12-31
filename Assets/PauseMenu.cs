using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class PauseMenu : Sfxable
{
	public Button firstButton;
	public GameObject restartButton;
	public Image veil;
	public AudioMixer mainMixer;
	AudioSource clickSoundEffect;
	Color startColor;
	public Color blackColor;
	public float duration = 3;
	float timeElapsed = 0;
	private void Awake()
	{
		clickSoundEffect = GetComponent<AudioSource>();
		clickSoundEffect.ignoreListenerPause = true;
	}
	private void Update()
	{
		if (timeElapsed < duration)
		{
			// fade background
			veil.color = Color.Lerp(startColor, blackColor, timeElapsed / duration);
		}
		timeElapsed += Time.unscaledDeltaTime;
	}
	private void OnEnable()
	{
		Info.gamePaused = true;
		Time.timeScale = 0;
		timeElapsed = 0;
		restartButton.SetActive(Info.raceStartDate != DateTime.MinValue);
		startColor = veil.color;
		firstButton.Select();
	}
	private void OnDisable()
	{
		Info.gamePaused = false;
		Time.timeScale = 1;
		Info.SaveSettingsDataToJson(mainMixer);
		clickSoundEffect.Play();
		veil.color = startColor;
		Info.raceStartDate = Info.raceStartDate.AddSeconds(timeElapsed);
		timeElapsed = 0;
	}
}