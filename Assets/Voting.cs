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
	public override void OnNetworkSpawn()
	{
		StartCoroutine(Initialize());
		base.OnNetworkSpawn();
	}
	public override void OnNetworkDespawn()
	{
		Info.raceManager.voting = null;
		base.OnNetworkDespawn();
	}
	IEnumerator Initialize()
	{
		while (Info.raceManager == null)
			yield return null;
		Info.raceManager.voting = this;
	}
	public void VoteForEnd()
	{
		if(Info.mpSelector.server.AmHost)
			VoteForEndPassedRpc();
		else
			VoteForRpc(Info.mpSelector.server.PlayerMe.Id, VoteFor.END);
	}

	public void VoteForRestart()
	{
		if (Info.mpSelector.server.AmHost)
			VoteForRestartPassedRpc();
		else
			VoteForRpc(Info.mpSelector.server.PlayerMe.Id, VoteFor.RESTART);
	}

	[Rpc(SendTo.Server)]
	void VoteForRpc(string playerLobbyId, VoteFor voteFor)
	{
		if (playersThatVoted.Contains(playerLobbyId))
			return;

		Player p = Info.mpSelector.server.lobby.Players.First(p => p.Id == playerLobbyId);
		playersThatVoted.Add(playerLobbyId);
		
		int votesRequiredToPass = (int)Mathf.Ceil(votingThreshold * (Info.ActivePlayers.Count - 1));
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
		Info.chat.AddChatRowRpc(p.NameGet(), msg, p.ReadColor(), color, Info.chat.RpcTarget.Everyone);
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
			Info.chat.AddChatRowRpc("*SERVER*", "all votes expired", Color.gray, Color.gray, Info.chat.RpcTarget.Everyone);
		votesForEnd = 0;
		votesForRestart = 0;
		playersThatVoted.Clear();
	}
	[Rpc(SendTo.Everyone)]
	void VoteForRestartPassedRpc()
	{
		Info.raceManager.StartRace();
	}

	[Rpc(SendTo.Everyone)]
	void VoteForEndPassedRpc()
	{
		Info.raceManager.BackToMenu(applyScoring:false);
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
		Info.raceManager.TimeForRaceEnded();
	}
}
