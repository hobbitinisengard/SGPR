using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using NUnit.Framework;

public class MainMenuView : Sfxable
{
	[NonSerialized]
	public MainMenuView prevView;
	public Image dyndak;
	public Button firstButtonToBeSelected;
	public TextMeshProUGUI bottomText;
	public Sprite bgTile;
	public AudioClip music;
	public YouSureDialog youSureDialog;
	public bool prevViewForbidden;
	static ViewSwitcher dimmer;
	bool champNameSubmitted;
	protected override void Awake()
	{
		base.Awake();

		if (dimmer == null)
			dimmer = transform.GetParentComponent<ViewSwitcher>();
	}
	private void Start()
	{
		if (!F.I.loaded)
		{
			F.I.loaded = true;
			PlaySFX("fe-cardssuccess");
		}
	}

	void CancelPressed(InputAction.CallbackContext obj)
	{
		GoBack();
	}
	/// <summary>
	/// Go backwards
	/// </summary>
	/// <param name="ignoreYouSure"></param>
	public void GoBack(bool ignoreYouSure = false)
	{
		if(prevViewForbidden)
		{
			PlaySFX("fe-warning");
			return;
		}

		if (gameObject.activeSelf && (ignoreYouSure || youSureDialog == null))
		{
			if(prevView)
			{
				SwitchView(prevView);
				PlaySFX("fe-dialogcancel");
			}
		}
		else
		{
			youSureDialog.gameObject.SetActive(true);
		}
	}
	public void OpenUsersManual()
	{
		Application.OpenURL(F.I.usersManualLinks[(int)F.I.playerData.language]);
	}
	/// <summary>
	/// Go forward
	/// </summary>
	public void GoToView(MainMenuView view)
	{
		if (name == "EnterNameView" && F.I.gameMode == GameMode.Championships)
		{
			if (champNameSubmitted) return;
			var input = GetComponentInChildren<EnterNameInputField>(true);
			string driverName = input ? input.GetInputField() : F.I.playerData.playerName;
			if (string.IsNullOrWhiteSpace(driverName)) return;
			if (Championships.Current == null) Championships.Create(driverName);
			view = ChampionshipUI.View("ChampSeasonSelection");
			if (!view) { Debug.LogError("ChampSeasonSelection view is missing.", this); return; }
			champNameSubmitted = true;
		}
		if (!view) return;
		if (view.prevView == null || (view != prevView && !view.prevViewForbidden && !prevViewForbidden))
			view.prevView = this;
		
		SwitchView(view);
	}
	void SwitchView(MainMenuView view)
	{
		if (view && view.name == "SPView" && F.I.gameMode == GameMode.Championships) Championships.Leave();
		if (view == null)
			return;
		for (int i = 0; i < transform.childCount; ++i)
		{
			if (transform.GetChild(i).gameObject.activeSelf)
				F.PlaySlideOutOnChildren(transform.GetChild(i));
		}
		dimmer.PlayDimmer(this, view);
	}
	protected virtual void OnDisable()
	{
		F.I.escRef.action.started -= CancelPressed;
		// get current selection and save it 
		firstButtonToBeSelected = EventSystem.current.currentSelectedGameObject?.GetComponent<Button>() ?? firstButtonToBeSelected;
	}
	protected virtual void OnEnable()
	{
		champNameSubmitted = false;
		F.I.escRef.action.started += CancelPressed;
		if (firstButtonToBeSelected)
			firstButtonToBeSelected.Select();
		
		dimmer.SwitchBackgroundTo(bgTile);
	}
	public void StartQuickRace()
	{
		var info = F.I;
		int carIndex = 0;
		int carCount = 0;
		string trackName = null;
		int trackCount = 0;
		string fallbackTrack = null;
		int fallbackTrackCount = 0;

		// Reservoir sampling selects each eligible entry with equal probability, without a list.
		for (int i = 0; i < info.cars.Length; ++i)
		{
			if (info.cars[i].unlocked && UnityEngine.Random.Range(0, ++carCount) == 0)
				carIndex = i;
		}
		foreach (var track in info.tracks)
		{
			if (track.Key.Length <= 3 || !track.Value.valid) continue;
			if (UnityEngine.Random.Range(0, ++fallbackTrackCount) == 0)
				fallbackTrack = track.Key;
			if (track.Value.unlocked && UnityEngine.Random.Range(0, ++trackCount) == 0)
				trackName = track.Key;
		}

		trackName ??= fallbackTrack;
		if (info.cars.Length == 0 || trackName == null)
		{
			PlaySFX("fe-cardserror");
			return;
		}

		info.gameMode = GameMode.QuickRace;
		info.s_raceType = RaceType.Race;
		info.teams = false;
		info.s_spectator = false;
		info.s_playerCarIdx = carIndex;
		info.s_cpuRivals = 5;
		info.s_inEditor = false;
		info.s_cpuLevel = (CpuLevel)UnityEngine.Random.Range(0, 3);
		info.s_trackName = trackName;
		info.s_timeOfDay = (TimeOfDay)UnityEngine.Random.Range(0, Info.TimeOfDays);
		info.s_laps = UnityEngine.Random.Range(3, 11);
		ToRaceScene();
	}
	public void ToRaceScene()
	{
		if (F.I.s_trackName == null || F.I.s_trackName.Length < 4)
			PlaySFX("fe-cardserror");
		else
		{
			PlaySFX("fe-gameload");
			if (F.I.s_roadType == PavementType.Random)
				F.I.s_roadType = F.RandomRoadType();

			for (int i = 0; i < transform.childCount; ++i)
			{
				if (transform.GetChild(i).gameObject.activeSelf)
					F.PlaySlideOutOnChildren(transform.GetChild(i));
			}
			F.I.s_inEditor = false;
			dimmer.PlayDimmerToWorld();
		}
	}
	public void ToEditorScene()
	{
		if (F.I.s_trackName == "MEX")
			PlaySFX("fe-cardserror");
		else
		{
			if(F.I.s_trackName.Length == 3)
			{
				F.I.s_roadType = PavementType.Arena;
			}
			else if (F.I.s_roadType == PavementType.Random)
				F.I.s_roadType = F.RandomRoadType();
			F.I.s_inEditor = true;
			F.I.s_spectator = false;
			dimmer.PlayDimmerToWorld();
		}
	}

	public void QuitGame()
	{
		Application.Quit();
	}
}
