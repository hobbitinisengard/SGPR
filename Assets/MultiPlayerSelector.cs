using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
	public ServerConnection server;
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
	
	[Header("Chat initialization references")]
	public GameObject chatLogicPrefab;
	public Transform chatContent;
	public TMP_InputField chatInputField;

	Sprite originalTrackSprite;

	bool canStartRace = false;
	float readyTimeoutTime;
	bool readyClicked = false;

	[NonSerialized]
	public ZippedTrackDataObject zippedTrackDataObject;

	public event Action OnHostChanged;

	protected override void Awake()
	{
		garageBtn.onClick.AddListener(() =>
		{
			if (Info.scoringType == ScoringType.Championship)
			{
				carSelector.SetType(GarageType.Earned, server.PlayerMe.ScoreGet());
			}
			else
			{
				carSelector.SetType(GarageType.Unlocked);
			}
		});
	}

	protected override async void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(ResetButtons());
		SwitchScoring(true);

		
		server.PlayerMe.carNameSet(Info.Car(Info.s_playerCarName).name);

		if (server.callbacks == null)
		{
			server.callbacks = new();
			server.callbacks.LobbyChanged += Callbacks_LobbyChanged;
			server.callbacks.PlayerJoined += Callbacks_PlayerJoined;
			server.callbacks.PlayerLeft += Callbacks_PlayerLeft;
			server.callbacks.PlayerDataChanged += Callbacks_PlayerDataChanged;
			await Lobbies.Instance.SubscribeToLobbyEventsAsync(server.lobby.Id, server.callbacks);
		}

		Info.mpSelector = this;

		if (!server.AmHost && !await IsCurrentTrackSyncedWithServerTrack())
		{
			RequestTrackSequence();
		}
	}

	public async void ZippedTrackDataObject_OnNewTrackArrived()
	{
		Debug.Log("ZippedTrackDataObject_OnNewTrackArrived()");
		// refresh tracks menu
		string trackName = server.lobby.Data[Info.k_trackName].Value;
		loadCo = true;
		StartCoroutine(Load(trackName));
		while (loadCo)
		{
			await Task.Delay(300);
			Debug.Log("reloading tracks");
		}
		dataTransferWnd.SetActive(false);
	}
	private async void Callbacks_LobbyChanged(ILobbyChanges changes)
	{
		bool hostChanged = false;
		if (changes.Data.Changed && !server.AmHost)
		{
			if(changes.HostId.Value != server.lobby.HostId)
			{
				hostChanged = true;
			}
			if (changes.Data.Value.ContainsKey(Info.k_raceConfig))
			{
				DecodeConfig(changes.Data.Value[Info.k_raceConfig].Value.Value);
			}
			if (changes.Data.Value.ContainsKey(Info.k_trackName))
			{
				Info.s_trackName = changes.Data.Value[Info.k_trackName].Value.Value;
				Debug.Log(Info.s_trackName);
				if (!await IsCurrentTrackSyncedWithServerTrack())
				{
					RequestTrackSequence();
				}
				StartCoroutine(Load(Info.s_trackName));
			}
			if (server.PlayerMe.ReadyGet())
			{
				server.PlayerMe.ReadySet(false);
				SwitchReady(true);
				await server.UpdatePlayerData();
			}
			if (changes.Data.Value.ContainsKey(ServerConnection.k_actionHappening))
			{
				var ah = (ActionHappening)Enum.Parse(typeof(ActionHappening), changes.Data.Value[ServerConnection.k_actionHappening].Value.Value);
				if (ah != Info.actionHappening)
				{
					if (ah == ActionHappening.InRace)
						thisView.ToRaceScene();
				}
			}
		}
		changes.ApplyToLobby(server.lobby);

		// after updating lobby variable -------------

		maxCPURivals = Info.maxCarsInRace - server.lobby.Players.Count;

		if(hostChanged)
		{
			if (changes.HostId.Value == AuthenticationService.Instance.PlayerId)
			{ // we are now host
				server.createdLobbyIds.Enqueue(server.lobby.Id);
				server.heartbeatTimer.Start();
			}
			OnHostChanged.Invoke();
		}
	}
	public void DecodeConfig(string data)
	{
		if(Info.scoringType != (ScoringType)data[0])
		{
			Info.scoringType = (ScoringType)(data[0] - '0'); // char to int
			server.PlayerMe.ScoreSet(0);
		}
		
		var newRandomCars = data[1] == '1';
		if (Info.randomCars != newRandomCars)
		{
			Info.randomCars = newRandomCars;
			SwitchRandomCar(true);
		}
		var newRandomTracks = data[2] == '1';
		if (Info.randomTracks != newRandomTracks)
		{
			Info.randomTracks = newRandomTracks;
			SwitchRandomTrack(true);
		}
		Info.s_raceType = (RaceType)data[3];
		Info.s_laps = int.Parse(data[4..6]);
		Info.s_isNight = data[6] == '1';
		Info.s_cpuLevel = (CpuLevel)data[7];
		Info.s_cpuRivals = data[8] - '0';
		Info.s_roadType = (PavementType)(data[9]-'0');
		Info.s_catchup = data[10] == '1';
		StartCoroutine(ResetButtons());
	}
	private void Callbacks_PlayerLeft(List<int> players)
	{
		UpdateInteractableButtons();
		leaderboard.Refresh();
	}

	private void Callbacks_PlayerJoined(List<LobbyPlayerJoined> newPlayers)
	{
		leaderboard.Refresh();
	}
	private void Callbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDatas)
	{
		int readyPlayers = 0;
		bool anyChanged = false;
		foreach (var pData in playerDatas)
		{
			var dataChanged = pData.Value;
			foreach (var kv in dataChanged)
			{
				if (kv.Value.Changed)
				{
					switch (kv.Key)
					{
						case Info.k_Ready:
							anyChanged = true;
							if (kv.Value.Value.Value == "true")
								readyPlayers++;
							break;
						default:
							anyChanged = true;
							break;
					}
				}
			}
		}
		if (anyChanged)
			leaderboard.Refresh();

		if (server.lobby.Players.Count >= 2 && readyPlayers == server.lobby.Players.Count 
			&& server.AmHost && server.PlayerMe.ReadyGet())
		{
			canStartRace = true;
			readyText.text = "START RACE";
		}
		else
		{
			canStartRace = false;
			readyText.text = server.AmHost ? "HOST READY" : "READY";
		}
	}
	//async Task SendTrackToPlayer(string ipv4)
	//{


	//var iptime = ipTimes.Find(it => it.Ip == ipv4);
	//if (iptime == null || Time.time - iptime.time > sendTrackRetryTimeSeconds)
	//{
	//ipTimes.Add(new IpTime(ipv4, Time.time));
	// over TCP
	//IPEndPoint ipendPoint = new(IPAddress.Parse(ipv4), ServerConnection.basePort);
	//Debug.Log("send track data to player " + ipendPoint.Address.ToString() + ":" + ipendPoint.Port.ToString());
	//await SendData(ipendPoint, CurrentTrackZipCached);
	//}

	//}

	async void RequestTrackSequence()
	{
		string trackName = server.lobby.Data[Info.k_trackName].Value;

		dataTransferText.text = "Downloading track " + trackName + " from host..";
		dataTransferWnd.SetActive(true);
		while (zippedTrackDataObject == null)
			await Task.Delay(100);
		zippedTrackDataObject.RequestTrackUpdate(trackName);
		try
		{
			//Player host = server.Host;
			//string hostIP = host.IPv4Get();
			//// Setting IP tells the host to send us the track
			//string myIP = await Info.MyIPv4();
			//playerMe.IPv4Set(myIP);
			//await server.UpdatePlayerData();

			//// Show UI
			

			//// Download data from host
			//IPEndPoint ipendPoint = new(IPAddress.Parse(hostIP), ServerConnection.basePort);
			//Debug.Log("ReadData try" + ipendPoint.Address.ToString() + ":" + ServerConnection.basePort.ToString());
			//byte[] data = await GetData(ipendPoint);

			//// Setting IP to empty, tells the host that we have track
			//playerMe.IPv4Set("");
			//await server.UpdatePlayerData();
			
		}
		catch (Exception e)
		{
			Debug.LogError("RequestTrack error:" + e.Message);
		}
	}
	//public async Task SendData(IPEndPoint ipEndPoint, byte[] data)
	//{
	//	await Task.Run(() =>
	//	{
	//		Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
	//		NetworkStream stream = new(client);
	//		int bufferSize = 1024;

	//		byte[] dataLength = BitConverter.GetBytes(data.Length);

	//		stream.Write(dataLength, 0, 4);

	//		int bytesSent = 0;
	//		int bytesLeft = data.Length;

	//		while (bytesLeft > 0)
	//		{
	//			int curDataSize = Math.Min(bufferSize, bytesLeft);

	//			stream.Write(data, bytesSent, curDataSize);

	//			bytesSent += curDataSize;
	//			bytesLeft -= curDataSize;
	//		}
	//		Debug.Log("data sent");
	//	});
	//}
	//public async Task<byte[]> GetData(IPEndPoint ipEndPoint)
	//{
	//	byte[] data = null;
	//	int bytesRead = 0;
	//	await Task.Run(() =>
	//	{
	//		TcpClient client = new(ipEndPoint);
	//		NetworkStream stream = client.GetStream();
	//		byte[] fileSizeBytes = new byte[4];
	//		int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);
	//		int bytesLeft = dataLength;
	//		data = new byte[dataLength];

	//		int bufferSize = 1024;
	//		bytesRead = 0;

	//		while (bytesLeft > 0)
	//		{
	//			int curDataSize = Math.Min(bufferSize, bytesLeft);
	//			if (client.Available < curDataSize)
	//				curDataSize = client.Available;

	//			stream.Read(data, bytesRead, curDataSize);

	//			bytesRead += curDataSize;
	//			bytesLeft -= curDataSize;
	//		}
	//		Debug.Log("data received");
	//	});
	//	return data;
	//}
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
		SwitchRandomTrack(true);
		UpdateInteractableButtons();
	}
	void UpdateInteractableButtons()
	{
		bool isHost = server.AmHost;
		bool notRdy = !server.PlayerMe.ReadyGet();
		sponsorbText.transform.parent.gameObject.SetActive(Info.scoringType == ScoringType.Championship && notRdy);
		sortButton.gameObject.SetActive(isHost && notRdy);
		garageBtn.gameObject.SetActive(notRdy);
		scoringText.text = Info.scoringType.ToString();
		scoringText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		randomCarsText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		randomTracksText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		raceTypeButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		lapsButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		nightButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		CPULevelButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		rivalsButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		wayButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
		catchupButtonText.transform.parent.GetComponent<Button>().interactable = isHost && notRdy;
	}
	public void SwitchSponsor(bool init)
	{
		int dir = 0;
		if (!init)
		{
			dir = shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
		}
		var playerMe = server.PlayerMe;
		playerMe.Data[Info.k_Sponsor].Value = ((Livery)Wraparound((int)playerMe.SponsorGet() + dir, 2, 7)).ToString();
		sponsorbText.text = "Sponsor:" + playerMe.Data[Info.k_Sponsor].Value;
	}
	public void SwitchRandomCar(bool init = false)
	{
		if (!init)
		{
			Info.randomCars = !Info.randomCars;
		}
		if(Info.randomCars)
		{ 
			int randomNr = UnityEngine.Random.Range(0, Info.cars.Length);
			var carName = Info.cars[randomNr].name;
			Info.s_playerCarName = "car" + (randomNr + 1).ToString("D2");
			server.PlayerMe.carNameSet(carName);
		}
		randomCarsText.text = "Cars:" + (Info.randomCars ? "Random" : "Select");
		garageBtn.interactable = !Info.randomCars;
	}
	public void SwitchRandomTrack(bool init = false)
	{
		if (!init)
		{
			Info.randomTracks = !Info.randomTracks;
		}

		var img = selectedTrack.GetComponent<Image>();
		if (Info.randomTracks == true)
		{
			originalTrackSprite = img.sprite;
			img.sprite = randomTrackSprite;

			EnableSelectionOfTracks(false);
			
			// select random track
			int random = UnityEngine.Random.Range(0, Info.tracks.Count);
			int i = 0;
			foreach (var t in Info.tracks)
			{
				if (i == random)
				{
					Info.s_trackName = t.Key;
					Debug.Log(Info.s_trackName);
					break;
				}
				i++;
			}
			trackDescText.text = "*RANDOM TRACK*";
		}
		else
		{
			if (originalTrackSprite != null)
			{
				img.sprite = originalTrackSprite;
				EnableSelectionOfTracks(true);
				originalTrackSprite = null;
			}
			trackDescText.text = selectedTrack.name + "\n\n" + Info.tracks[selectedTrack.name].desc;
		}
		tilesContainer.gameObject.SetActive(!Info.randomTracks);

		randomTracksText.text = "Tracks:" + (Info.randomTracks ? "Random" : "Select");
	}
	public async void SwitchReady(bool init = false)
	{
		if (canStartRace && server.AmHost)
		{
			Info.actionHappening = ActionHappening.InRace;
			await server.UpdateServerData();
			
			thisView.ToRaceScene();
		}
		else
		{
			ReadyButton(init);
		}
	}
	async Task<bool> IsCurrentTrackSyncedWithServerTrack()
	{
		string ServerSideTrackSHA = server.lobby.Data[Info.k_trackSHA].Value;
		string trackName = server.lobby.Data[Info.k_trackName].Value;
		string trackPath = Info.tracksPath + trackName + ".data";
		if (!File.Exists(trackPath))
			return false;

		return await Info.SHA(Info.tracksPath + trackName + ".data") == ServerSideTrackSHA;
	}
	public void EnableSelectionOfTracks(bool enabled)
	{
		// selecting track possible only if we're hosting and not ready 
		// remove it first to make sure we don't subscribe to event more than once
		// -= is not throwing
		move2Ref.action.performed -= CalculateTargetToSelect;
		if (enabled)
			move2Ref.action.performed += CalculateTargetToSelect;
	}
	public async void ReadyButton(bool init = false)
	{
		if (readyClicked)
			return;

		readyClicked = true;

		var playerMe = server.PlayerMe;

		bool amReady = !init && !playerMe.ReadyGet();
		
		if (Time.time - readyTimeoutTime < 1)
			await Task.Delay(Mathf.RoundToInt((1 - (Time.time - readyTimeoutTime))*1000));

		readyTimeoutTime = Time.time;
		try
		{
			EnableSelectionOfTracks(!amReady && server.AmHost);
			// UPDATE PLAYER
			playerMe.ReadySet(amReady);
			await server.UpdatePlayerData();

			if (server.AmHost && amReady)
			{// UPDATE HOST INFO
				server.lobby.Data[Info.k_trackSHA] = new DataObject(DataObject.VisibilityOptions.Member, await Info.SHA(Info.tracksPath + Info.s_trackName + ".data"));
				server.lobby.Data[Info.k_trackName] = new DataObject(DataObject.VisibilityOptions.Member, Info.s_trackName);
				await server.UpdateServerData();
			}
		}
		catch (LobbyServiceException e)
		{
			Debug.Log("Ready switch failed: " + e.Message);
		}
		UpdateInteractableButtons();
		readyText.text = server.AmHost ? "HOST READY" : "READY";
		leaderboard.Refresh();
		readyClicked = false;
	}
	public void SwitchScoring(bool init)
	{
		int dir = 0;
		if (!init)
			dir = shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		Info.scoringType = (ScoringType)Wraparound((int)Info.scoringType + dir, 0, Enum.GetNames(typeof(ScoringType)).Length - 1);
		scoringText.text = Info.scoringType.ToString();

		sponsorbText.transform.parent.gameObject.SetActive(Info.scoringType == ScoringType.Championship);
	}
}

