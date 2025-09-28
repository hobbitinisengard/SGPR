using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
public class SplitScreenPlayerInfo
{
	public string name;
	public float score;
	public int car;
}
public class SplitScreenSelector : TrackSelector
{
	public static SplitScreenSelector I;
	public MainMenuView thisView;
	public MainMenuView enterNameView;

	public LeaderBoardTable leaderboard;
	public TextMeshProUGUI splitScreenPlayersNumberText;
	public TextMeshProUGUI ipText;
	public TextMeshProUGUI randomCarsText;
	public TextMeshProUGUI randomTracksText;
	public TextMeshProUGUI readyText;
	public TextMeshProUGUI scoringText;
	public TextMeshProUGUI roundText;
	public Button garageBtn;
	public GameObject dataTransferWnd;
	public TextMeshProUGUI dataTransferText;
	public CarSelector carSelector;

	int splitScreenPlayerEnteringName = 0;
	[NonSerialized]
	private Coroutine lobbyCntdwnCo;
	private Coroutine afterEnabledCo;
	List<string> AvailableTracksForRandomSession = new();
	List<SplitScreenPlayerInfo> splitScreenPlayers = new();

	protected override void Awake()
	{
		// minimum number of split screen players is 2
		splitScreenPlayers.Add(new SplitScreenPlayerInfo());
		splitScreenPlayers.Add(new SplitScreenPlayerInfo());
		I = this;
		garageBtn.onClick.AddListener(() =>
		{
			if (F.I.scoringType == ScoringType.Championship)
			{
				carSelector.SetType(GarageType.Earned, ServerC.I.PlayerMe.ScoreGet());
			}
			else
			{
				carSelector.SetType(GarageType.Unlocked);
			}
		});
	}
	public void GetPlayerName(string name)
	{
		splitScreenPlayers[splitScreenPlayerEnteringName].name = name;
		splitScreenPlayerEnteringName++;
	}
	protected override void OnEnable()
	{
		if(splitScreenPlayerEnteringName < splitScreenPlayers.Count)
		{
			thisView.GoToView(enterNameView);
			thisView.prevViewForbidden = true;
		}
		else
		{
			// first time we come here after entering names
			if(F.I.CurRound == 0)
			{
				ResultsView.Clear();
			}
			afterEnabledCo = StartCoroutine(EnableSeq());
		}
	}

	public void OnLobbyExit()
	{
		thisView.GoBack(true);
		F.I.Rounds = 0;
		F.I.CurRound = 1;
		F.I.randomTracks = false;
		F.I.randomCars = false;
		F.I.actionHappening = ActionHappening.InLobby;
		AvailableTracksForRandomSession.Clear();
	}

	IEnumerator EnableSeq()
	{
		thisView.prevViewForbidden = true;

		if (ServerC.I.AmHost)
		{
			F.I.actionHappening = ActionHappening.InLobby;

			if (ServerC.I.ActionHappening != F.I.actionHappening)
			{
				ServerC.I.ActionHappening = F.I.actionHappening;
				ServerC.I.UpdateServerData();
			}
		}
		else
		{
			F.I.actionHappening = ServerC.I.ActionHappening;
			ServerC.I.DecodeConfig(ServerC.I.lobby.Data[ServerC.k_raceConfig].Value);
			F.I.s_trackName = ServerC.I.lobby.Data[ServerC.k_trackName].Value;
		}

		loadCo = true;
		base.OnEnable();
		while (loadCo) //wait for OnEnable to end
			yield return null;

		ResetButtons();

		dataTransferWnd.SetActive(false);
		EnableSelectionOfTracks(ServerC.I.AmHost && !ServerC.I.PlayerMe.ReadyGet());

		leaderboard.Refresh();

		ServerC.I.ReadySet(false);
		ServerC.I.UpdatePlayerData();

		thisView.prevViewForbidden = false;
	}

	public new void ResetButtons()
	{
		base.ResetButtons();
		SwitchScoring(true);
		SwitchRandomCar(true);
		SwitchRandomTrack(true);
		UpdateInteractableButtons();
		SwitchScoring(true);
		SwitchRound(true);
	}

	void UpdateInteractableButtons()
	{
		bool isHost = ServerC.I.AmHost;
		bool notRdy = !ServerC.I.PlayerMe.ReadyGet();
		SwitchRound(true);
		sortButton.gameObject.SetActive(isHost);
		sortButton.buttonComponent.interactable = notRdy;
		garageBtn.interactable = notRdy && !F.I.randomCars;
		scoringText.text = F.I.scoringType.ToString();
		scoringText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		randomCarsText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		randomTracksText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		raceTypeButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		lapsButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy && F.I.s_raceType != RaceType.Knockout;
		nightButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		//CPULevelButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		catchupButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		wayButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		roundText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		sponsorButtonText.transform.parent.gameObject.SetActive(isHost || F.I.teams);
		sponsorButtonText.transform.parent.GetComponent<Button>().interactable = (isHost || F.I.teams) && notRdy;
	}
	public void SwitchSplitScreenPlayers(bool init = false)
	{
		if (!init)
		{
			int dir = F.I.shiftRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
			if (dir > 0)
			{
				if(splitScreenPlayers.Count < F.I.maxCarsInRace)
					splitScreenPlayers.Add(new SplitScreenPlayerInfo());
			}
			else
			{
				if (splitScreenPlayers.Count > 2)
					splitScreenPlayers.RemoveAt(splitScreenPlayers.Count - 1);
			}
		}

		splitScreenPlayersNumberText.text = F.I.LocStr("SPLIT SCREEN PLAYERS") + ": " + splitScreenPlayers.Count;
	}
	public void SwitchRandomCar(bool init = false)
	{
		if (!init)
		{
			F.I.randomCars = !F.I.randomCars;

			if (F.I.randomCars)
			{
				F.I.s_playerCarIdx = UnityEngine.Random.Range(0, F.I.cars.Length);
			}
			else
			{
				F.I.s_playerCarIdx = 0;
			}
		}
		ServerC.I.CarNameSet();
		randomCarsText.text = "Cars:" + (F.I.randomCars ? F.I.LocStr("Random") : F.I.LocStr("Select"));
		garageBtn.interactable = !F.I.randomCars;
	}
	void PickRandomTrack()
	{
		if (AvailableTracksForRandomSession.Count == 0)
			AvailableTracksForRandomSession.AddRange(F.I.tracks.Where(kv => kv.Value.valid && kv.Key.Length > 3).Select(kv => kv.Key));

		int randomTrkNr = UnityEngine.Random.Range(0, AvailableTracksForRandomSession.Count);

		F.I.s_trackName = AvailableTracksForRandomSession[randomTrkNr];
		ServerC.I.SetTrackName();
		Debug.Log(F.I.s_trackName);
		AvailableTracksForRandomSession.RemoveAt(randomTrkNr);
	}
	public void SwitchRandomTrack(bool init = false)
	{
		if (!init)
		{
			F.I.randomTracks = !F.I.randomTracks;
		}

		if (F.I.randomTracks)
		{
			EnableSelectionOfTracks(false);
			trackDescText.text = F.I.LocStr("Random");
		}
		else
		{
			EnableSelectionOfTracks(ServerC.I.AmHost);
		}
		SetTrackShaenigans();

		randomTracksText.text = F.I.LocStr("Tracks") + ":" + F.I.LocStr(F.I.randomTracks ? "Random" : "Select");
	}
	
	public void MoveToSelectedTrack()
	{
		if (FindSelectedTrackEqualToTrackname())
		{
			SetTrackShaenigans();
		}
	}
	public void EnableSelectionOfTracks(bool enabled)
	{
		// remove it first to make sure we don't subscribe to event more than once
		// -= is not throwing
		F.I.move2Ref.action.performed -= CalculateTargetToSelect;
		if (enabled && !F.I.randomTracks)
			F.I.move2Ref.action.performed += CalculateTargetToSelect;
	}
	public void SwitchScoring(bool init)
	{
		int dir = 0;
		if (!init)
		{
			dir = F.I.shiftRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
		}
		F.I.scoringType = (ScoringType)F.Wraparound((int)F.I.scoringType + dir, 0, Enum.GetNames(typeof(ScoringType)).Length - 1);
		scoringText.text = F.I.LocStr(F.I.scoringType.ToString());
	}
	public void SwitchRound(bool init = false)
	{
		if (!init)
		{
			if (F.I.shiftInputRef.action.ReadValue<float>() > 0.5f)
				F.I.Rounds -= 3;
			else
			{
				if (F.I.ctrlInputRef.action.ReadValue<float>() > 0.5f)
					F.I.Rounds -= 1;
				else if (F.I.altInputRef.action.ReadValue<float>() > 0.5f)
					F.I.Rounds += 1;
				else
					F.I.Rounds += 3;
			}
			//if (F.I.Rounds < 3)
			//	F.I.Rounds = 0;
		}

		F.I.Rounds = (byte)F.Wraparound(F.I.Rounds, 0, 99);


		if (ServerC.I.GetRounds() != F.I.Rounds && F.I.Rounds > 0)
			roundText.text = F.I.LocStr("Rounds") + ": " + F.I.Rounds;
		else
		{
			if (F.I.Rounds == 0)
				roundText.text = F.I.LocStr("Rounds") + ": " + F.I.LocStr("No limit");
			else
				roundText.text = $"{F.I.LocStr("Round")} {F.I.CurRound}/{F.I.Rounds}";
		}
	}

	IEnumerator LobbyCountdown()
	{
		yield return new WaitForSecondsRealtime(2.5f);
		if (ServerC.I.AmHost && ServerC.I.readyPlayers == ServerC.I.lobby.Players.Count)
		{
			if (F.I.teams && ServerC.I.TeamsInLobby < 2)
			{
				F.I.chat.AddChatRowLocally("", F.I.LocStr("You need at least two teams"), Color.grey, Color.grey);
			}
			else
			{
				F.I.actionHappening = ActionHappening.InRace;
				ServerC.I.ActionHappening = F.I.actionHappening;
				ServerC.I.UpdateServerData();
				thisView.ToRaceScene();
			}
		}
	}
}
