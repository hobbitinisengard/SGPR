using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class PauseMenu : Sfxable
{
	public SGP_HUD hud;
	public Button firstButton;
	public GameObject restartButton;
	public GameObject endButton;
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
		steerGamma.SetActive(F.I.controllerInUse);
		if(F.I.gameMode == MultiMode.Singleplayer)
			Time.timeScale = 0;
		F.I.gamePaused = true;
		paused.TransitionTo(0);
		timeElapsed = 0;
		restartButton.SetActive(!F.I.s_inEditor);
		endButton.SetActive(F.I.gameMode == MultiMode.Multiplayer && !ServerC.I.AmHost);
		startColor = veil.color;
		firstButton.Select();
		PlaySFX("menublip2", true);
	}
	private void OnDisable()
	{
		unPaused.TransitionTo(0);
		F.I.gamePaused = false;
		Time.timeScale = 1;
		F.I.SaveSettingsDataToJson(mainMixer);
		PlaySFX("menublip2",true);
		veil.color = startColor;

		if(!F.I.s_inEditor && F.I.gameMode == MultiMode.Singleplayer)
			F.I.raceStartDate = F.I.raceStartDate.AddSeconds(timeElapsed);
	}
}