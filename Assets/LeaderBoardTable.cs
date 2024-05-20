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
	public GameObject LeaderboardRowPrefab;
	public Sprite knob;
	public Sprite crown;
	
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

		string carNr = (player == ServerC.I.PlayerMe) ? F.I.s_playerCarName : player.carNameGet();
		string carName = F.I.Car(carNr).name;

		newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = F.I.randomCars ? "*random*" : carName;

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

