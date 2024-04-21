using RVP;
using System;
using Unity.Netcode;
using UnityEngine;

public class OnlineCommunication : NetworkBehaviour
{
	public static OnlineCommunication I;
	public NetworkVariable<bool> raceAlreadyStarted = new();
	private void Awake()
	{
		I = this;
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
