using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
public class LeaderBoardTable : MonoBehaviour
{
	public ServerConnection server;
	public GameObject LeaderboardRowPrefab;
	public Sprite knob;
	public Sprite crown;
	Player[] playerOrder;
	class SponsorScore
	{
		public Livery sponsor;
		public int score;
	}
	public void OnEnable()
	{
		SortPlayersByScore();
		Refresh();
	}
	public void SortPlayersByScore()
	{
		if (Info.scoringType == ScoringType.Championship) // Team scoring
		{
			List<SponsorScore> scores = new();
			playerOrder = new Player[server.lobby.Players.Count];

			for(int i=0; i< server.lobby.Players.Count; ++i)
			{
				playerOrder[i] = server.lobby.Players[i];

				var playerSponsor = playerOrder[i].SponsorGet();
				var playerScore = playerOrder[i].ScoreGet();

				var teamScores = scores.Find(s => s.sponsor == playerSponsor);

				if (teamScores == null)
				{
					scores.Add(new SponsorScore { sponsor = playerSponsor, score = playerScore });
				}
				else
				{
					teamScores.score += playerScore;
				}
			}
			scores.Sort((y, x) => x.score.CompareTo(y.score));
			
			Array.Sort(playerOrder, (Player p2, Player p1) =>
			{
				Livery p1Sponsor = p1.SponsorGet();
				Livery p2Sponsor = p2.SponsorGet();
				var teamScoreA = scores.Find(s => s.sponsor == p1Sponsor).score;
				var teamScoreB = scores.Find(s => s.sponsor == p2Sponsor).score;
				return teamScoreA.CompareTo(teamScoreB);
			});
		}
		else
		{ // Individual scoring
			for (int i = 0; i < server.lobby.Players.Count; ++i)
			{
				playerOrder[i] = server.lobby.Players[i];
			}
			Array.Sort(playerOrder, (Player p2, Player p1) => p2.ScoreGet().CompareTo(p1.ScoreGet()));
		}
	}
	
	public void Refresh()
	{
		if (playerOrder.Length != server.lobby.Players.Count)
			SortPlayersByScore();
		// TITLE + PLAYERS
		for(int i=1; i<transform.childCount; ++i)
		{
			Destroy(transform.GetChild(i).gameObject);
		}
		foreach (var player in playerOrder)
		{
			Add(player);
		}
	}
	void Add(Player player)
	{
		var newRow = Instantiate(LeaderboardRowPrefab, transform).transform;
		newRow.name = player.NameGet();
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = (player.Id == server.lobby.HostId) ? crown : knob;
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = player.ReadyGet() ? Color.green : Color.yellow;
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = player.NameGet();
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().color = player.ReadColor();
		newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = Info.randomCars ? "*random*" : player.carNameGet();
		newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = ((Info.scoringType == ScoringType.Championship) ? "$ " : "") + player.ScoreGet().ToString();
	}
}

