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
using static SlideInOut;
[Serializable]
public class ChatInitializer
{
	[Header("Chat initialization references")]
	public ScrollRect lobbyChat;
	public ScrollRect raceChat;

	public TMP_InputField lobbyChatInputField;
	public TMP_InputField raceChatInputField;
}
public class MultiPlayerSelector : TrackSelector
{
	public class IpTime
	{
		public string Ip;
		public float time;

		public IpTime(string ipv4, float time)
		{
			this.Ip = ipv4;
			this.time = time;
		}
	}
	public static MultiPlayerSelector I;
	public NetworkManager networkManager;
	public MainMenuView thisView;
	
	public LeaderBoardTable leaderboard;
	public TextMeshProUGUI ipText;
	public TextMeshProUGUI randomCarsText;
	public TextMeshProUGUI randomTracksText;
	public TextMeshProUGUI readyText;
	public TextMeshProUGUI scoringText;
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
	private Coroutine afterEnabledCo;

	public bool Busy { get { return dataTransferWnd.activeSelf; } }
	protected override void Awake()
	{
		I = this;

		ServerC.I.callbacks.PlayerDataChanged += Callbacks_PlayerDataChanged;
		ServerC.I.callbacks.LobbyChanged += Callbacks_LobbyChanged;
		ServerC.I.callbacks.PlayerJoined += Callbacks_PlayerJoined;
		ServerC.I.OnLobbyExit += OnLobbyExit;
		networkManager.OnTransportFailure += NetworkManager_OnTransportFailure;
		
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
	public void OnLobbyExit()
	{
		thisView.GoBack(true);
		F.I.randomTracks = false;
		F.I.randomCars = false;
		F.I.actionHappening = ActionHappening.InLobby;
	}
	private void Callbacks_PlayerJoined(List<LobbyPlayerJoined> obj)
	{
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
	}

	private void NetworkManager_OnTransportFailure()
	{
		ExitLobby();
	}
	public void ExitLobby()
	{
		ServerC.I.DisconnectFromLobby();
	}
	protected override void OnEnable()
	{
		F.I.chat.UpdateCanvases();

		if (afterEnabledCo != null)
			StopCoroutine(afterEnabledCo);
		afterEnabledCo = StartCoroutine(EnableSeq());
	}
	IEnumerator EnableSeq()
	{
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
			DecodeConfig(ServerC.I.lobby.Data[ServerC.k_raceConfig].Value);
		}

		loadCo = true;
		base.OnEnable();
		while (loadCo) //wait for OnEnable to end
			yield return null;
		F.I.s_trackName = ServerC.I.lobby.Data[ServerC.k_trackName].Value;
		ResetButtons();

		dataTransferWnd.SetActive(false);
		EnableSelectionOfTracks(ServerC.I.AmHost && !ServerC.I.PlayerMe.ReadyGet());

		if (!ServerC.I.AmHost && !IsCurrentTrackSyncedWithServerTrack(ServerC.I.lobby.Data[ServerC.k_trackSHA].Value))
		{
			RequestTrackSequence();
		}

		leaderboard.Refresh();

		if (ServerC.I.PlayerMe.ReadyGet())
		{
			ServerC.I.ReadySet(false);
			ServerC.I.UpdatePlayerData();
		}
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
	private async void Callbacks_LobbyChanged(ILobbyChanges changes)
	{
		bool refreshLeaderboard = false;
		bool trackChanged = false;
		if (changes.PlayerLeft.Changed)
		{ // PLAYER LEFT REQUIRES OLD LOBBY
			foreach (var pIndex in changes.PlayerLeft.Value)
			{
				F.I.chat.PlayerLeft(ServerC.I.lobby.Players[pIndex]);
				refreshLeaderboard = true;
			}
		}
		if (changes.Data.Changed)
		{
			if (changes.Data.Value.ContainsKey(ServerC.k_raceConfig))
			{ // SCORING TYPE CHANGED
				DecodeConfig(changes.Data.Value[ServerC.k_raceConfig].Value.Value);
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

				Load(F.I.s_trackName);
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
	public void DecodeConfig(string data)
	{
		if(F.I.scoringType != (ScoringType)(data[0] - '0')) // char to int
		{
			F.I.scoringType = (ScoringType)(data[0] - '0');
			ServerC.I.ScoreSet(0);
		}
		
		F.I.randomCars = data[1] == '1';
		F.I.randomTracks = data[2] == '1';
		F.I.s_raceType = (RaceType)(data[3] - '0');
		F.I.s_laps = int.Parse(data[4..6]);
		F.I.s_isNight = data[6] == '1';
		F.I.s_cpuLevel = (CpuLevel)(data[7] - '0');
		F.I.s_cpuRivals = data[8] - '0';
		F.I.s_roadType = (PavementType)(data[9]-'0');
		F.I.s_catchup = data[10] == '1';
	}
	private void Callbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDatas)
	{
		leaderboard.Refresh();	
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
	}

	void UpdateInteractableButtons()
	{
		bool isHost = ServerC.I.AmHost;
		bool notRdy = !ServerC.I.PlayerMe.ReadyGet();
		
		sortButton.gameObject.SetActive(isHost && notRdy);
		garageBtn.gameObject.SetActive(notRdy);
		scoringText.text = F.I.scoringType.ToString();
		scoringText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		randomCarsText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		randomTracksText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		raceTypeButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		lapsButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy && F.I.s_raceType != RaceType.Knockout;
		nightButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		CPULevelButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		rivalsButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		wayButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		sponsorButtonText.transform.parent.gameObject.SetActive(F.I.scoringType == ScoringType.Championship && notRdy);
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
		if (F.I.tracks.Any(kv => kv.Key.Length > 3 && kv.Value.valid))
		{
			while (true)
			{
				int random = UnityEngine.Random.Range(0, F.I.tracks.Count);
				int i = 0;
				foreach (var t in F.I.tracks)
				{
					if (i == random)
					{
						if (F.I.tracks[t.Key].valid)
						{
							F.I.s_trackName = t.Key;
							ServerC.I.SetTrackName();
							Debug.Log(F.I.s_trackName);
							return;
						}
						break;
					}
					i++;
				}
			}
		}
	}
	public void SwitchRandomTrack(bool init = false)
	{
		if (!init)
		{
			F.I.randomTracks = !F.I.randomTracks;
			if (F.I.randomTracks && ServerC.I.AmHost)
				PickRandomTrack();
		}

		if (F.I.randomTracks)
		{
			EnableSelectionOfTracks(false);
			trackDescText.text = "*RANDOM TRACK*";
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

			if(!init)
			{
				readyTimeoutTime = Time.time;

				if (!OnlineCommunication.I.IsSpawned)
				{
					Debug.LogWarning("Not synch yet");
					readyClicked = false;
					return;
				}

				if (amReady)
				{
					if(F.I.scoringType != ServerC.I.GetScoringType() 
						|| (F.I.scoringType == ScoringType.Championship && F.I.s_PlayerCarSponsor != ServerC.I.GetSponsor()))
						ServerC.I.ScoreSet(0);

					if(F.I.scoringType != ScoringType.Championship)
					{
						F.I.s_PlayerCarSponsor = (Livery)UnityEngine.Random.Range(1,F.I.Liveries+1);
					}

					if(F.I.s_PlayerCarSponsor != ServerC.I.GetSponsor())
						ServerC.I.SponsorSet();

					if (ServerC.I.AmHost)
					{
						if (F.I.randomTracks)
							PickRandomTrack();
						Debug.Log("ServerConnection.I new track=" + F.I.s_trackName);
						ServerC.I.lobby.Data[ServerC.k_trackSHA] = new DataObject(DataObject.VisibilityOptions.Member, F.I.SHA(F.I.tracksPath + F.I.s_trackName + ".data"));
						ServerC.I.lobby.Data[ServerC.k_trackName] = new DataObject(DataObject.VisibilityOptions.Member, F.I.s_trackName);
						ServerC.I.UpdateServerData();
					}
				}
				ServerC.I.ReadySet(amReady);
				ServerC.I.CarNameSet();
				ServerC.I.UpdatePlayerData();
			}
		}
		catch (LobbyServiceException e)
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
		string trackPath = F.I.tracksPath + F.I.s_trackName + ".data";
		Debug.Log(trackPath);
		if (!File.Exists(trackPath))
		{
			return false;
		}
			
		if(F.I.SHA(F.I.tracksPath + F.I.s_trackName + ".data") == ServerSideTrackSHA)
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
		
		sponsorButtonText.transform.parent.gameObject.SetActive(F.I.scoringType == ScoringType.Championship);
	}
	IEnumerator LobbyCountdown()
	{
		yield return new WaitForSecondsRealtime(2.5f);
		if (ServerC.I.AmHost && ServerC.I.readyPlayers == ServerC.I.lobby.Players.Count
			/*&& ServerC.I.lobby.Players.Count == ServerC.I.activePlayers.Count*/)
		{
			F.I.actionHappening = ActionHappening.InRace;
			ServerC.I.ActionHappening = F.I.actionHappening;
			ServerC.I.UpdateServerData();
			ServerC.I.ReadySet(false);
			ServerC.I.UpdatePlayerData();
			thisView.ToRaceScene();
		}
	}
}

