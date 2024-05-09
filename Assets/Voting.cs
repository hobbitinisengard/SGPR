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
	const float votingThreshold = .51f;
	Coroutine invalidatorCo;
	List<string> playersThatVoted = new();
	public static Voting I;
	private void Awake()
	{
		I = this;
	}
	public void VoteForEnd()
	{
		VoteForRpc(ServerC.I.PlayerMe.Id, VoteFor.END);
	}

	public void VoteForRestart()
	{
		if (ServerC.I.AmHost)
			VoteForRestartPassedRpc();
		else
			VoteForRpc(ServerC.I.PlayerMe.Id, VoteFor.RESTART);
	}

	[Rpc(SendTo.Server)]
	void VoteForRpc(string playerLobbyId, VoteFor voteFor)
	{
		if (playersThatVoted.Contains(playerLobbyId))
			return;

		Player p = ServerC.I.lobby.Players.First(p => p.Id == playerLobbyId);
		playersThatVoted.Add(playerLobbyId);
		
		int votesRequiredToPass = (int)Mathf.Ceil(votingThreshold * ServerC.I.lobby.Players.Count);
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
		RaceManager.I.RemoveCars();
		RaceManager.I.StartRace();
	}

	public void EndForEveryone()
	{
		VoteForEndPassedRpc();
	}
	[Rpc(SendTo.Everyone)]
	void VoteForEndPassedRpc()
	{
		RaceManager.I.BackToMenu(applyScoring:false);
	}
	
}
