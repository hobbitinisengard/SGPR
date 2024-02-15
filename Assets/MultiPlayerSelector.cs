using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MultiPlayerSelector : TrackSelector
{
	public Chat chat;
	public LeaderBoardTable leaderboard;
	public TextMeshProUGUI ipText;
	public TextMeshProUGUI sponsorbText;
	public TextMeshProUGUI randomCarsText;
	public TextMeshProUGUI readyText;
	public TextMeshProUGUI scoringText;

	private void OnEnable()
	{
		base.OnEnable();
		if (Info.onlinePlayers == null)
			Info.onlinePlayers = new();

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
		randomCarsText.text = "Track:" + (Info.randomTrack ? "Random" : "Select");
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

