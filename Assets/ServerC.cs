using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using System.Collections.Concurrent;
public class ServerC : MonoBehaviour
{
	//public Dictionary<string, PlayerDataObject> playerMeData;
	//public Dictionary<string, DataObject> lobbyData;
	public enum EncryptionType
	{
		DTLS, // Datagram Transport Layer Security
		UDP
	}
	public static ServerC I;
	/// <summary>
	/// active players are connected to relay & lobby
	/// </summary>
	public List<LobbyRelayId> activePlayers = new();
	public EncryptionType encryption = EncryptionType.DTLS;
	public bool isCreatingLobby { get; private set; }
	public string lobbyName;
	public string password;
	public int maxPlayers;
	public const int basePort = 7770;
	string connectionType => (encryption == EncryptionType.DTLS) ? k_dtlsEncryption : k_udpEncryption;

	const int lobbyHeartbeatInterval = 15;
	const int lobbyPollInterval = 60;
	public static readonly string k_relayCode = "RelayJoinCode";
	public const string k_actionHappening = "ah";
	public const string k_lobbyCode = "lc";
	NetworkManager networkManager;

	string k_dtlsEncryption = "dtls";
	string k_udpEncryption = "udp";
	public LobbyEventCallbacks callbacks = new();
	string callbacksLobbyId = "";
	public ConcurrentQueue<string> createdLobbyIds = new();
	public Lobby lobby;
	public CountdownTimer heartbeatTimer = new(lobbyHeartbeatInterval);
	CountdownTimer pollForUpdatesTimer = new(lobbyPollInterval);
	private bool updatingPlayer;
	public const string k_Ready = "r";
	public const string k_Sponsor = "s";
	public const string k_Name = "n";
	public const string k_carName = "c";
	public const string k_score = "sc";

	public const string k_raceConfig = "e";
	public const string k_zippedTrack = "t";
	public const string k_duringRace = "d";
	public const string k_trackSHA = "ts";
	public const string k_trackName = "tn";
	private void Awake()
	{
		networkManager = GetComponent<NetworkManager>();
		I = this;

		heartbeatTimer.OnTimerStop += async () =>
		{
			await HandleHeartbeatAsync();
			heartbeatTimer.Start();
		};
		pollForUpdatesTimer.OnTimerStop += async () =>
		{
			await HandlePollForUpdateAsync();
			pollForUpdatesTimer.Start();
		};
	}
	public int readyPlayers
	{
		get
		{
			return lobby.Players.Count(p => p.ReadyGet() == true);
		}
	}
	private async void Start()
	{
		await Authenticate();
	}
	private void Update()
	{
		heartbeatTimer.Tick(Time.deltaTime);
		pollForUpdatesTimer.Tick(Time.deltaTime);
	}
	void OnApplicationQuit()
	{
		DeleteLobby();
	}
	public async void AddCallbacksToLobby()
	{
		if (callbacksLobbyId != lobby.Id)
		{
			callbacksLobbyId = lobby.Id;
			Debug.Log("subscribe to lobby events");
			await Lobbies.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
		}
	}
	public async void DisconnectFromLobby()
	{
		if (lobby == null)
			return;
		DeleteLobby();
		activePlayers.Clear();
		heartbeatTimer.Pause();
		pollForUpdatesTimer.Pause();
		await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
		networkManager.Shutdown();
		callbacksLobbyId = "";
	}
	public async Task GetLobbyManually()
	{
		lobby = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
	}
	Dictionary<string, PlayerDataObject> InitializePlayerData()
	{
		Dictionary<string, PlayerDataObject> playerMeData = new()
		{
			{
				k_carName, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: "car01")
			},
			{
				k_Name, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: F.I.playerData.playerName)
			},
			{
				k_Ready, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: "false")
			},
			{
				k_score, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: "0")
			},
			{
				k_Sponsor, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: ((Livery)Random.Range(1,F.I.Liveries+1)).ToString())
			},
			//{
			//	F.I.k_message, new PlayerDataObject(
			//		visibility: PlayerDataObject.VisibilityOptions.Member,
			//		value: "")
			//},
		};
		return playerMeData;
	}

	void DeleteLobby()
	{
		while (createdLobbyIds.TryDequeue(out var lobbyId))
		{
			if (lobby.Players.Count == 1)
			{
				Debug.Log("Deleting lobby" + lobbyId);
				LobbyService.Instance.DeleteLobbyAsync(lobbyId);
			}
		}
	}

	string EncodeConfig()
	{
		string encodeConfig = ((int)F.I.scoringType).ToString() + (F.I.randomCars ? "1" : "0") + (F.I.randomTracks ? "1" : "0")
			+ ((int)F.I.s_raceType).ToString() + (F.I.s_laps).ToString("D2") + (F.I.s_isNight ? "1" : "0") + ((int)F.I.s_cpuLevel).ToString()
			+ (F.I.s_cpuRivals).ToString() + ((int)F.I.s_roadType).ToString() + (F.I.s_catchup ? "1" : "0");
		return encodeConfig;
	}
	public Player Host
	{
		get
		{
			return lobby.Players.First(p => p.Id == lobby.HostId);
		}
	}


	public bool ServerInRace
	{
		get
		{
			string cfg = lobby.Data[k_raceConfig].Value;
			if (cfg.Length < 12)
				return false;
			return cfg[11] == '1';
		}
	}
	public async void UpdateServerData()
	{
		try
		{
			lobby.Data[k_raceConfig] = new DataObject(DataObject.VisibilityOptions.Member, EncodeConfig());

			UpdateLobbyOptions options = new()
			{
				Data = lobby.Data
			};
			await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, options);
		}
		catch (LobbyServiceException e)
		{
			Debug.LogError("Failed to update server data: " + e.Message);
		}
	}
	public async void UpdatePlayerData()
	{
		if (updatingPlayer)
			return;

		updatingPlayer = true;
		try
		{
			await Task.Delay(100);
			UpdatePlayerOptions options = new()
			{
				Data = PlayerMe.Data
			};
			await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, PlayerMe.Id, options);
		}
		catch (LobbyServiceException e)
		{
			Debug.LogError("Failed to update player: " + e.Message);
		}
		updatingPlayer = false;
	}

	public Player PlayerMe
	{
		get
		{
			return lobby.Players.First(p => p.Id == AuthenticationService.Instance.PlayerId);
		}
	}
	public bool AmHost
	{
		get
		{
			return networkManager.IsHost || F.I.gameMode == MultiMode.Singleplayer;
		}
	}



	private async Task Authenticate()
	{
		await Authenticate("Player" + Random.Range(0, 1000));
	}
	async Task Authenticate(string playerName)
	{
		if (UnityServices.State == ServicesInitializationState.Uninitialized)
		{
			var options = new InitializationOptions();
			options.SetProfile(playerName);
			await UnityServices.InitializeAsync(options);
		}
		AuthenticationService.Instance.SignedIn += () =>
		{
			Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
		};
		if (!AuthenticationService.Instance.IsSignedIn)
		{
			await AuthenticationService.Instance.SignInAnonymouslyAsync();
		}
	}


	/// <summary>
	/// Returns true if lobby creation succeeded
	/// </summary>
	public async Task<bool> CreateLobby(string trackName, string trackSHA)
	{
		isCreatingLobby = true;
		try
		{
			string relayJoinCode = await StartRelay();

			CreateLobbyOptions options = new()
			{
				IsPrivate = false,
				Player = new Player(id: AuthenticationService.Instance.PlayerId, data: InitializePlayerData()),
				Data = new()
				{
					{  k_relayCode, new DataObject(
						visibility:DataObject.VisibilityOptions.Public,
						value:relayJoinCode)
					},
					{
						k_actionHappening, new DataObject(
							visibility: DataObject.VisibilityOptions.Public,
							value: ActionHappening.InLobby.ToString())
					},
					{
						k_raceConfig, new DataObject(
							visibility: DataObject.VisibilityOptions.Member,
							value: EncodeConfig())
					},
					{
						k_trackSHA, new DataObject(
							visibility: DataObject.VisibilityOptions.Member,
							value: trackSHA)
					},
					{
						k_trackName, new DataObject(
							visibility: DataObject.VisibilityOptions.Member,
							value: trackName)
					},
				},
			};
			if (password.Length > 7)
				options.Password = password;

			lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
			AddCallbacksToLobby();
			createdLobbyIds.Enqueue(lobby.Id);
			Debug.Log("created lobby " + lobby.Name + " with code " + lobby.LobbyCode + ", id " + lobby.Id);
			heartbeatTimer.Start();
			pollForUpdatesTimer.Start();
		}
		catch (LobbyServiceException e)
		{
			isCreatingLobby = false;
			Debug.LogError("Failed to create lobby: " + e.Message);
			return false;
		}
		isCreatingLobby = false;
		return true;
	}
	public ScoringType GetScoringType()
	{
		return (ScoringType)(lobby.Data[k_raceConfig].Value[0] - '0');
	}
	/// <summary>
	/// Connect to lobby Id or Quick Join
	/// </summary>
	public async Task<bool> JoinLobby(string lobbyId)
	{
		try
		{
			JoinLobbyByIdOptions o = new()
			{
				Player = new Player(id: AuthenticationService.Instance.PlayerId, data: InitializePlayerData())
			};
			if (password != null && password.Length > 7)
				o.Password = password;
			lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, o);
			AddCallbacksToLobby();
			pollForUpdatesTimer.Start();

			string relayJoinCode = lobby.Data[k_relayCode].Value;
			await JoinRelayByCode(relayJoinCode);

			
		}
		catch (Exception e)
		{
			Debug.LogError("Failed to join lobby" + e.Message);
			return false;
		}
		return true;
	}
	public async Task<string> StartRelay()
	{
		string relayJoinCode = null;
		try
		{
			networkManager.Shutdown();
			var alloc = await HostAllocateRelay();
			relayJoinCode = await HostGetRelayJoinCode(alloc);
			networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(alloc, connectionType));
			networkManager.StartHost();
		}
		catch (Exception e)
		{
			Debug.Log(e.Message);
		}
		return relayJoinCode;
	}
	public async Task JoinRelayByCode(string relayJoinCode)
	{
		networkManager.Shutdown();
		var joinAlloc = await JoinRelay(relayJoinCode);
		networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAlloc, connectionType));
		networkManager.StartClient();
	}
	async Task<Allocation> HostAllocateRelay()
	{
		try
		{
			var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1); // excluding host
			return allocation;
		}
		catch (RelayServiceException e)
		{
			Debug.LogError("Relay alloc failed: " + e.Message);
			return default;
		}
	}
	async Task<string> HostGetRelayJoinCode(Allocation allocation)
	{
		try
		{
			string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
			return relayJoinCode;
		}
		catch (RelayServiceException e)
		{
			Debug.LogError("failed to get Relay join code: " + e.Message);
			return default;
		}
	}
	async Task<JoinAllocation> JoinRelay(string relayJoinCode)
	{
		try
		{
			var joinAlloc = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
			return joinAlloc;
		}
		catch (RelayServiceException e)
		{
			Debug.LogError("failed to get join relay: " + e.Message);
			return default;
		}
	}
	async Task HandlePollForUpdateAsync()
	{
		try
		{
			var lobby = await LobbyService.Instance.GetLobbyAsync(this.lobby.Id);
		}
		catch (LobbyServiceException e)
		{
			Debug.LogError("Failed to poll for updates on lobby: " + e.Message);
		}
	}

	async Task HandleHeartbeatAsync()
	{
		try
		{
			await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
		}
		catch (LobbyServiceException e)
		{
			Debug.Log("Failed to heartbeat lobby " + e.Message);
		}
	}
	
	
}
