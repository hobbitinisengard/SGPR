using JetBrains.Annotations;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardTable : MonoBehaviour
{
	public GameObject LeaderboardRowPrefab;
	public Sprite knob;
	public Sprite crown;
	float timer = 2;
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
			foreach (var player in Info.onlinePlayers)
			{
				var standing = scores.Find(s => s.sponsor == player.sponsor);
				if (standing == null)
				{
					scores.Add(new SponsorScore { sponsor = player.sponsor, score = player.score });
				}
				else
				{
					standing.score += player.score;
				}
			}
			scores.Sort((y, x) => x.score.CompareTo(y.score));
			Info.onlinePlayers.Sort((b, a) =>
			{
				var teamScoreA = scores.Find(s => s.sponsor == a.sponsor).score;
				var teamScoreB = scores.Find(s => s.sponsor == b.sponsor).score;
				return teamScoreA.CompareTo(teamScoreB);
			});
		}
		else
		{ // Individual scoring
			Info.onlinePlayers.Sort((b, a) => b.score.CompareTo(a.score));
		}
	}
	public void Refresh()
	{
		// TITLE + PLAYERS
		for(int i=1; i<transform.childCount; ++i)
		{
			Destroy(transform.GetChild(i).gameObject);
		}
		foreach (var player in Info.onlinePlayers)
		{
			Add(player);
		}
	}
	public void Update()
	{
		if(timer == 0)
		{
			Refresh();
			timer = 2;
		}
		timer -= Time.deltaTime;
	}
	void Add(OnlinePlayer player)
	{
		var newRow = Instantiate(LeaderboardRowPrefab, transform).transform;
		newRow.name = player.name;
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = player.ready ? Color.green : Color.red;
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = (player.Id == Info.hostId) ? crown : knob;
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = player.name;
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().color = player.nickColor;
		newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = player.carName;
		newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = player.score.ToString();
		newRow.GetChild(3).GetComponent<TextMeshProUGUI>().text = player.won.ToString();
	}
}

