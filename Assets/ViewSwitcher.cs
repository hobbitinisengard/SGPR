using RVP;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class ViewSwitcher : MonoBehaviour
{
	public Image blackness;
	public AnimationCurve dimCurve;
	public BackgroundTiles background;
	public AudioMixerGroup audioMusic;
	public AudioMixerGroup audioSFX;
	public GameObject world;
	public GameObject menu;

	public GameObject lobbyView;
	public GameObject resultsView;

	AudioSource menuMusic;

	float timer = 0;
	float duration;
	GameObject viewA;
	GameObject viewB;
	public event Action OnWorldMenuSwitch;
	private void Awake()
	{
		F.I.viewSwitcher = this;
		menuMusic = GetComponent<AudioSource>();
		duration = dimCurve.keys[dimCurve.length - 1].time;
	}
	public void SwitchBackgroundTo(in Sprite sprite) => background.SwitchBackgroundTo(sprite);
	IEnumerator Transition(Action method = null)
	{
		timer = 0;
		// Action method can take place over multiple frames which can disrupt the transition
		float delta = Mathf.Max(0.01f, Time.unscaledDeltaTime);
		while (timer < duration)
		{
			timer += delta;
			if (timer >= 0.5f * duration && viewA.activeSelf)
			{
				method?.Invoke();
				viewA.SetActive(false);
				viewB.SetActive(true);
				SetBlacknessColor(1);
			}
			else
			{
				SetBlacknessColor(dimCurve.Evaluate(timer));
			}
			blackness.gameObject.SetActive(true);
			yield return null;
		}
		blackness.gameObject.SetActive(false);
	}
	void SetBlacknessColor(float a)
	{
		var c = blackness.color;
		c.a = a;
		blackness.color = c;
	}
	public void PlayDimmer(MainMenuView viewA, MainMenuView viewB)
	{
		this.viewA = viewA.gameObject;
		this.viewB = viewB.gameObject;
		
		// switch music if
		if (viewB.music && (!viewA.music || viewA.music != viewB.music))
		{
			menuMusic.clip = viewB.music;
			menuMusic.loop = menuMusic.clip.length > 30;
			menuMusic.Play();
		}
		
		StartCoroutine(Transition());
	}
	/// <summary>
	/// switch between world <---> menu
	/// </summary>
	public void PlayDimmerWorldMenu(GameObject viewA, GameObject viewB)
	{
		this.viewA = viewA;
		this.viewB = viewB;
		// switch music if
		StartCoroutine(Transition());
	}
	/// <summary>
	/// Dims to targetVisibility. 0 = menu fully visible, 1 = blackness
	/// </summary>
	public void PlayDimmerToWorld()
	{
		OnWorldMenuSwitch?.Invoke();
		menuMusic.Stop();
		this.viewA = menu;
		this.viewB = world;
		StartCoroutine(Transition());
	}
	public void PlayDimmerToMenu(bool applyScoring)
	{
		OnWorldMenuSwitch?.Invoke();
		menuMusic.Stop();
		this.viewA = world;
		this.viewB = menu;
		
		StartCoroutine(Transition(() => 
		{
			RaceManager.I.editorPanel.gameObject.SetActive(true);
			RaceManager.I.RemoveCars();
			RaceManager.I.editorPanel.RemoveTrackLeftovers();
			Time.timeScale = 1;

			if (applyScoring && F.I.gameMode == MultiMode.Multiplayer && ResultsView.Count > 1)
			{
				lobbyView.SetActive(false);
				resultsView.SetActive(true);
			}
		}));
	}
}
