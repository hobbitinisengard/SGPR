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

public class LobbyView : MonoBehaviour
{
	public enum EncryptionType
	{
		DTLS, // Datagram Transport Layer Security
		WSS // Websocket secure
	}
	EncryptionType encryption = EncryptionType.DTLS;
	string connectionType => (encryption == EncryptionType.DTLS) ? k_dtlsEncryption : k_wssEncryption;
	const int lobbyHeartbeatInterval = 20;
	const int lobbyPollInterval = 60;
	string k_dtlsEncryption;
	string k_wssEncryption;
	string lobbyName;
	int maxPlayers;
	private string playerId;
	private string playerName;
	private Lobby currentLobby;
	CountdownTimer heartbeatTimer = new (lobbyHeartbeatInterval);
	CountdownTimer pollForUpdatesTimer = new(lobbyPollInterval);
	private string k_keyJoinCode;
	NetworkManager networkManager;
	private async void Start()
	{
		networkManager = GetComponent<NetworkManager>();
		await Authenticate();
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
			playerId = AuthenticationService.Instance.PlayerId;
			this.playerName = playerName;
		}
	}
	public async Task QuickJoinLobby()
	{
		try
		{
			currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
			pollForUpdatesTimer.Start();
			string relayJoinCode = currentLobby.Data[k_keyJoinCode].Value;
			var joinAlloc = await JoinRelay(relayJoinCode);
			networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(joinAlloc, connectionType));
			networkManager.StartClient();
		}
		catch(LobbyServiceException e)
		{
			Debug.LogError("Failed to quick join lobby" + e.Message);
		}
	}
	public async Task CreateLobby()
	{
		try
		{
			var alloc = await HostAllocateRelay();
			string relayJoinCode = await HostGetRelayJoinCode(alloc);
			CreateLobbyOptions options = new()
			{
				IsPrivate = false,
			};
			currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
			Debug.Log("created lobby " + currentLobby.Name + " with code" + currentLobby.LobbyCode);
			heartbeatTimer.Start();
			pollForUpdatesTimer.Start();

			await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
			{
				Data = new Dictionary<string, DataObject>
				{
					{ k_keyJoinCode, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
				}
			});
			networkManager.GetComponent<UnityTransport>().SetRelayServerData(new RelayServerData(alloc, connectionType));
			networkManager.StartHost();
		}
		catch (LobbyServiceException e)
		{
			Debug.LogError("Failed to create lobby: " + e.Message);
		}
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
			var lobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
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
			await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
		}
		catch (LobbyServiceException e)
		{
			Debug.Log("Failed to heartbeat lobby " + e.Message);
		}
	}
}
