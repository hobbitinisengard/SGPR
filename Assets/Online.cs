using RVP;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Online : NetworkBehaviour
{
	public static Online I;
	public NetworkVariable<bool> raceAlreadyStarted = new();
	readonly NetworkVariable<double> endraceDeadline = new();
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
	
	[Rpc(SendTo.Server)]
	public void CountdownTillForceEveryoneToResultsRpc(ulong winnerId, long raceStartTicks, RpcParams rpcParams = default)
	{
		// Only the finishing car's owner can announce this race's finish.
		// Ignore delayed announcements from a previous race or during countdown.
		if (rpcParams.Receive.SenderClientId != winnerId || F.I.gameMode != GameMode.Multiplayer ||
			F.I.actionHappening != ActionHappening.InRace || F.I.raceStartDate.Ticks != raceStartTicks ||
			CountDownSeq.Countdown > 0 || !RaceManager.I || !RaceManager.I.gameObject.activeInHierarchy)
			return;
		if (endraceDeadline.Value <= 0 && ResultsView.FinishedPlayers < ServerC.I.lobby.Players.Count)
			endraceDeadline.Value = NetworkManager.ServerTime.Time + Info.AfterMultiPlayerRaceWaitForPlayersSeconds;
	}
	public void ResetEndraceCountdown()
	{
		if (IsServer) endraceDeadline.Value = 0;
	}
	void Update()
	{
		// The replicated deadline also survives a late HUD activation or joining
		// while the countdown is already running. All peers use server time.
		if (!IsSpawned || F.I.gameMode != GameMode.Multiplayer ||
			F.I.actionHappening != ActionHappening.InRace || !RaceManager.I ||
			!RaceManager.I.gameObject.activeInHierarchy || !RaceManager.I.hud)
			return;
		if (RaceManager.I.RestartInProgress) return;
		var timer = RaceManager.I.hud.endraceTimer;
		if (timer) timer.SetNetworkDeadline(DateTime.UtcNow < F.I.raceStartDate ? 0 : endraceDeadline.Value);
	}

	public void ClientDisconnected(ulong id)
	{
		ClientDisconnectedRpc(id);
	}
	[Rpc(SendTo.Everyone)]
	void ClientDisconnectedRpc(ulong id)
	{
		if (F.I.actionHappening == ActionHappening.InRace)
			ResultsView.MarkDisconnected(id);
		else
			ResultsView.Remove(id);
	}

	public void EndRaceByTimeout()
	{
		if (!IsServer) return;
		// One host snapshot supplies the same order to every peer before stopping
		// race clocks. Locally elapsed countdowns and truncated times are not ranks.
		var cars = new List<VehicleParent>(F.I.s_cars);
		cars.RemoveAll(car => !car || !car.raceBox || !car.raceBox.enabled);
		cars.Sort((a, b) =>
		{
			int order = b.raceBox.RaceProgressLaps.CompareTo(a.raceBox.RaceProgressLaps);
			return order != 0 ? order : a.OwnerClientId.CompareTo(b.OwnerClientId);
		});
		var ids = new ulong[cars.Count];
		for (int i = 0; i < cars.Count; i++) ids[i] = cars[i].OwnerClientId;
		EndRaceByTimeoutRpc(ids);
	}
	[Rpc(SendTo.Everyone)]
	void EndRaceByTimeoutRpc(ulong[] unfinishedPlayers)
	{
		ResultsView.PreserveTimeoutPositions(unfinishedPlayers);
		RaceManager.I.StopRaceAfterTimeout();
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
		if(F.I.raceStartDate == DateTime.MinValue)
		{
			RaceManager.I.ExitButton();
		}
	}
}
