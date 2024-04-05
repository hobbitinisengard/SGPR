using RVP;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class Voting : NetworkBehaviour
{
	enum VoteFor { RESTART, END };
	float votingTimer;
	int votesForRestart;
	int votesForEnd;
	const float votingThreshold = .6f;
	Coroutine invalidatorCo;
	List<string> playersThatVoted = new();
	public static Voting I;
	private void Awake()
	{
		I = this;
	}
	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
	}
	public void VoteForEnd()
	{
		if(MultiPlayerSelector.I.server.AmHost)
			VoteForEndPassedRpc();
		else
			VoteForRpc(MultiPlayerSelector.I.server.PlayerMe.Id, VoteFor.END);
	}

	public void VoteForRestart()
	{
		if (MultiPlayerSelector.I.server.AmHost)
			VoteForRestartPassedRpc();
		else
			VoteForRpc(MultiPlayerSelector.I.server.PlayerMe.Id, VoteFor.RESTART);
	}

	[Rpc(SendTo.Server)]
	void VoteForRpc(string playerLobbyId, VoteFor voteFor)
	{
		if (playersThatVoted.Contains(playerLobbyId))
			return;

		Player p = MultiPlayerSelector.I.server.lobby.Players.First(p => p.Id == playerLobbyId);
		playersThatVoted.Add(playerLobbyId);
		
		int votesRequiredToPass = (int)Mathf.Ceil(votingThreshold * (F.I.ActivePlayers.Count - 1));
		string msg = null;
		Color color = Color.yellow;
		bool passed = false;
		if(voteFor == VoteFor.RESTART)
		{
			votesForRestart++;
			msg = $"voted for RESTART. [{votesForRestart}/{votesRequiredToPass}]";
			passed = votesForRestart >= votesRequiredToPass;
		}
		if(voteFor == VoteFor.END)
		{
			votesForEnd++;
			msg = $"voted for END. [{votesForEnd}/{votesRequiredToPass}]";
			color = Color.red;
			passed = votesForEnd >= votesRequiredToPass;
		}
		F.I.chat.AddChatRowRpc(p.NameGet(), msg, p.ReadColor(), color, F.I.chat.RpcTarget.Everyone);
		if (invalidatorCo != null)
			StopCoroutine(invalidatorCo);

		if(passed)
		{
			if (voteFor == VoteFor.RESTART)
			{
				VoteForRestartPassedRpc();
			}
			if (voteFor == VoteFor.END)
			{
				VoteForEndPassedRpc();
			}
			invalidatorCo = StartCoroutine(VoteInvalidator(-1));
		}
		else
		{
			invalidatorCo = StartCoroutine(VoteInvalidator(10));
		}
	}
	IEnumerator VoteInvalidator(float value)
	{
		votingTimer = value;

		while(votingTimer > 0)
		{
			votingTimer -= Time.deltaTime;
			yield return null;
		}
		if(votingTimer > -1)
			F.I.chat.AddChatRowRpc("*SERVER*", "all votes expired", Color.gray, Color.gray, F.I.chat.RpcTarget.Everyone);
		votesForEnd = 0;
		votesForRestart = 0;
		playersThatVoted.Clear();
	}
	[Rpc(SendTo.Everyone)]
	void VoteForRestartPassedRpc()
	{
		RaceManager.I.StartRace();
	}

	[Rpc(SendTo.NotMe)]
	void VoteForEndPassedRpc()
	{
		RaceManager.I.BackToMenu(applyScoring:false);
	}


	public void CountdownTillForceEveryoneToResults()
	{
		CountdownTillForceEveryoneToResultsRpc();
	}
	[Rpc(SendTo.Server)]
	void CountdownTillForceEveryoneToResultsRpc()
	{
		StartCoroutine(CountdownTillForceEveryoneToResultsSeqCo());
	}
	IEnumerator CountdownTillForceEveryoneToResultsSeqCo()
	{
		Debug.Log("CountdownTillForceEveryoneToResultsSeqCo");
		float timer = 30;
		while(timer > 0)
		{
			timer -= Time.deltaTime;
			yield return null;
		}
		TimeForRaceEndedRpc();
	}
	[Rpc(SendTo.Everyone)]
	void TimeForRaceEndedRpc()
	{
		RaceManager.I.TimeForRaceEnded();
	}
}
