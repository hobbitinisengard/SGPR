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
public class ServerConnection : MonoBehaviour
{
	//public Dictionary<string, PlayerDataObject> playerMeData;
	//public Dictionary<string, DataObject> lobbyData;
	public enum EncryptionType
	{
		DTLS, // Datagram Transport Layer Security
		UDP
	}
	public EncryptionType encryption = EncryptionType.DTLS;
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
	public NetworkManager networkManager;

	string k_dtlsEncryption = "dtls";
	string k_udpEncryption = "udp";
	public LobbyEventCallbacks callbacks;
	public ConcurrentQueue<string> createdLobbyIds = new ();
	public Lobby lobby { get; private set; }
	public CountdownTimer heartbeatTimer = new(lobbyHeartbeatInterval);
	CountdownTimer pollForUpdatesTimer = new(lobbyPollInterval);
	private void Awake()
	{
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
	public async void DisconnectFromLobby()
	{
		if (lobby == null)
			return;

		callbacks = null;
		Info.mpSelector = null;
		DeleteLobby();
		heartbeatTimer.Pause();
		pollForUpdatesTimer.Pause();
		await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
		networkManager.Shutdown();
	}
	Dictionary<string, PlayerDataObject>  InitializePlayerData()
	{
		Dictionary<string, PlayerDataObject> playerMeData = new()
		{
			{
				Info.k_carName, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: Info.cars[Random.Range(0,Info.cars.Length)].name)
			},
			{
				Info.k_Name, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: Info.playerData.playerName)
			},
			{
				Info.k_Ready, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: "false")
			},
			{
				Info.k_score, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: "0")
			},
			{
				Info.k_Sponsor, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: ((Livery)Random.Range(1,Info.Liveries+1)).ToString())
			},
			//{
			//	Info.k_message, new PlayerDataObject(
			//		visibility: PlayerDataObject.VisibilityOptions.Member,
			//		value: "")
			//},
		};
		return playerMeData;
	}
	
	void DeleteLobby()
	{
		Debug.Log("try deleting lobbies..");
		while (createdLobbyIds.TryDequeue(out var lobbyId))
		{
			if(lobby.Players.Count == 1)
			{
				Debug.Log("Deleting lobby" + lobbyId);
				LobbyService.Instance.DeleteLobbyAsync(lobbyId);
			}
		}
	}
	
	string EncodeConfig()
	{
		
		string encodeConfig =  ((int)Info.scoringType).ToString() + (Info.randomCars ? "1" : "0") + (Info.randomTracks ? "1" : "0")
			+ ((int)Info.s_raceType).ToString() + (Info.s_laps).ToString("D2") + (Info.s_isNight ? "1" : "0") + ((int)Info.s_cpuLevel).ToString()
			+ (Info.s_cpuRivals).ToString() + ((int)Info.s_roadType).ToString() + (Info.s_catchup ? "1" : "0");
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
			string cfg = lobby.Data[Info.k_raceConfig].Value;
			if (cfg.Length < 12)
				return false;
			return cfg[11] == '1';
		}
	}
	public async Task UpdateServerData()
	{
		try
		{
			lobby.Data[Info.k_raceConfig] = new DataObject(DataObject.VisibilityOptions.Member, EncodeConfig());

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
	public async Task UpdatePlayerData()
	{
		try
		{
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
			return AuthenticationService.Instance.PlayerId == lobby.HostId;
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
		try
		{
			string relayJoinCode = await StartRelay();

			CreateLobbyOptions options = new()
			{
				IsPrivate = false,
				Player = new Player(id: AuthenticationService.Instance.PlayerId, data: InitializePlayerData()),
				Data = new()
				{
					{	k_relayCode, new DataObject(
						visibility:DataObject.VisibilityOptions.Public,
						value:relayJoinCode)
					},
					{
						k_actionHappening, new DataObject(
							visibility: DataObject.VisibilityOptions.Public,
							value: ActionHappening.InLobby.ToString())
					},
					{
						Info.k_raceConfig, new DataObject(
							visibility: DataObject.VisibilityOptions.Member,
							value: EncodeConfig())
					},
					{
						Info.k_trackSHA, new DataObject(
							visibility: DataObject.VisibilityOptions.Member,
							value: trackSHA)
					},
					{
						Info.k_trackName, new DataObject(
							visibility: DataObject.VisibilityOptions.Member,
							value: trackName)
					},
				},
			};
			if (password.Length > 7)
				options.Password = password;

			lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

			createdLobbyIds.Enqueue(lobby.Id);
			Debug.Log("created lobby " + lobby.Name + " with code " + lobby.LobbyCode + ", id " + lobby.Id);
			heartbeatTimer.Start();
			pollForUpdatesTimer.Start();
		}
		catch (LobbyServiceException e)
		{
			Debug.LogError("Failed to create lobby: " + e.Message);
			return false;
		}
		return true;
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
			if(password != null && password.Length > 7)
				o.Password = password;
			lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, o);

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
