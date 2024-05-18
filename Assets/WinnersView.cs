using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class WinnersView : MainMenuView
{
	public RankingView rankingView;
   public Image result123Obj;
   public Sprite[] result123Sprites;
	/// <summary>
	/// 0 = gameover, 1= success
	/// </summary>
   public Sprite[] succOverSprites;
	public AudioClip gameoverClip;
	public AudioClip goodendingClip;
   public Image succOverObj;
	public TextMeshProUGUI description;
	public void OKButton()
	{
		GoToView(rankingView);
	}
	public void PrepareView()
	{
		var players = ServerC.I.scoreSortedPlayers;

		//foreach(var p in players) // DEBUG
		//{
		//	Debug.Log(string.Format("{0}, {1}, {2}", p.NameGet(), p.ScoreGet(), p.SponsorGet()));
		//}

		if (F.I.teams) 
		{
			List<Player> winnerPlayers = new();
			Livery winningTeam = players[0].SponsorGet();
			foreach(var p in players)
			{
				if(p.SponsorGet() == winningTeam)
				{
					winnerPlayers.Add(p);
				}
			}

			description.text = "";
			string joinStr = winnerPlayers.Count == 2 ? " & " : ", ";
			for(int i=0; i<winnerPlayers.Count; ++i)
			{
				description.text += winnerPlayers[i];
				if ((i + 1) < winnerPlayers.Count)
					description.text += joinStr;
			}
			description.text += $" WIN{((winnerPlayers.Count == 1) ? "S" : "")} THE GAME!";

			result123Obj.gameObject.SetActive(false);
			succOverObj.gameObject.SetActive(true);
			succOverObj.sprite = succOverSprites[winnerPlayers.Count(winner => winner.Id == AuthenticationService.Instance.PlayerId)]; 
		}
		else
		{
			if(players.Length < 4)
			{
				description.text = players[0].NameGet() + " WINS THE GAME!";
				result123Obj.gameObject.SetActive(false);
				succOverObj.gameObject.SetActive(true);
				succOverObj.sprite = succOverSprites[(players[0].Id == AuthenticationService.Instance.PlayerId) ? 1 : 0];
			}
			else
			{
				description.text = players[0].NameGet() + " WINS THE GAME!";
				int pos = Array.FindIndex(players, 0, p => p.Id == AuthenticationService.Instance.PlayerId);
				if(pos <= 2)
				{
					succOverObj.gameObject.SetActive(false);
					result123Obj.gameObject.SetActive(true);
					result123Obj.sprite = result123Sprites[pos];
				}
				else
				{
					succOverObj.gameObject.SetActive(true);
					result123Obj.gameObject.SetActive(false);
					succOverObj.sprite = succOverSprites[0];
				}
			}
		}
		if (succOverObj.gameObject.activeSelf && succOverObj.sprite == succOverSprites[0])
			music = gameoverClip;
		else
			music = goodendingClip;
	}
	
}
