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
	public TextMeshProUGUI sponsorbText;
	public TextMeshProUGUI randomCarsText;
	public TextMeshProUGUI randomTracksText;
	public TextMeshProUGUI readyText;
	public TextMeshProUGUI scoringText;
	public Button garageBtn;
	public GameObject dataTransferWnd;
	public TextMeshProUGUI dataTransferText;
	public Sprite randomTrackSprite;
	public CarSelector carSelector;
	
	public GameObject zippedTrackDataObjectPrefab;

	public ChatInitializer chatInitializer;

	Sprite originalTrackSprite;
	bool readyClicked = false;
	[NonSerialized]
	public ZippedTrackDataObject zippedTrackDataObject;
	private Coroutine lobbyCntdwnCo;
	private float readyTimeoutTime;
	public bool Busy { get { return dataTransferWnd.activeSelf; } }
	protected override void Awake()
	{
		I = this;

		ServerC.I.callbacks.PlayerDataChanged += Callbacks_PlayerDataChanged;
		ServerC.I.callbacks.LobbyChanged += Callbacks_LobbyChanged;
		ServerC.I.callbacks.PlayerJoined += Callbacks_PlayerJoined;
		
		networkManager.OnTransportFailure += NetworkManager_OnTransportFailure;
		networkManager.OnClientStopped += NetworkManager_OnClientStopped;
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

	private void NetworkManager_OnClientStopped(bool wasHost)
	{
		thisView.GoBack(true);
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
		F.I.s_trackName = ServerC.I.lobby.Data[ServerC.k_trackName].Value;
		if(ServerC.I.AmHost)
		{
			F.I.actionHappening = ActionHappening.InLobby;
			if(ServerC.I.ActionHappening != F.I.actionHappening)
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
		
		base.OnEnable();

		StartCoroutine(ResetButtons());
		SwitchScoring(true);

		dataTransferWnd.SetActive(false);
		EnableSelectionOfTracks(ServerC.I.AmHost && !ServerC.I.PlayerMe.ReadyGet());
		
		if (!ServerC.I.AmHost && !IsCurrentTrackSyncedWithServerTrack(ServerC.I.lobby.Data[ServerC.k_trackSHA].Value))
		{
			RequestTrackSequence();
		}

		leaderboard.Refresh();

		if(ServerC.I.PlayerMe.ReadyGet())
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
		StartCoroutine(Load(F.I.s_trackName));
		while (loadCo)
		{
			await Task.Delay(300);
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

		if(trackChanged)
		{
			F.I.s_trackName = ServerC.I.lobby.Data[ServerC.k_trackName].Value;
			string newSha = ServerC.I.lobby.Data[ServerC.k_trackSHA].Value;
			Debug.Log("new trackname = " + F.I.s_trackName);

			if (!IsCurrentTrackSyncedWithServerTrack(newSha))
			{
				RequestTrackSequence();
				StartCoroutine(Load(F.I.s_trackName));
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

		ServerC.I.UpdatePlayerData();
	}
	public void DecodeConfig(string data)
	{
		if(F.I.scoringType != (ScoringType)(data[0] - '0')) // char to int
		{
			F.I.scoringType = (ScoringType)(data[0] - '0');
			ServerC.I.ScoreSet(0);
		}
		
		var newRandomCars = data[1] == '1';
		if (F.I.randomCars != newRandomCars)
		{
			F.I.randomCars = newRandomCars;
			SwitchRandomCar(true);
		}
		var newRandomTracks = data[2] == '1';
		if (F.I.randomTracks != newRandomTracks)
		{
			F.I.randomTracks = newRandomTracks;
			SwitchRandomTrack(true);
		}
		F.I.s_raceType = (RaceType)(data[3] - '0');
		F.I.s_laps = int.Parse(data[4..6]);
		F.I.s_isNight = data[6] == '1';
		F.I.s_cpuLevel = (CpuLevel)(data[7] - '0');
		F.I.s_cpuRivals = data[8] - '0';
		F.I.s_roadType = (PavementType)(data[9]-'0');
		F.I.s_catchup = data[10] == '1';

		if(gameObject.activeInHierarchy)
			StartCoroutine(ResetButtons());
	}
	private void Callbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDatas)
	{
		leaderboard.Refresh();	
	}

	async void RequestTrackSequence()
	{
		UpdateInteractableButtons();
		dataTransferText.text = "Downloading track " + F.I.s_trackName + " from host..";
		dataTransferWnd.SetActive(true);
		while (zippedTrackDataObject == null)
			await Task.Delay(100);
		zippedTrackDataObject.RequestTrackUpdate();
	}
	
	new IEnumerator ResetButtons()
	{
		while(loadCo)
		{
			yield return null;
		}
		base.ResetButtons();
		SwitchReady(true);
		SwitchScoring(true);
		SwitchRandomCar(true);
		SwitchRandomTrack(true);
		UpdateInteractableButtons();
	}
	void UpdateInteractableButtons()
	{
		bool isHost = ServerC.I.AmHost;
		bool notRdy = !ServerC.I.PlayerMe.ReadyGet();
		sponsorbText.transform.parent.gameObject.SetActive(F.I.scoringType == ScoringType.Championship && notRdy);
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
		catchupButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
	}
	public void SwitchSponsor(bool init)
	{
		var playerMe = ServerC.I.PlayerMe;
		if (!init)
		{
			int dir = F.I.shiftRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
			playerMe.Data[ServerC.k_Sponsor].Value = ((Livery)F.Wraparound((int)playerMe.SponsorGet() + dir, 2, 7)).ToString();
			ServerC.I.ScoreSet(0);
		}
		sponsorbText.text = "Sponsor:" + playerMe.Data[ServerC.k_Sponsor].Value;
	}
	public void SwitchRandomCar(bool init = false)
	{
		if (!init)
		{
			F.I.randomCars = !F.I.randomCars;
		}
		if(F.I.randomCars)
		{ 
			int randomNr = UnityEngine.Random.Range(0, F.I.cars.Length);
			F.I.s_playerCarName = "car" + (randomNr + 1).ToString("D2");
			ServerC.I.CarNameSet();
		}
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
						F.I.s_trackName = t.Key;
						if (F.I.tracks[t.Key].valid)
						{
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
		}
		if (selectedTrack == null)
			return;
		var img = selectedTrack.GetComponent<Image>();
		if (F.I.randomTracks == true)
		{
			originalTrackSprite = img.sprite;
			img.sprite = randomTrackSprite;

			EnableSelectionOfTracks(false);

			PickRandomTrack();
			
			trackDescText.text = "*RANDOM TRACK*";
		}
		else
		{
			if (originalTrackSprite != null)
			{
				img.sprite = originalTrackSprite;
				EnableSelectionOfTracks(ServerC.I.AmHost);
				originalTrackSprite = null;
			}
			trackDescText.text = selectedTrack.name + "\n\n" + F.I.tracks[selectedTrack.name].desc;
		}
		tilesContainer.gameObject.SetActive(!F.I.randomTracks);

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

				if (ServerC.I.AmHost && amReady)
				{// UPDATE HOST INFO
					Debug.Log("ServerConnection.I new track=" + F.I.s_trackName);
					ServerC.I.lobby.Data[ServerC.k_trackSHA] = new DataObject(DataObject.VisibilityOptions.Member, F.I.SHA(F.I.tracksPath + F.I.s_trackName + ".data"));
					ServerC.I.lobby.Data[ServerC.k_trackName] = new DataObject(DataObject.VisibilityOptions.Member, F.I.s_trackName);
					if(F.I.scoringType != ServerC.I.GetScoringType())
					{
						ServerC.I.ScoreSet(0);
					}
					ServerC.I.UpdateServerData();
				}
				ServerC.I.ReadySet(amReady);
				ServerC.I.CarNameSet();
				ServerC.I.UpdatePlayerData();
			}
		}
		catch (LobbyServiceException e)
		{
			Debug.Log("Ready switch failed: " + e.Message);
		}

		UpdateInteractableButtons();

		readyText.text = ((ServerC.I.AmHost ? "HOST " : "") + (amReady ? "NOT READY" : "READY"));

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
			return false;
		if(F.I.SHA(F.I.tracksPath + F.I.s_trackName + ".data") == ServerSideTrackSHA)
		{
			MoveToSelectedTrack();
			return true;
		}
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
		move2Ref.action.performed -= CalculateTargetToSelect;
		if (enabled && !F.I.randomTracks)
			move2Ref.action.performed += CalculateTargetToSelect;
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
		
		sponsorbText.transform.parent.gameObject.SetActive(F.I.scoringType == ScoringType.Championship);
	}
	IEnumerator LobbyCountdown()
	{
		yield return new WaitForSecondsRealtime(2);
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

