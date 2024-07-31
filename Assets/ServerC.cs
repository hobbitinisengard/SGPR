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
using RVP;
class SponsorScore
{
	public Livery sponsor;
	public int score;
}
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
	//public List<LobbyRelayId> activePlayers = new();
	public EncryptionType encryption = EncryptionType.DTLS;
	public bool isCreatingLobby { get; private set; }
	public string lobbyName;
	public string password;
	public int maxPlayers;
	public const int basePort = 7770;
	string connectionType => (encryption == EncryptionType.DTLS) ? "dtls" : "udp";

	const int lobbyHeartbeatInterval = 15;
	const int lobbyPollInterval = 60;
	public const string k_relayCode = "RelayJoinCode";
	public const string k_actionHappening = "ah";
	public const string k_lobbyCode = "lc";
	public const string k_gameVer = "gv";
	public NetworkManager networkManager;
	public LobbyEventCallbacks callbacks = new();
	string callbacksLobbyId = "";
	public ConcurrentQueue<string> createdLobbyIds = new();
	public Lobby lobby;
	public CountdownTimer heartbeatTimer = new(lobbyHeartbeatInterval);
	CountdownTimer pollForUpdatesTimer = new(lobbyPollInterval);
	bool updatingPlayer;
	bool playerChanged;

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
	public const int tickRate = 10;
	public event Action OnLobbyExit;
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
			return lobby.Players.Count(p => p.ReadyGet());
		}
	}

	List<Player> RandomResults()
	{
		int playersNum = UnityEngine.Random.Range(1, 11);
		List<Player> players = new(playersNum);
		for (int i = 0; i < playersNum; ++i)
		{
			Dictionary<string, PlayerDataObject> Data = new()
			{
				[ServerC.k_Name] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public,
					(i == 0) ? F.I.playerData.playerName : F.RandomString(F.R(3, 13))),
				[ServerC.k_Sponsor] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, (F.R(1, F.I.Liveries + 1)).ToString())
			};
			players.Add(new Player(data: Data));

			switch (F.I.scoringType)
			{
				case ScoringType.Championship:
					players[i].Data[ServerC.k_score] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, F.R(0, 10).ToString());
					break;
				case ScoringType.Points:
					players[i].Data[ServerC.k_score] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, F.R(0, 500).ToString());
					break;
				case ScoringType.Victory:
					players[i].Data[ServerC.k_score] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, F.R(0, 50).ToString());
					break;
				default:
					break;
			}
		}
		return players;
	}
	public Player[] ScoreSortedPlayers
	{
		get
		{
			// DEBUG
			//var lobbyPlayers = RandomResults();
			var lobbyPlayers = lobby.Players;

			Player[] players = new Player[lobbyPlayers.Count];
			for (int i = 0; i < lobbyPlayers.Count; ++i)
				players[i] = lobbyPlayers[i];

			// Team scoring
			if (F.I.teams)
			{
				List<SponsorScore> teamScores = new();

				foreach (var p in lobbyPlayers)
				{

					var playerSponsor = p.SponsorGet();
					var playerScore = p.ScoreGet();

					var teamScore = teamScores.Find(s => playerSponsor == s.sponsor);

					if (teamScore == null)
					{
						teamScores.Add(new SponsorScore { sponsor = playerSponsor, score = playerScore });
					}
					else
					{
						teamScore.score += playerScore;
					}
				}
				teamScores.Sort((y, x) => x.score.CompareTo(y.score));

				Array.Sort(players, (Player p2, Player p1) =>
				{
					Livery p1Sponsor = p1.SponsorGet();
					Livery p2Sponsor = p2.SponsorGet();
					var teamScoreA = teamScores.Find(s => s.sponsor == p1Sponsor).score;
					var teamScoreB = teamScores.Find(s => s.sponsor == p2Sponsor).score;
					return teamScoreA.CompareTo(teamScoreB);
				});
			}
			else
			{ // Individual scoring

				Array.Sort(players, (Player a, Player b) => b.ScoreGet().CompareTo(a.ScoreGet()));
			}
			return players;
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
		DeleteEmptyLobbies();
	}

	public async void AddCallbacksToLobby()
	{
		if (callbacksLobbyId != lobby.Id)
		{
			callbacksLobbyId = lobby.Id;
			callbacks.PlayerDataChanged += MultiPlayerSelector.I.Callbacks_PlayerDataChanged;
			callbacks.LobbyChanged += MultiPlayerSelector.I.Callbacks_LobbyChanged;
			callbacks.PlayerJoined += MultiPlayerSelector.I.Callbacks_PlayerJoined;
			//Debug.Log("subscribe to lobby events");
			await Lobbies.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
		}
	}
	/// <summary>
	/// from 0 
	/// </summary>
	public int LeaderboardPos
	{
		get
		{
			Player[] sortedPlayers = new Player[lobby.Players.Count];
			for (int i = 0; i < sortedPlayers.Length; ++i)
				sortedPlayers[i] = lobby.Players[i];

			Array.Sort(sortedPlayers, (Player a, Player b) =>
			{
				return a.ScoreGet().CompareTo(b.ScoreGet());
			});

			int startingPos = Array.FindIndex(sortedPlayers, 0, p => p == PlayerMe);

			return startingPos;
		}
	}
	public void SetTrackName()
	{
		lobby.Data[k_trackName] = new DataObject(DataObject.VisibilityOptions.Member, F.I.s_trackName);
	}
	public async void DisconnectFromLobby()
	{
		if (lobby == null)
			return;
		
		heartbeatTimer.Pause();
		pollForUpdatesTimer.Pause();
		callbacks.PlayerDataChanged -= MultiPlayerSelector.I.Callbacks_PlayerDataChanged;
		callbacks.LobbyChanged -= MultiPlayerSelector.I.Callbacks_LobbyChanged;
		callbacks.PlayerJoined -= MultiPlayerSelector.I.Callbacks_PlayerJoined;
		callbacksLobbyId = "";

		RaceManager.I.playerCar = null;
		networkManager.Shutdown();
		await LobbyService.Instance.RemovePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
		DeleteEmptyLobbies();

		OnLobbyExit.Invoke();
		Debug.Log("DISCONNECTED");
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
					value: ((int)PlayerState.InLobbyUnready).ToString())
			},
			{
				k_score, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: "0")
			},
			{
				k_Sponsor, new PlayerDataObject(
					visibility: PlayerDataObject.VisibilityOptions.Member,
					value: ((int)F.I.s_PlayerCarSponsor).ToString())
			},
		};
		return playerMeData;
	}

	void DeleteEmptyLobbies()
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
	public int TeamsInLobby
	{
		get
		{
			if (!F.I.teams)
				return 0;
			bool[] teams = new bool[F.I.Liveries];
			foreach (var p in lobby.Players)
			{
				int sponsor = Mathf.Clamp((int)p.SponsorGet(), 1, F.I.Liveries);
				teams[sponsor] = true;
			}
			return teams.Count(t => t);
		}
	}
	string EncodeConfig()
	{
		string encodeConfig =
			((int)F.I.scoringType).ToString()
			+ (F.I.randomCars ? "1" : "0")
			+ (F.I.randomTracks ? "1" : "0")
			+ ((int)F.I.s_raceType).ToString()
			+ (F.I.s_laps).ToString("D2")
			+ (F.I.s_isNight ? "1" : "0")
			+ ((int)F.I.s_cpuLevel).ToString()
			+ F.I.s_cpuRivals
			+ ((int)F.I.s_roadType).ToString()
			+ (F.I.teams ? "1" : "0")
			+ (F.I.catchup ? "1" : "0");

		if (AnyClientsStillInRace)
			encodeConfig += GetCurRound().ToString("D2") + GetRounds().ToString("D2");
		else
			encodeConfig += F.I.CurRound.ToString("D2") + F.I.Rounds.ToString("D2");

		return encodeConfig;
	}
	public void DecodeConfig(string data)
	{
		if (F.I.scoringType != (ScoringType)(data[0] - '0')) // char to int
		{
			F.I.scoringType = (ScoringType)(data[0] - '0');
			ScoreSet(0);
		}

		F.I.randomCars = data[1] == '1';
		F.I.randomTracks = data[2] == '1';
		F.I.s_raceType = (RaceType)(data[3] - '0');
		F.I.s_laps = int.Parse(data[4..6]);
		F.I.s_isNight = data[6] == '1';
		F.I.s_cpuLevel = (CpuLevel)(data[7] - '0');
		F.I.s_cpuRivals = data[8] - '0';
		F.I.s_roadType = (PavementType)(data[9] - '0');
		F.I.catchup = false;
		F.I.teams = data[10] == '1';
		F.I.catchup = data[11] == '1';

		if (!AmHost)
		{
			F.I.CurRound = (byte)GetCurRound(data);
			var nRounds = (byte)GetRounds(data);

			if (F.I.Rounds != nRounds)
				ScoreSet(0);
			F.I.Rounds = nRounds;
		}

		if (!F.I.teams)
			F.I.s_PlayerCarSponsor = Livery.Random;
		if (F.I.teams && F.I.s_PlayerCarSponsor == Livery.Random)
			F.I.s_PlayerCarSponsor = Livery.TGR;
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
			if (playerChanged)
			{
				await Task.Delay(100);
				UpdatePlayerOptions options = new()
				{
					Data = PlayerMe.Data
				};
				await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, PlayerMe.Id, options);
			}
		}
		catch (LobbyServiceException e)
		{
			Debug.LogError("Failed to update player: " + e.Message);
		}
		updatingPlayer = false;
		playerChanged = false;
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
			return F.I.gameMode == MultiMode.Singleplayer || networkManager.IsHost;
		}
	}
	public ActionHappening ActionHappening
	{
		get { return (ActionHappening)Enum.Parse(typeof(ActionHappening), lobby.Data[k_actionHappening].Value); }
		set { lobby.Data[k_actionHappening] = new DataObject(DataObject.VisibilityOptions.Public, value.ToString()); }
	}

	public bool AnyClientsStillInRace
	{
		get
		{
			if (lobby == null)
				return false;
			return lobby.Players.Any(p => p.PlayerStateGet() == PlayerState.InRace);
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
					{  k_gameVer, new DataObject(
						visibility:DataObject.VisibilityOptions.Public,
						value:Info.VERSION)
					},
					{  k_relayCode, new DataObject(
						visibility:DataObject.VisibilityOptions.Public,
						value:relayJoinCode)
					},
					{
						k_actionHappening, new DataObject(
							visibility: DataObject.VisibilityOptions.Public,
							value: F.I.actionHappening.ToString())
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
	public bool GetTeams()
	{
		return lobby.Data[k_raceConfig].Value[10] == '1';
	}
	public int GetCurRound(string serverConfig)
	{
		return byte.Parse(serverConfig[12..14]);
	}
	public int GetCurRound()
	{
		return GetCurRound(lobby.Data[k_raceConfig].Value);
	}
	public int GetRounds(string serverConfig)
	{
		return byte.Parse(serverConfig[14..16]);
	}
	public int GetRounds()
	{
		return GetRounds(lobby.Data[k_raceConfig].Value);
	}
	public Livery GetSponsor()
	{
		return (Livery)int.Parse(PlayerMe.Data[k_Sponsor].Value);
	}
	public void SponsorSet()
	{
		string s = ((int)F.I.s_PlayerCarSponsor).ToString();
		if (s != PlayerMe.Data[ServerC.k_Sponsor].Value)
		{
			PlayerMe.Data[ServerC.k_Sponsor].Value = s;
			playerChanged = true;
		}
	}
	public void ScoreSet(int newScore)
	{
		if (newScore == 0)
			Debug.LogWarning("ScoreSet 0");
		string s = newScore.ToString();
		if (s != PlayerMe.Data[ServerC.k_score].Value)
		{
			PlayerMe.Data[ServerC.k_score].Value = s;
			playerChanged = true;
		}
	}
	public void ReadySet(bool ready)
	{
		if (!MultiPlayerSelector.I.gameObject.activeInHierarchy)
			ReadySet(PlayerState.InRace);
		else
			ReadySet(ready ? PlayerState.InLobbyReady : PlayerState.InLobbyUnready);
	}
	public void ReadySet(PlayerState ready)
	{
		string r = ((int)ready).ToString();
		if (r != PlayerMe.Data[ServerC.k_Ready].Value)
		{
			PlayerMe.Data[ServerC.k_Ready].Value = r;
			playerChanged = true;
		}
	}
	public void NameSet(string name)
	{
		if (name != PlayerMe.Data[ServerC.k_Name].Value)
		{
			PlayerMe.Data[ServerC.k_Name].Value = name;
			playerChanged = true;
		}
	}
	public void CarNameSet()
	{
		if (PlayerMe.Data[ServerC.k_carName].Value != F.I.s_playerCarName)
		{
			PlayerMe.Data[ServerC.k_carName].Value = F.I.s_playerCarName;
			playerChanged = true;
		}
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
			throw new RelayServiceException(e);
			//return default;
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
