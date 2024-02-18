using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class MultiPlayerSelector : TrackSelector
{
	
	public Chat chat;
	public LeaderBoardTable leaderboard;
	public TextMeshProUGUI ipText;
	public TextMeshProUGUI sponsorbText;
	public TextMeshProUGUI randomCarsText;
	public TextMeshProUGUI randomTracksText;
	public TextMeshProUGUI readyText;
	public TextMeshProUGUI scoringText;

	

	private void OnEnable()
	{
		base.OnEnable();
		if (Info.onlinePlayers == null)
			Info.onlinePlayers = new();

		sortButton.gameObject.SetActive(Info.serverSide == ServerSide.Host);
		SwitchReady(true);
		bool isHost = Info.serverSide == ServerSide.Host;
		scoringText.transform.parent.GetComponent<Button>().interactable = isHost;
		sponsorbText.transform.parent.gameObject.SetActive(Info.scoringType == ScoringType.Championship);
		randomCarsText.transform.parent.GetComponent<Button>().interactable = isHost;
		randomTracksText.transform.parent.GetComponent<Button>().interactable = isHost;
		raceTypeButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		lapsButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		nightButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		CPULevelButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		rivalsButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		wayButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		catchupButtonText.transform.parent.GetComponent<Button>().interactable = isHost;
		SwitchScoring(true);
	}
	void Awake()
	{
		ipText.text = Info.playerData.ipAddress + Info.playerData.portNumber.ToString();
	}
	public void SwitchSponsor(bool init)
	{
		int dir = 0;
		if (!init)
			dir = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;

		Info.onlinePlayers[Info.myId].sponsor = (Livery)Wraparound((int)Info.onlinePlayers[Info.myId].sponsor + dir, 2, 7);
		sponsorbText.text = "Sponsor:" + Info.onlinePlayers[Info.myId].sponsor.ToString();
	}
	public void SwitchRandomCar()
	{
		Info.randomCars = !Info.randomCars;
		randomCarsText.text = "Car:" + (Info.randomCars ? "Random" : "Select");
	}
	public void SwitchRandomTrack()
	{
		Info.randomTrack = !Info.randomTrack;
		randomTracksText.text = "Track:" + (Info.randomTrack ? "Random" : "Select");
	}
	public void SwitchReady(bool init)
	{
		if(!init)
		{
			var me = Info.onlinePlayers.First(p => p.Id == Info.myId);
			me.ready = !me.ready;
		}
		readyText.text = (Info.serverSide == ServerSide.Client) ? "READY" : "HOST READY";
	}
	public void SwitchScoring(bool init)
	{
		int dir = 0;
		if (!init)
			dir = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;

		Info.scoringType = (ScoringType)Wraparound((int)Info.scoringType + dir, 0, 7);
		scoringText.text = Info.scoringType.ToString();
	}
}

