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
	class SponsorScore
	{
		public Livery sponsor;
		public int score;
	}
	public void OnEnable()
	{
		Refresh();
	}
	
	public void Refresh()
	{
		Player[] players = new Player[server.lobby.Players.Count];
		for (int i = 0; i < server.lobby.Players.Count; ++i)
			players[i] = server.lobby.Players[i];

		// Team scoring
		if (Info.scoringType == ScoringType.Championship)
		{
			List<SponsorScore> scores = new();

			foreach (var p in server.lobby.Players)
			{

				var playerSponsor = p.SponsorGet();
				var playerScore = p.ScoreGet();

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

			Array.Sort(players, (Player p2, Player p1) =>
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
			Array.Sort(players, (Player p2, Player p1) => p2.ScoreGet().CompareTo(p1.ScoreGet()));
		}

		// TITLE + PLAYERS
		for (int i=1; i<transform.childCount; ++i)
		{
			Destroy(transform.GetChild(i).gameObject);
		}
		foreach (var p in players)
		{
			Add(p);
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

