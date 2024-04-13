using RVP;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;

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
			if (IsServer || F.I.gameMode == MultiMode.Singleplayer)
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
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		AddActivePlayerRpc(NetworkManager.LocalClientId, AuthenticationService.Instance.PlayerId);
		
	}
	private void RaceStartDateChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
	{
		_parsedRaceStartDate = DateTime.Parse(newValue.ToString());
	}
	public void DelActivePlayer(string id)
	{
		DelActivePlayerRpc(id);
	}

	[Rpc(SendTo.Server)]
	void AddActivePlayerRpc(ulong relayId, string lobbyId)
	{
		if(false == ServerC.I.activePlayers.Any(ap => ap.playerLobbyId == lobbyId))
		{
			ServerC.I.activePlayers.Add(new LobbyRelayId() { playerRelayId = relayId, playerLobbyId = lobbyId });
		}
	}
	[Rpc(SendTo.Server)]
	void DelActivePlayerRpc(string id)
	{
		var el = ServerC.I.activePlayers.First(p => p.playerLobbyId == id);
		if(el != null)
			ServerC.I.activePlayers.Remove(el);
	}
	[Rpc(SendTo.SpecifiedInParams)]
	public void RequestCarForMeLatecomerRpc(RpcParams ps)
	{
		RaceManager.I.SpawnCarForLatecomer(ps.Receive.SenderClientId);
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
