using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class PauseMenu : Sfxable
{
	public SGP_HUD hud;
	public Button firstButton;
	public GameObject restartButton;
	public GameObject steerGamma;
	public Image veil;
	public AudioMixer mainMixer;
	public AudioMixerSnapshot paused;
	public AudioMixerSnapshot unPaused;
	AudioSource clickSoundEffect;
	Color startColor;
	public Color blackColor;
	public float duration = 3;
	float timeElapsed = 0;
	[NonSerialized]
	public bool controllerInUse = false;
	private new void Awake()
	{
		base.Awake();
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
		steerGamma.SetActive(Info.controllerInUse);
		Time.timeScale = 0;
		Info.gamePaused = true;
		paused.TransitionTo(0);
		timeElapsed = 0;
		restartButton.SetActive(Info.raceStartDate != DateTime.MinValue);
		startColor = veil.color;
		firstButton.Select();
		PlaySFX("menublip2", true);
	}
	private void OnDisable()
	{
		unPaused.TransitionTo(0);
		Info.gamePaused = false;
		Time.timeScale = 1;
		Info.SaveSettingsDataToJson(mainMixer);
		PlaySFX("menublip2",true);
		veil.color = startColor;

		if(!Info.s_inEditor)
			Info.raceStartDate = Info.raceStartDate.AddSeconds(timeElapsed);
		
		timeElapsed = 0;
	}
}