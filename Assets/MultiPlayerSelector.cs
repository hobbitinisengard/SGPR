using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
public class MultiPlayerSelector : TrackSelector
{
	public static MultiPlayerSelector I;
	public NetworkManager networkManager;
	public MainMenuView thisView;
	
	public LeaderBoardTable leaderboard;
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
	
	public GameObject zippedTrackDataObjectPrefab;

	bool readyClicked = false;
	[NonSerialized]
	public ZippedTrackDataObject zippedTrackDataObject;
	private Coroutine lobbyCntdwnCo;
	private float readyTimeoutTime;
	private float connectionTimeoutTime;
	private Coroutine afterEnabledCo;
	public bool Busy { get { return dataTransferWnd.activeSelf; } }

	bool OnLobbyExitAndWaitingForThisToBecomeActive;

	List<string> AvailableTracksForRandomSession = new();
	float clientConnectedTime;

	protected override void Awake()
	{
		ServerC.I.OnLobbyExit += OnLobbyExit;
		networkManager.OnTransportFailure += NetworkManager_OnTransportFailure;
		networkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
		networkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
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

	private void NetworkManager_OnClientConnectedCallback(ulong id)
	{
		if(F.I.minimized)
		{
			PlaySFX("fe-cardssuccess");
		}
		clientConnectedTime = Time.time;
	}
	// this event is called only on disconnecting client and the server
	private void NetworkManager_OnClientDisconnectCallback(ulong id)
	{
		if(id != networkManager.LocalClientId)
		{
			Online.I.ClientDisconnected(id);
		}
	}

	public void OnLobbyExit()
	{
		if(!gameObject.activeInHierarchy)
		{
			OnLobbyExitAndWaitingForThisToBecomeActive = true;
			return;
		}
		thisView.GoBack(true);
		F.I.Rounds = 0;
		F.I.CurRound = 1;
		F.I.randomTracks = false;
		F.I.randomCars = false;
		F.I.actionHappening = ActionHappening.InLobby;
		AvailableTracksForRandomSession.Clear();
	}
	public void Callbacks_PlayerJoined(List<LobbyPlayerJoined> newPlayers)
	{
		clientConnectedTime = Time.time;
		maxCPURivals = F.I.maxCarsInRace - ServerC.I.lobby.Players.Count;

		if (F.I.s_raceType == RaceType.Knockout)
		{
			if (ServerC.I.AmHost && ServerC.I.PlayerMe.ReadyGet())
			{
				SwitchReady();
				ServerC.I.UpdatePlayerData();
			}
			SwitchRivals();
		}
		foreach (var p in newPlayers)
		{
			F.I.chat.AddChatRowLocally(p.Player.NameGet(), "has joined the server", Color.white, Color.grey);
		}
	}

	private void NetworkManager_OnTransportFailure()
	{
		ExitLobby();
	}
	public void ExitLobby()
	{
		if(!thisView.prevViewForbidden)
			ServerC.I.DisconnectFromLobby();
	}
	void OnApplicationQuit()
	{
		ServerC.I.DisconnectFromLobby();
	}
	protected override void OnEnable()
	{
		connectionTimeoutTime = Time.time;
		ResultsView.Clear();
		F.I.chat.UpdateCanvases();

		if (afterEnabledCo != null)
			StopCoroutine(afterEnabledCo);

		if (OnLobbyExitAndWaitingForThisToBecomeActive)
		{
			OnLobbyExitAndWaitingForThisToBecomeActive = false;
			OnLobbyExit();
			return;
		}

		afterEnabledCo = StartCoroutine(EnableSeq());
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

		if (!ServerC.I.AmHost && !IsCurrentTrackSyncedWithServerTrack(ServerC.I.lobby.Data[ServerC.k_trackSHA].Value))
		{
			RequestTrackSequence();
		}

		leaderboard.Refresh();

		ServerC.I.ReadySet(false);
		ServerC.I.UpdatePlayerData();


		while(!Online.I.IsSpawned && Time.time - connectionTimeoutTime < 5)
		{
			yield return null;
		}
		thisView.prevViewForbidden = false;
	}
	public async void ZippedTrackDataObject_OnNewTrackArrived()
	{
		Debug.Log("ZippedTrackDataObject_OnNewTrackArrived()");
		// refresh tracks menu
		F.I.s_trackName = ServerC.I.lobby.Data[ServerC.k_trackName].Value;
		loadCo = true;

		// wait till mpSelector is active to start coroutine on it
		while(!gameObject.activeInHierarchy)
			await Task.Delay(100);
		StartCoroutine(Load(F.I.s_trackName));

		while (loadCo)
		{
			await Task.Delay(100);
			Debug.Log("reloading tracks");
		}
		dataTransferWnd.SetActive(false);
	}
	public async void Callbacks_LobbyChanged(ILobbyChanges changes)
	{
		bool refreshLeaderboard = false;
		bool trackChanged = false;
		if (changes.PlayerLeft.Changed)
		{ // PLAYER LEFT REQUIRES OLD LOBBY
			foreach (var pIndex in changes.PlayerLeft.Value)
			{
				var p = ServerC.I.lobby.Players[pIndex];
				F.I.chat.PlayerLeft(p);
				refreshLeaderboard = true;
				if(F.I.teams && p.SponsorGet() == F.I.s_PlayerCarSponsor)
				{ // cash earned by the leaving player is distributed evenly to his teammates
					int teammates = ServerC.I.lobby.Players.Count(pl => pl.SponsorGet() == F.I.s_PlayerCarSponsor) - 1;
					ServerC.I.ScoreSet(ServerC.I.PlayerMe.ScoreGet() + Mathf.RoundToInt(p.ScoreGet() / teammates));
				}
			}
		}
		if (changes.Data.Changed)
		{
			if (changes.Data.Value.ContainsKey(ServerC.k_raceConfig))
			{ // SCORING TYPE CHANGED
				ServerC.I.DecodeConfig(changes.Data.Value[ServerC.k_raceConfig].Value.Value);
				ResetButtons();
				refreshLeaderboard = true;
			}
			if (!ServerC.I.AmHost)
			{
				if (changes.Data.Value.ContainsKey(ServerC.k_relayCode))
				{ // HOST CHANGED
					Debug.Log("reconnect to new host");
					string newRelayJoinCode = ServerC.I.lobby.Data[ServerC.k_relayCode].Value;
					try
					{
						await ServerC.I.JoinRelayByCode(newRelayJoinCode);
					}
					catch (Exception e)
					{
						Debug.Log(e.Message);
					}
				}

				if (changes.Data.Value.ContainsKey(ServerC.k_trackName))
				{
					trackChanged = true;
				}

				if (changes.Data.Value.ContainsKey(ServerC.k_actionHappening))
				{
					var ah = (ActionHappening)Enum.Parse(typeof(ActionHappening), changes.Data.Value[ServerC.k_actionHappening].Value.Value);
					if (ah == ActionHappening.InRace)
					{
						F.I.actionHappening = ActionHappening.InRace;
						thisView.ToRaceScene();
					}
				}
				if (ServerC.I.PlayerMe.ReadyGet())
				{ // CHANGE IN SERVER DATA TURNS OFF READY
					ServerC.I.ReadySet(false);
					SwitchReady(init: true);
				}
			}
		}
		try
		{
			changes.ApplyToLobby(ServerC.I.lobby); // from now on lobby updated 
		}
		catch
		{
			Debug.LogWarning("ApplyToLobby failed. fetching whole lobby");
			await ServerC.I.GetLobbyManually();
			OnEnable();
			return;
		}

		ServerC.I.UpdatePlayerData();

		if (trackChanged)
		{
			F.I.s_trackName = ServerC.I.lobby.Data[ServerC.k_trackName].Value;
			string newSha = ServerC.I.lobby.Data[ServerC.k_trackSHA].Value;
			Debug.Log("new trackname = " + F.I.s_trackName);

			if (!IsCurrentTrackSyncedWithServerTrack(newSha))
			{
				RequestTrackSequence();
			}
		}
		maxCPURivals = F.I.maxCarsInRace - ServerC.I.lobby.Players.Count;

		UpdateInteractableButtons();

		if (F.I.actionHappening == ActionHappening.InLobby && ServerC.I.AmHost 
			&& ServerC.I.readyPlayers == ServerC.I.lobby.Players.Count)
		{
			if (lobbyCntdwnCo != null)
				StopCoroutine(lobbyCntdwnCo);

			lobbyCntdwnCo = StartCoroutine(LobbyCountdown());
		}

		if (changes.HostId.Changed)
		{
			if (changes.HostId.Value == AuthenticationService.Instance.PlayerId)
			{ 
				Debug.Log("We are new host");
				ServerC.I.maxPlayers = ServerC.I.lobby.MaxPlayers;
				string newRelayJoinCode = await ServerC.I.StartRelay();
				ServerC.I.lobby.Data[ServerC.k_relayCode] = new DataObject(DataObject.VisibilityOptions.Public, newRelayJoinCode);
				ServerC.I.createdLobbyIds.Enqueue(ServerC.I.lobby.Id);
				ServerC.I.heartbeatTimer.Start();
				ServerC.I.UpdateServerData();
				refreshLeaderboard = true;
			}
		}
		if (refreshLeaderboard)
			leaderboard.Refresh();

	}
	
	public void Callbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDatas)
	{
		leaderboard.Refresh();
		
		if(F.I.minimized && !dataTransferWnd.activeSelf)
		{
			var hostRdy = ServerC.I.Host.ReadyGet();
			if (hostRdy && !ServerC.I.PlayerMe.ReadyGet())
			{
				SwitchReady();
			}
		}
	}

	async void RequestTrackSequence()
	{
		UpdateInteractableButtons();
		string trackName = F.I.randomTracks ? "" : F.I.s_trackName;
		dataTransferText.text = $"Downloading track {trackName} from host..";
		dataTransferWnd.SetActive(true);
		while (zippedTrackDataObject == null)
			await Task.Delay(100);
		zippedTrackDataObject.RequestTrackUpdate();
	}
	
	public new void ResetButtons()
	{
		base.ResetButtons();
		SwitchScoring(true);
		SwitchRandomCar(true);
		SwitchRandomTrack(true);
		UpdateInteractableButtons();
		SwitchReady(true);
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
	
	public void SwitchRandomCar(bool init = false)
	{
		if (!init)
		{
			F.I.randomCars = !F.I.randomCars;

			if (F.I.randomCars)
			{
				int randomNr = UnityEngine.Random.Range(0, F.I.cars.Length);
				F.I.s_playerCarName = "car" + (randomNr + 1).ToString("D2");
			}
			else
			{
				F.I.s_playerCarName = "car01";
			}
		}
		

		ServerC.I.CarNameSet();
		randomCarsText.text = "Cars:" + (F.I.randomCars ? "Random" : "Select");
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
			trackDescText.text = "*random*";
		}
		else
		{
			EnableSelectionOfTracks(ServerC.I.AmHost);
		}
		SetTrackShaenigans();

		randomTracksText.text = "Tracks:" + (F.I.randomTracks ? "Random" : "Select");
	}
	public async void SwitchReady(bool init = false)
	{
		if (readyClicked)
			return;

		readyClicked = true;

		if (!init && Time.time - readyTimeoutTime < 1)
			await Task.Delay(Mathf.RoundToInt((1 - (Time.time - readyTimeoutTime)) * 1000));

		var playerMe = ServerC.I.PlayerMe;
		bool amReady = !init && !playerMe.ReadyGet();

		try
		{
			EnableSelectionOfTracks(!amReady && ServerC.I.AmHost);

			// You can set ready in lobby only if nobody is racing
			if (!init)
			{
				readyTimeoutTime = Time.time;

				if (!Online.I.IsSpawned || ServerC.I.AnyClientsStillInRace || (amReady && Time.time - clientConnectedTime < 3))
				{
					if (Time.time - clientConnectedTime < 3)
						F.I.chat.AddChatRowLocally("", "A new player is synching right now...", Color.grey, Color.grey);
					if (!Online.I.IsSpawned)
						F.I.chat.AddChatRowLocally("", "No synchronization. Try again or reconnect", Color.grey, Color.grey);
					if (ServerC.I.AnyClientsStillInRace)
						F.I.chat.AddChatRowLocally("", "Some players haven't come back to lobby yet", Color.grey, Color.grey);

					PlaySFX("fe-cardserror");
					readyClicked = false;
					return;
				}

				if (amReady)
				{
					if (F.I.randomCars)
					{
						int randomNr = UnityEngine.Random.Range(0, F.I.cars.Length);
						F.I.s_playerCarName = "car" + (randomNr + 1).ToString("D2");
					}

					if(F.I.teams && F.I.s_PlayerCarSponsor != ServerC.I.GetSponsor())
					{
						ServerC.I.ScoreSet(0);
						F.I.CurRound = 0;
					}

					ServerC.I.SponsorSet();

					if (ServerC.I.AmHost)
					{
						if (F.I.CurRound == 0 || F.I.Rounds != ServerC.I.GetRounds() || (F.I.Rounds > 0 && F.I.CurRound > F.I.Rounds)
							|| F.I.scoringType != ServerC.I.GetScoringType())
						{
							ServerC.I.ScoreSet(0);
							AvailableTracksForRandomSession.Clear();
							F.I.CurRound = 1;
						}

						if (F.I.randomTracks)
							PickRandomTrack();

						ServerC.I.lobby.Data[ServerC.k_trackSHA] = 
							new DataObject(DataObject.VisibilityOptions.Member, F.I.SHA(F.I.tracksPath + F.I.s_trackName + ".data"));
						ServerC.I.lobby.Data[ServerC.k_trackName] = new DataObject(DataObject.VisibilityOptions.Member, F.I.s_trackName);
						ServerC.I.UpdateServerData();
					}
				}
				
				ServerC.I.ReadySet(amReady);
				ServerC.I.CarNameSet();
				ServerC.I.UpdatePlayerData();
			}
		}
		catch (Exception e)
		{
			Debug.LogError("Ready switch failed: " + e.Message);
		}

		UpdateInteractableButtons();

		readyText.text = (ServerC.I.AmHost ? "HOST " : "") + "SWITCH READY";

		if (lobbyCntdwnCo != null)
			StopCoroutine(lobbyCntdwnCo);

		if ((ServerC.I.AmHost && ServerC.I.readyPlayers == ServerC.I.lobby.Players.Count))
		{
			lobbyCntdwnCo = StartCoroutine(LobbyCountdown());
		}

		leaderboard.Refresh();

		readyClicked = false;
	}
	bool IsCurrentTrackSyncedWithServerTrack(string ServerSideTrackSHA)
	{
		string trackPath = F.I.tracksPath + F.I.s_trackName;
		Debug.Log(trackPath);
		if (!File.Exists(trackPath + ".data"))
		{
			return false;
		}
			
		if(F.I.SHA(trackPath + ".data") == ServerSideTrackSHA)
		{
			MoveToSelectedTrack();
			return true;
		}
		Debug.LogWarning("SHA differs");
		return false;
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
		scoringText.text = F.I.scoringType.ToString();
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
			roundText.text = "Rounds: " + F.I.Rounds;
		else
		{
			if (F.I.Rounds == 0)
				roundText.text = $"Rounds:No limit";
			else
				roundText.text = $"Round {F.I.CurRound}/{F.I.Rounds}";
		}
	}

	IEnumerator LobbyCountdown()
	{
		yield return new WaitForSecondsRealtime(2.5f);
		if (ServerC.I.AmHost && ServerC.I.readyPlayers == ServerC.I.lobby.Players.Count)
		{
			if (F.I.teams && ServerC.I.TeamsInLobby < 2)
			{
				F.I.chat.AddChatRowLocally("", "You need at least two teams", Color.grey, Color.grey);
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

