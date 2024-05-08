using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
public class LeaderBoardTable : MonoBehaviour
{
	public ServerC server;
	public GameObject LeaderboardRowPrefab;
	public Sprite knob;
	public Sprite crown;
	class SponsorScore
	{
		public Livery sponsor;
		public int score;
	}
	private void OnEnable()
	{
		Refresh();
	}
	private void Start()
	{
		ServerC.I.callbacks.PlayerJoined += Refresh;
		ServerC.I.callbacks.PlayerLeft += Refresh;
	}

	private void Refresh(List<int> list)
	{
		Refresh();
	}

	private void Refresh(List<LobbyPlayerJoined> list)
	{
		Refresh();
	}

	public void Refresh()
	{
		if(gameObject.activeInHierarchy)
			StartCoroutine(RefreshSeq());
	}
	IEnumerator RefreshSeq()
	{
		yield return null; // wait one frame before updating
		Player[] players = new Player[server.lobby.Players.Count];
		for (int i = 0; i < server.lobby.Players.Count; ++i)
			players[i] = server.lobby.Players[i];

		// Team scoring
		if (F.I.scoringType == ScoringType.Championship)
		{
			List<SponsorScore> teamScores = new();

			foreach (var p in server.lobby.Players)
			{

				var playerSponsor = p.SponsorGet();
				var playerScore = p.ScoreGet();

				var teamScore = teamScores.Find(s => playerSponsor == s.sponsor);

				if (teamScore == null)
				{
					teamScores.Add(new SponsorScore { sponsor = playerSponsor, score = playerScore });
				}
				else
				{
					teamScore.score += playerScore;
				}
			}
			teamScores.Sort((y, x) => x.score.CompareTo(y.score));

			Array.Sort(players, (Player p2, Player p1) =>
			{
				Livery p1Sponsor = p1.SponsorGet();
				Livery p2Sponsor = p2.SponsorGet();
				var teamScoreA = teamScores.Find(s => s.sponsor == p1Sponsor).score;
				var teamScoreB = teamScores.Find(s => s.sponsor == p2Sponsor).score;
				return teamScoreA.CompareTo(teamScoreB);
			});
		}
		else
		{ // Individual scoring
			Array.Sort(players, (Player a, Player b) => b.ScoreGet().CompareTo(a.ScoreGet()));
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

		string carNr = (player == ServerC.I.PlayerMe) ? F.I.s_playerCarName : player.carNameGet();
		string carName = F.I.Car(carNr).name;

		newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = F.I.randomCars ? "*random*" : carName;

		newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = ((F.I.scoringType == ScoringType.Championship) ? "$ " : "") + player.ScoreGet().ToString();
	}
	void DefaultView()
	{
		var newRow = Instantiate(LeaderboardRowPrefab, transform).transform;
		newRow.name = "Initializing";
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = knob;
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.magenta;
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.magenta;
		newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = "STARTING";
		newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = "SERVER";
	}
}

