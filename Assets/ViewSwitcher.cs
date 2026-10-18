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

	public GameObject arcadeMapView;
	public GameObject SPView;

	AudioSource menuMusic;

	float timer = 0;
	float duration;
	GameObject viewA;
	GameObject viewB;
	public event Action OnWorldMenuSwitch;
	bool applyScoring = false;
	private void Awake()
	{
		F.I.viewSwitcher = this;
		menuMusic = GetComponent<AudioSource>();
		duration = dimCurve.keys[dimCurve.length - 1].time;
	}
	public void SwitchBackgroundTo(in Sprite sprite) => background.SwitchBackgroundTo(sprite);
	IEnumerator Transition(bool cleanUp)
	{
		timer = 0;
		// Action method can take place over multiple frames which can disrupt the transition
		float delta = Mathf.Max(0.01f, Time.unscaledDeltaTime);
		while (timer < duration)
		{
			timer += delta;
			if (timer >= 0.5f * duration && viewA.activeSelf)
			{
				if (cleanUp)
				{
					CleanUp();
				}
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
		StartCoroutine(Transition(false));
	}
	/// <summary>
	/// switch between world <---> menu
	/// </summary>
	public void PlayDimmerWorldMenu(GameObject viewA, GameObject viewB)
	{
		this.viewA = viewA;
		this.viewB = viewB;
		// switch music if
		StartCoroutine(Transition(false));
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
		StartCoroutine(Transition(false));
	}
	public void PlayDimmerToMenu(bool applyScoring)
	{
		OnWorldMenuSwitch?.Invoke();
		menuMusic.Stop();
		this.viewA = world;
		this.viewB = menu;
		this.applyScoring = applyScoring;
		StartCoroutine(Transition(true));
	}
	void CleanUp()
	{
		bool splitScreen = F.I.gameMode == GameMode.Splitscreen;
		if (splitScreen && applyScoring) SplitScreenSelector.RecordResults();
		bool skateCarUnlocked = false;
		if (Championships.Active)
		{
			if (applyScoring) Championships.RecordRace();
			else Championships.CancelRace();
		}
		if (applyScoring && F.I.gameMode == GameMode.Skate)
		{
			var player = RaceManager.I.playerCar;
			var result = ResultsView.Get(player) ?? (player ? new Result(player) : null);
			F.I.rankingView.PrepareSkateResult(result);
			skateCarUnlocked = result != null && F.I.TryUnlockSkateCar(result.aeromiles);
		}
		RaceManager.I.editorPanel.gameObject.SetActive(true);
		RaceManager.I.RemoveCars();
		RaceManager.I.editorPanel.RemoveTrackLeftovers();
		Time.timeScale = 1;
		if (splitScreen)
		{
			MainMenuView destination = applyScoring ? resultsView.GetComponent<MainMenuView>() : SplitScreenSelector.I.thisView;
			foreach (var view in Resources.FindObjectsOfTypeAll<MainMenuView>())
				if (view != destination && view.transform.IsChildOf(menu.transform) && view.gameObject.activeSelf) view.gameObject.SetActive(false);
			destination.gameObject.SetActive(true);
			if (destination.music) { menuMusic.clip = destination.music; menuMusic.Play(); }
			return;
		}

		if (Championships.Active)
		{
			MainMenuView destination = applyScoring ? resultsView.GetComponent<ResultsView>() : ChampionshipUI.View("ChampionshipsView");
			foreach (var view in Resources.FindObjectsOfTypeAll<MainMenuView>())
				if (view != destination && view.transform.IsChildOf(menu.transform) && view.gameObject.activeSelf) view.gameObject.SetActive(false);
			destination.gameObject.SetActive(true);
			menuMusic.clip = destination.music; menuMusic.Play();
			return;
		}
		if (applyScoring)
		{
			if (F.I.gameMode == GameMode.Arcade)
			{
				arcadeMapView.SetActive(false);
				resultsView.SetActive(true);
			}
			else if (F.I.gameMode == GameMode.Multiplayer && ResultsView.Count > 1)
			{
				lobbyView.SetActive(false);
				resultsView.SetActive(true);
			}
			else if (F.I.gameMode == GameMode.Skate)
			{
				MainMenuView destination = F.I.rankingView;
				if (skateCarUnlocked)
				{
					var prize = resultsView.GetComponent<ResultsView>().prizeView;
					prize.Prepare(new System.Collections.Generic.List<string> { "car20" }, false, F.I.rankingView);
					destination = prize;
				}
				foreach (var view in Resources.FindObjectsOfTypeAll<MainMenuView>())
					if (view != destination && view.transform.IsChildOf(menu.transform) && view.gameObject.activeSelf)
						view.gameObject.SetActive(false);
				destination.gameObject.SetActive(true);
				menuMusic.clip = destination.music;
				menuMusic.Play();
				return;
			}
				menuMusic.clip = resultsView.GetComponent<ResultsView>().music;
			menuMusic.Play();
		}
	}
}
