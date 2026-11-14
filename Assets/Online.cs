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
	public bool RaceStartDateReceived { get; private set; }
	bool awaitingRaceStartDate;
	int raceStartRequestVersion;
	float nextRaceStartRequest;
	public void PrepareRaceStartSynchronization()
	{
		raceStartRequestVersion++;
		RaceStartDateReceived = false;
		awaitingRaceStartDate = true;
		nextRaceStartRequest = 0;
	}
	private void Awake()
	{
		I = this;
	}
	public override async void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!IsServer && F.I.actionHappening == ActionHappening.InRace)
			PrepareRaceStartSynchronization();
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
		awaitingRaceStartDate = false;
		RaceStartDateReceived = false;
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
		// A request can reach the host before its track/car has loaded. Retry
		// independently of HUD activation instead of accepting an invalid date.
		if (IsSpawned && !IsServer && awaitingRaceStartDate &&
			F.I.gameMode == GameMode.Multiplayer && F.I.actionHappening == ActionHappening.InRace &&
			RaceManager.I && RaceManager.I.gameObject.activeInHierarchy &&
			Time.realtimeSinceStartup >= nextRaceStartRequest)
			AskHostForRacestartdate();
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

	[Rpc(SendTo.Everyone)]
	public void CompleteSurvivalRaceRpc(ulong winnerId, long raceStartTicks, RpcParams rpc = default)
	{
		if (rpc.Receive.SenderClientId != Unity.Netcode.NetworkManager.ServerClientId ||
			F.I.gameMode != GameMode.Multiplayer || F.I.raceStartDate.Ticks != raceStartTicks ||
			F.I.actionHappening != ActionHappening.InRace || !RaceManager.I ||
			RaceManager.I.RestartInProgress) return;
		ResetEndraceCountdown();
		foreach (var car in F.I.s_cars)
			if (car && car.OwnerClientId == winnerId)
			{
				RaceManager.I.CompleteSurvivalRace(car);
				break;
			}
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
		awaitingRaceStartDate = true;
		nextRaceStartRequest = Time.realtimeSinceStartup + 1f;
		if (IsSpawned && !IsServer)
			AskHostForRacestartdateRpc(raceStartRequestVersion, RpcTarget.Server);
	}
	[Rpc(SendTo.SpecifiedInParams)]
	private void AskHostForRacestartdateRpc(int requestVersion, RpcParams ps)
	{
		if (!IsServer || !RaceManager.I || !RaceManager.I.gameObject.activeInHierarchy ||
			RaceManager.I.RestartInProgress || F.I.actionHappening != ActionHappening.InRace ||
			F.I.raceStartDate == DateTime.MinValue || !RaceManager.I.RaceStartPrepared) return;
		TellRacestartdateRpc(F.I.raceStartDate, requestVersion,
			RpcTarget.Single(ps.Receive.SenderClientId, RpcTargetUse.Temp));
	}
	[Rpc(SendTo.SpecifiedInParams)]
	private void TellRacestartdateRpc(DateTime raceStartDate, int requestVersion, RpcParams ps)
	{
		if (ps.Receive.SenderClientId != Unity.Netcode.NetworkManager.ServerClientId ||
			requestVersion != raceStartRequestVersion || raceStartDate == DateTime.MinValue ||
			F.I.gameMode != GameMode.Multiplayer || F.I.actionHappening != ActionHappening.InRace ||
			!RaceManager.I || !RaceManager.I.gameObject.activeInHierarchy) return;
		awaitingRaceStartDate = false;
		RaceStartDateReceived = true;
		F.I.raceStartDate = raceStartDate;
		CountDownSeq.Countdown = (float)(raceStartDate - DateTime.UtcNow).TotalSeconds;
		// A delayed correction must never leave a positive, frozen countdown
		// after its coroutine has already deactivated itself.
		if (CountDownSeq.Countdown > 0 && RaceManager.I.playerCar &&
			RaceManager.I.hud.gameObject.activeInHierarchy && !RaceManager.I.RestartInProgress)
			RaceManager.I.countDownSeq.gameObject.SetActive(true);
	}
}
