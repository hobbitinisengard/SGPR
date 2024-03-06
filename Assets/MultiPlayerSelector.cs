using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TMPro;
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
	const float sendTrackRetryTimeSeconds = 10;
	public ServerConnection server;
	public MainMenuView thisView;
	public Chat chat;
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
	Sprite originalTrackSprite;
	readonly List<IpTime> ipTimes = new();

	bool canStartRace = false;
	byte[] _currentTrackZipCached;
	float readyTimeoutTime;

	byte[] CurrentTrackZipCached
	{
		get
		{
			if (_currentTrackZipCached == null)
			{
				string zipPath = Info.documentsSGPRpath + persistentSelectedTrack + ".zip";
				using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
				{
					zip.CreateEntryFromFile(Info.tracksPath + persistentSelectedTrack + ".track", persistentSelectedTrack + ".track");
					zip.CreateEntryFromFile(Info.tracksPath + persistentSelectedTrack + ".png", persistentSelectedTrack + ".png");
					zip.CreateEntryFromFile(Info.tracksPath + persistentSelectedTrack + ".data", persistentSelectedTrack + ".data");
				}
				_currentTrackZipCached = File.ReadAllBytes(zipPath);
			}
			return _currentTrackZipCached;
		}
	}
	protected override async void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(ResetButtons());
		SwitchScoring(true);

		server.PlayerMe.carNameSet(Info.Car(Info.s_playerCarName).name);
		await server.UpdatePlayerData();

		if (server.callbacks == null)
		{
			LobbyEventCallbacks callbacks = new();
			callbacks.PlayerDataChanged += Callbacks_PlayerDataChanged;
			callbacks.PlayerJoined += Callbacks_PlayerJoined;
			callbacks.PlayerLeft += Callbacks_PlayerLeft;
			callbacks.LobbyChanged += Callbacks_LobbyChanged;
			server.SetCallbacks(callbacks);
		}

		_currentTrackZipCached = null;
		if (!server.AmHost && !await IsCurrentTrackSyncedWithServerTrack())
		{
			await RequestTrackSequence();
		}
	}
	void Start()
	{
		chat.inputField.onSelect.AddListener(s => { move2Ref.action.performed -= CalculateTargetToSelect; });
		chat.inputField.onDeselect.AddListener(s => { move2Ref.action.performed += CalculateTargetToSelect; });
	}
	private async void Callbacks_LobbyChanged(ILobbyChanges changes)
	{
		if (changes.Data.Changed && !server.AmHost)
		{
			if (changes.Data.Value[Info.k_raceConfig].Changed)
			{
				DecodeConfig(changes.Data.Value[Info.k_raceConfig].Value.Value);
			}
			if (changes.Data.Value[Info.k_trackName].Changed)
			{
				if (!await IsCurrentTrackSyncedWithServerTrack())
				{
					await RequestTrackSequence();
				}
			}
			if (changes.Data.Value[Info.k_actionHappening].Changed)
			{
				var ah = (ActionHappening)Enum.Parse(typeof(ActionHappening), changes.Data.Value[Info.k_actionHappening].Value.Value);
				if (ah != Info.actionHappening)
				{
					if (ah == ActionHappening.InWorld)
						thisView.ToRaceScene();
				}
			}
		}
		changes.ApplyToLobby(server.lobby);
		maxRivals = 9 - server.lobby.Players.Count;
	}
	public void DecodeConfig(string data)
	{
		Info.scoringType = (ScoringType)data[0];
		var newRandomCars = data[1] == '1';
		var newRandomTracks = data[2] == '1';
		Info.s_raceType = (RaceType)data[3];
		Info.s_laps = int.Parse(data[4..6]);
		Info.s_isNight = data[6] == '1';
		Info.s_cpuLevel = (CpuLevel)data[7];
		Info.s_rivals = data[8] - '0';
		Info.s_roadType = (PavementType)data[9];
		Info.s_catchup = data[10] == '1';

		if (Info.randomCars != newRandomCars)
		{
			Info.randomCars = newRandomCars;
			SwitchRandomCar(true);
		}
		if (Info.randomTracks != newRandomTracks)
		{
			Info.randomTracks = newRandomTracks;
			SwitchRandomTrack(true);
		}
		StartCoroutine(ResetButtons());
	}
	private void Callbacks_PlayerLeft(List<int> players)
	{
		foreach (var p in players)
		{
			chat.AddChatRow(server.lobby.Players[p].NameGet(), "has left the server", Color.white, Color.gray);
		}
	}

	private void Callbacks_PlayerJoined(List<LobbyPlayerJoined> newPlayers)
	{
		foreach (var p in newPlayers)
		{
			chat.AddChatRow(p.Player.NameGet(), "has joined the server", Color.white, Color.gray);
		}
	}
	private async void Callbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDatas)
	{
		int readyPlayers = 0;
		bool anyChanged = false;
		for (int i = 0; i < playerDatas.Count; ++i)
		{
			foreach (var kv in playerDatas[i])
			{
				if (kv.Value.Changed)
				{
					switch (kv.Key)
					{
						case Info.k_message:
							chat.AddChatRow(server.lobby.Players[i], kv.Value.Value.Value);
							break;
						case Info.k_IPv4:
							if (Info.k_IPv4.Length > 1)
							{
								await SendTrackToPlayer(kv.Value.Value.Value);
							}
							break;
						case Info.k_Ready:
							anyChanged = true;
							if (kv.Value.Value.Value == "true" && playerDatas[i][Info.k_IPv4].Value.Value == "")
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

		if (server.lobby.Players.Count >= 2 && readyPlayers == server.lobby.Players.Count && server.AmHost)
		{
			canStartRace = true;
			readyText.text = "START";
		}
		else
		{
			canStartRace = false;
			readyText.text = server.AmHost ? "HOST READY" : "READY";
		}
	}
	async Task SendTrackToPlayer(string ipv4)
	{
		var iptime = ipTimes.Find(it => it.Ip == ipv4);
		if (iptime == null || Time.time - iptime.time > sendTrackRetryTimeSeconds)
		{
			ipTimes.Add(new IpTime(ipv4, Time.time));
			IPEndPoint ipendPoint = new(IPAddress.Parse(ipv4), ServerConnection.basePort);
			Debug.Log("send track data to player " + ipendPoint.Address.ToString() + ":" + ipendPoint.Port.ToString());
			await SendData(ipendPoint, CurrentTrackZipCached);
		}
	}
	async Task RequestTrackSequence()
	{
		Debug.Log("RequestTrackSequence");
		var playerMe = server.PlayerMe;
		try
		{
			// Setting IP tells the host to send us the track
			playerMe.IPv4Set(server.GetIPv4());
			await server.UpdatePlayerData();

			// Show UI
			string trackName = server.lobby.Data[Info.k_trackName].Value;
			dataTransferText.text = "Downloading track " + trackName + " from host..";
			dataTransferWnd.SetActive(true);

			// Download data from host
			Player host = server.Host;
			IPEndPoint ipendPoint = new(IPAddress.Parse(host.IPv4Get()), ServerConnection.basePort);
			Debug.Log("ReadData try" + ipendPoint.Address.ToString() + ":" + ServerConnection.basePort.ToString());
			byte[] data = await GetData(ipendPoint);

			// Setting IP to empty, tells the host that we have track
			playerMe.IPv4Set("");
			await server.UpdatePlayerData();

			dataTransferText.text = "Unpacking track..";
			string zipPath = Info.downloadPath + trackName + ".zip";
			File.WriteAllBytes(zipPath, data);
			ZipFile.ExtractToDirectory(zipPath, Info.downloadPath);
			bool localTrackExists = File.Exists(Info.tracksPath + trackName + ".data");
			if (localTrackExists)
			{
				if (!await MatchingSHA(Info.downloadPath + trackName + ".data", server.lobby.Data[Info.k_trackSHA].Value))
				{
					// rename local track to trackName+number
					string newName = trackName + "0";
					for (int i = 1; i < 1000; ++i)
					{
						newName = trackName + i.ToString();
						if (!File.Exists(Info.tracksPath + newName + ".png"))
						{
							break;
						}
					}
					File.Move(Info.tracksPath + trackName + ".png", Info.tracksPath + newName + ".png");
					File.Move(Info.tracksPath + trackName + ".data", Info.tracksPath + newName + ".data");
					File.Move(Info.tracksPath + trackName + ".track", Info.tracksPath + newName + ".track");
					var header = new TrackHeader(Info.tracks[trackName]);
					Info.tracks.Add(newName, header);

					File.Move(Info.downloadPath + trackName + ".png", Info.tracksPath + trackName + ".png");
					File.Move(Info.downloadPath + trackName + ".data", Info.tracksPath + trackName + ".data");
					File.Move(Info.downloadPath + trackName + ".track", Info.tracksPath + trackName + ".track");

					string trackJson = File.ReadAllText(Info.tracksPath + trackName + ".track");
					header = JsonConvert.DeserializeObject<TrackHeader>(trackJson);
					Info.tracks[trackName] = header;
				}
			}
			else
			{
				File.Move(Info.downloadPath + trackName + ".png", Info.tracksPath + trackName + ".png");
				File.Move(Info.downloadPath + trackName + ".data", Info.tracksPath + trackName + ".data");
				File.Move(Info.downloadPath + trackName + ".track", Info.tracksPath + trackName + ".track");
			}
			//File.Delete(zipPath);


			// refresh tracks menu
			loadCo = true;
			StartCoroutine(Load(trackName));
			while (loadCo)
			{
				await Task.Delay(300);
				Debug.Log("reloading tracks");
			}
		}
		catch (Exception e)
		{
			dataTransferWnd.SetActive(false);
			Debug.LogError("RequestTrack error:" + e.Message);

			if (playerMe.IPv4Get() != "")
			{
				playerMe.IPv4Set("");
				await server.UpdatePlayerData();
			}
		}
		dataTransferWnd.SetActive(false);
	}
	public async Task SendData(IPEndPoint ipEndPoint, byte[] data)
	{
		await Task.Run(() =>
		{
			Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			NetworkStream stream = new(client);
			int bufferSize = 1024;

			byte[] dataLength = BitConverter.GetBytes(data.Length);

			stream.Write(dataLength, 0, 4);

			int bytesSent = 0;
			int bytesLeft = data.Length;

			while (bytesLeft > 0)
			{
				int curDataSize = Math.Min(bufferSize, bytesLeft);

				stream.Write(data, bytesSent, curDataSize);

				bytesSent += curDataSize;
				bytesLeft -= curDataSize;
			}
			Debug.Log("data sent");
		});
	}
	public async Task<byte[]> GetData(IPEndPoint ipEndPoint)
	{
		byte[] data = null;
		int bytesRead = 0;
		await Task.Run(() =>
		{
			TcpClient client = new(ipEndPoint);
			NetworkStream stream = client.GetStream();
			byte[] fileSizeBytes = new byte[4];
			int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);
			int bytesLeft = dataLength;
			data = new byte[dataLength];

			int bufferSize = 1024;
			bytesRead = 0;

			while (bytesLeft > 0)
			{
				int curDataSize = Math.Min(bufferSize, bytesLeft);
				if (client.Available < curDataSize)
					curDataSize = client.Available;

				stream.Read(data, bytesRead, curDataSize);

				bytesRead += curDataSize;
				bytesLeft -= curDataSize;
			}
			Debug.Log("data received");
		});
		return data;
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
		SwitchRandomTrack(true);
		bool isHost = server.AmHost;
		sortButton.gameObject.SetActive(isHost);
		scoringText.transform.parent.GetComponent<Button>().interactable = isHost;
		sponsorbText.transform.parent.gameObject.SetActive(Info.scoringType == ScoringType.Championship);
		randomCarsText.transform.parent.GetComponent<Button>().interactable = isHost;
		randomTracksText.transform.parent.GetComponent<Button>().interactable = isHost;
		raceTypeButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		lapsButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		nightButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		CPULevelButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		rivalsButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		wayButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		catchupButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
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
			Info.randomCars = !Info.randomCars;

		int randomNr = UnityEngine.Random.Range(0, Info.cars.Length);
		Car randomCar = Info.cars[randomNr];
		Info.s_playerCarName = "car" + (randomNr + 1).ToString("D2");
		server.PlayerMe.carNameSet(randomCar.name);

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
		if (Info.randomTracks)
		{
			originalTrackSprite = img.sprite;
			img.sprite = randomTrackSprite;
			move2Ref.action.performed -= CalculateTargetToSelect;

			
			// select random track
			int random = UnityEngine.Random.Range(0, Info.tracks.Count);
			int i = 0;
			foreach (var t in Info.tracks)
			{
				if (i == random)
				{
					Info.s_trackName = t.Key;
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
				move2Ref.action.performed += CalculateTargetToSelect;
				originalTrackSprite = null;
			}
			trackDescText.text = persistentSelectedTrack + "\n\n" + Info.tracks[persistentSelectedTrack].desc;
		}
		tilesContainer.gameObject.SetActive(!Info.randomTracks);

		randomTracksText.text = "Tracks:" + (Info.randomTracks ? "Random" : "Select");
	}
	public async void SwitchReady(bool init = false)
	{
		if (canStartRace && server.AmHost)
		{
			Info.actionHappening = ActionHappening.InWorld;
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
		string ServerSideTrackSHAHash = server.lobby.Data[Info.k_trackSHA].Value;
		string trackName = server.lobby.Data[Info.k_trackName].Value;
		string trackPath = Info.tracksPath + trackName + ".data";
		if (!File.Exists(trackPath))
			return false;

		return await MatchingSHA(Info.tracksPath + trackName + ".data", ServerSideTrackSHAHash);
	}
	async Task<bool> MatchingSHA(string filePath, string matchSHA)
	{
		string hash;
		using (var cryptoProvider = new SHA1CryptoServiceProvider())
		{
			byte[] buffer = await File.ReadAllBytesAsync(filePath);
			hash = BitConverter.ToString(cryptoProvider.ComputeHash(buffer));
		}
		return hash == matchSHA;
	}
	public async void ReadyButton(bool init = false)
	{
		var playerMe = server.PlayerMe;
		bool amReady = !init && !playerMe.ReadyGet();
		if (!init && Time.time - readyTimeoutTime < 1)
			return;
		readyTimeoutTime = Time.time;
		try
		{
			// UPDATE PLAYER
			playerMe.ReadySet(amReady);

			await server.UpdatePlayerData();

			if (playerMe.Id == server.lobby.HostId && amReady)
			{// UPDATE HOST INFO
				await server.UpdateServerData();
			}
		}
		catch (LobbyServiceException e)
		{
			Debug.Log("Ready switch failed: " + e.Message);
		}
		readyText.text = server.AmHost ? "HOST READY" : "READY";
		leaderboard.Refresh();
	}
	public void SwitchScoring(bool init)
	{
		int dir = 0;
		if (!init)
			dir = shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		Info.scoringType = (ScoringType)Wraparound((int)Info.scoringType + dir, 0, Enum.GetNames(typeof(ScoringType)).Length - 1);
		scoringText.text = Info.scoringType.ToString();
	}
}

