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
using System.Net.Sockets;
using System.Net;
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
	public int maxPlayers;
	public const int basePort = 7770;
	string connectionType => (encryption == EncryptionType.DTLS) ? k_dtlsEncryption : k_udpEncryption;

	const int lobbyHeartbeatInterval = 15;
	const int lobbyPollInterval = 60;
	public static readonly string k_keyJoinCode = "RelayJoinCode";
	public const string k_actionHappening = "ah";
	public const string k_lobbyCode = "lc";
	public NetworkManager networkManager;

	string k_dtlsEncryption = "dtls";
	string k_udpEncryption = "udp";
	public LobbyEventCallbacks callbacks;
	ConcurrentQueue<string> createdLobbyIds = new ();
	public Lobby lobby { get; private set; }
	CountdownTimer heartbeatTimer = new(lobbyHeartbeatInterval);
	CountdownTimer pollForUpdatesTimer = new(lobbyPollInterval);
	private void Update()
	{
		heartbeatTimer.Tick(Time.deltaTime);
		pollForUpdatesTimer.Tick(Time.deltaTime);
	}
	async Task<Dictionary<string, PlayerDataObject>>  InitializePlayerData()
	{
		string myIpv4 = await Info.MyIPv4();
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
			{
				Info.k_message, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: "")
			},
			{
				Info.k_IPv4, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: myIpv4)
			},
		};
		return playerMeData;
	}
	void OnApplicationQuit()
	{
		DeleteLobby();
	}
	void DeleteLobby()
	{
		while (createdLobbyIds.TryDequeue(out var lobbyId))
		{
			LobbyService.Instance.DeleteLobbyAsync(lobbyId);
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
	public void DisconnectFromLobby()
	{
		networkManager.Shutdown();
		callbacks = null;
		DeleteLobby();
		heartbeatTimer.Reset();
	}
	async void Start()
	{
		await Authenticate();
		pollForUpdatesTimer.OnTimerStop += async () =>
		{
			await HandlePollForUpdateAsync();
			pollForUpdatesTimer.Start();
		};

	}
	/// <summary>
	/// Returns true if lobby creation succeeded
	/// </summary>
	public async Task<bool> CreateLobby(string trackName, string trackSHA)
	{
		try
		{
			heartbeatTimer.OnTimerStop += async () =>
			{
				await HandleHeartbeatAsync();
				heartbeatTimer.Start();
			};
			

			var alloc = await HostAllocateRelay();
			string relayJoinCode = await HostGetRelayJoinCode(alloc);

			CreateLobbyOptions options = new CreateLobbyOptions()
			{
				IsPrivate = false,
				Player = new Player(id: AuthenticationService.Instance.PlayerId, data: await InitializePlayerData()),
				Data = new()
				{
					{	k_keyJoinCode, new DataObject(
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

			lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

			Debug.Log("created lobby " + lobby.Name + " with code " + lobby.LobbyCode + ", id " + lobby.Id);

			heartbeatTimer.Start();
			pollForUpdatesTimer.Start();

			//await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
			//{
			//	Data = lobbyData
			//});
			networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(alloc, connectionType));
			networkManager.StartHost();
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
			JoinLobbyByIdOptions o = new() { Player = new Player(id: AuthenticationService.Instance.PlayerId, data: await InitializePlayerData()) };
			lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, o);

			pollForUpdatesTimer.Start();
			string relayJoinCode = lobby.Data[k_keyJoinCode].Value;
			var joinAlloc = await JoinRelay(relayJoinCode);
			networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAlloc, connectionType));
			networkManager.StartClient();
		}
		catch (Exception e)
		{
			Debug.LogError("Failed to join lobby" + e.Message);
			return false;
		}
		return true;
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
