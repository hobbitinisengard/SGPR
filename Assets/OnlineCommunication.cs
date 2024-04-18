using RVP;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class OnlineCommunication : NetworkBehaviour
{
	public static OnlineCommunication I;
	NetworkVariable<FixedString128Bytes> _raceStartDate = new("");
	DateTime _parsedRaceStartDate;
	public DateTime raceStartDate
	{
		get { return _parsedRaceStartDate; }
		set
		{
			if (IsHost || F.I.gameMode == MultiMode.Singleplayer)
			{
				_parsedRaceStartDate = value;
				_raceStartDate.Value = value.ToString();
			}
		}
	}
	private void Awake()
	{
		raceStartDate = DateTime.MinValue;
		OnlineCommunication.I = this;
		_raceStartDate.OnValueChanged += RaceStartDateChanged;
	}
	//public override void OnNetworkSpawn()
	//{
	//	base.OnNetworkSpawn();
	//	AddActivePlayerRpc(NetworkManager.LocalClientId, AuthenticationService.Instance.PlayerId);
		
	//}
	private void RaceStartDateChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
	{
		_parsedRaceStartDate = DateTime.Parse(newValue.ToString());
	}
	//public void DelActivePlayer(string id)
	//{
	//	DelActivePlayerRpc(id);
	//}

	//[Rpc(SendTo.Server)]
	//void AddActivePlayerRpc(ulong relayId, string lobbyId)
	//{
	//	if(false == ServerC.I.activePlayers.Any(ap => ap.playerLobbyId == lobbyId))
	//	{
	//		ServerC.I.activePlayers.Add(new LobbyRelayId() { playerRelayId = relayId, playerLobbyId = lobbyId });
	//	}
	//}
	//[Rpc(SendTo.Server)]
	//void DelActivePlayerRpc(string id)
	//{
	//	var el = ServerC.I.activePlayers.First(p => p.playerLobbyId == id);
	//	if(el != null)
	//		ServerC.I.activePlayers.Remove(el);
	//}
	public void GibCar()
	{
		GibCarRpc(ServerC.I.PlayerMe.Id, RpcTarget.Server);
	}
	public void GibCar(Vector3 position, Quaternion rotation)
	{
		GibCarAtRpc(ServerC.I.PlayerMe.Id, position, rotation, RpcTarget.Server);
	}
	[Rpc(SendTo.SpecifiedInParams)]
	void GibCarAtRpc(string lobbyId, Vector3 position, Quaternion rotation, RpcParams ps)
	{
		RaceManager.I.SpawnCarForPlayer(ps.Receive.SenderClientId, lobbyId, position, rotation);
	}
	[Rpc(SendTo.SpecifiedInParams)]
	void GibCarRpc(string lobbyId, RpcParams ps)
	{
		RaceManager.I.SpawnCarForPlayer(ps.Receive.SenderClientId, lobbyId, null, null);
	}
	public void CountdownTillForceEveryoneToResults()
	{
		CountdownTillForceEveryoneToResultsRpc();
	}
	[Rpc(SendTo.Everyone)]
	void CountdownTillForceEveryoneToResultsRpc()
	{
		if (RaceManager.I.playerCar.raceBox.enabled)
			RaceManager.I.hud.endraceTimer.gameObject.SetActive(true);
	}
}
