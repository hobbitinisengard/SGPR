using RVP;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Online : NetworkBehaviour
{
	public static Online I;
	public NetworkVariable<bool> raceAlreadyStarted = new();
	private void Awake()
	{
		I = this;
	}
	public override async void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if(F.I.actionHappening == ActionHappening.InRace)
		{
			while(MultiPlayerSelector.I.Busy)
			{
				await Task.Delay(100);
			}
			MultiPlayerSelector.I.thisView.ToRaceScene();
		}
	}
	public override void OnNetworkDespawn()
	{
		raceAlreadyStarted = new();
		base.OnNetworkDespawn();
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
	public void ActivateEndraceTimer()
	{
		if(F.I.gameMode == MultiMode.Multiplayer)
			CountdownTillForceEveryoneToResultsRpc();
	}
	[Rpc(SendTo.Everyone)]
	void CountdownTillForceEveryoneToResultsRpc()
	{
		if (ResultsView.FinishedPlayers < ServerC.I.lobby.Players.Count)
			RaceManager.I.hud.endraceTimer.gameObject.SetActive(true);
	}

	public void ClientDisconnected(ulong id)
	{
		ClientDisconnectedRpc(id);
	}
	[Rpc(SendTo.Everyone)]
	void ClientDisconnectedRpc(ulong id)
	{
		ResultsView.Remove(id);
	}

	public void AskHostForRacestartdate()
	{
		AskHostForRacestartdateRpc(RpcTarget.Server);
	}
	[Rpc(SendTo.SpecifiedInParams)]
	private void AskHostForRacestartdateRpc(RpcParams ps)
	{
		TellRacestartdateRpc(F.I.raceStartDate, RpcTarget.Single(ps.Receive.SenderClientId, RpcTargetUse.Temp));
	}
	[Rpc(SendTo.SpecifiedInParams)]
	private void TellRacestartdateRpc(DateTime raceStartDate, RpcParams ps)
	{
		F.I.raceStartDate = raceStartDate;
	}
}
