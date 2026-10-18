using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
public class LeaderBoardTable : MonoBehaviour
{
	public GameObject LeaderboardRowPrefab;
	public Sprite knob;
	public Sprite crown;
	sealed class LocalRow
	{
		public Transform root;
		public TMP_Text driver, car, score;
		public int playerIndex;
	}
	readonly List<LocalRow> localRows = new();
	GameObject localRowPrefab;
	public void RefreshSplitScreen(GameObject prefab, int activePlayer)
	{
		if (!prefab) return;
		if (localRowPrefab != prefab)
		{
			// Preserve the title row, matching the existing multiplayer table layout.
			for (int i = 1; i < transform.childCount; i++)
			{ transform.GetChild(i).gameObject.SetActive(false); Destroy(transform.GetChild(i).gameObject); }
			localRows.Clear(); localRowPrefab = prefab;
		}
		var players = F.I.splitScreenPlayers;
		bool championship = F.I.scoringType == ScoringType.Championship;
		var teamScores = F.I.teams ? players.Select(player => player.sponsor).Distinct()
			.ToDictionary(sponsor => sponsor, sponsor => SplitScreenSelector.I.TeamScore(sponsor)) : null;
		var order = Enumerable.Range(0, players.Count)
			.OrderByDescending(index => F.I.teams ? teamScores[players[index].sponsor] : championship ? players[index].Funds : players[index].score)
			.ThenBy(index => F.I.teams ? (int)players[index].sponsor : 0).ThenBy(index => index).ToArray();
		while (localRows.Count < players.Count)
		{
			var row = Instantiate(prefab, transform).transform;
			localRows.Add(new LocalRow { root = row, driver = row.GetChild(0).GetComponent<TMP_Text>(),
				car = row.GetChild(1).GetComponent<TMP_Text>(), score = row.GetChild(2).GetComponent<TMP_Text>() });
		}
		for (int i = 0; i < localRows.Count; i++)
		{
			var row = localRows[i]; row.root.gameObject.SetActive(i < players.Count);
			if (i >= players.Count) continue;
			int index = order[i]; var player = players[index]; row.playerIndex = index;
			row.root.name = player.name; row.root.SetSiblingIndex(i + 1);
			row.driver.text = (index + 1) + "P  " + player.name;
			row.car.text = F.I.randomCars ? F.I.LocStr("Random") : player.car >= 0 && player.car < F.I.cars.Length ? F.I.LocStr(F.I.cars[player.car].name) : "-";
			if (F.I.teams) row.car.text += " / " + F.I.LocStr(player.sponsor.ToString());
			float score = F.I.teams ? teamScores[player.sponsor] : championship ? player.Funds : player.score;
			row.score.text = (championship ? "$ " : "") + score.ToString("N0");
		}
		HighlightSplitScreenPlayer(activePlayer);
	}
	public void HighlightSplitScreenPlayer(int index)
	{
		foreach (var row in localRows)
		{
			Color color = row.playerIndex == index ? Color.white : new Color(.7f, .7f, .7f, 1);
			row.driver.color = row.car.color = row.score.color = color;
		}
	}
	
	private void OnEnable()
	{
		Refresh();
	}
	private void Start()
	{
		if (F.I.gameMode == GameMode.Splitscreen) return;
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
		if (F.I.gameMode == GameMode.Splitscreen)
		{
			var selector = SplitScreenSelector.I;
			if (selector && selector.leaderboard == this) RefreshSplitScreen(selector.ShortLeaderboardRowPrefab, selector.ActiveMenuPlayerIndex);
			return;
		}
		localRows.Clear(); localRowPrefab = null;
		var players = ServerC.I.ScoreSortedPlayers;

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
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = (player.Id == ServerC.I.lobby.HostId) ? crown : knob;
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = player.ReadyGet() ? Color.green : Color.yellow;
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = player.NameGet();
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().color = player.ReadColor();

		string carName = (player == ServerC.I.PlayerMe) ? F.I.cars[F.I.s_playerCarIdx].name : F.I.Car(player.carNameGet()).name;

		newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = F.I.randomCars ? F.I.LocStr("Random") : F.I.LocStr(carName);

		newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = (F.I.scoringType == ScoringType.Championship ? "$ " : "") + player.ScoreGet().ToString();
	}
	//void DefaultView()
	//{
	//	var newRow = Instantiate(LeaderboardRowPrefab, transform).transform;
	//	newRow.name = "Initializing";
	//	newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = knob;
	//	newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.magenta;
	//	newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
	//	newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.magenta;
	//	newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = "STARTING";
	//	newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = "SERVER";
	//}
}

