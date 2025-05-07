using System.Collections.Generic;
using System.Linq;
using TMPro;
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

		var players = ResultsView.SortedResultsByScore;
		rankingView.sortedResults = players;

		if (F.I.teams)
		{
			List<ResultInfo> winnerPlayers = new();
			Livery winningTeam = players[0].sponsor;
			foreach (var p in players)
			{
				if (p.sponsor == winningTeam)
				{
					winnerPlayers.Add(p);
				}
			}

			description.text = "";
			for (int i = 0; i < winnerPlayers.Count; ++i)
			{
				description.text += winnerPlayers[i].name;
				if ((i + 1) < winnerPlayers.Count)
					description.text += ", ";
			}
			description.text += " " + ((winnerPlayers.Count == 1) ? F.I.LocStr("WINS THE GAME!") : F.I.LocStr("WIN THE GAME!"));

			result123Obj.gameObject.SetActive(false);
			succOverObj.gameObject.SetActive(true);
			succOverObj.sprite = succOverSprites[winnerPlayers.Count(winner => winner.id == ServerC.I.networkManager.LocalClientId)];
		}
		else
		{
			if (players.Count < 4)
			{
				description.text = players[0].name + " " + F.I.LocStr("WINS THE GAME!");
				result123Obj.gameObject.SetActive(false);
				succOverObj.gameObject.SetActive(true);
				succOverObj.sprite = succOverSprites[(players[0].id == ServerC.I.networkManager.LocalClientId) ? 1 : 0];
			}
			else
			{
				description.text = players[0].name + " " + F.I.LocStr("WINS THE GAME!");
				int pos = players.FindIndex(p => p.id == ServerC.I.networkManager.LocalClientId);
				if (pos <= 2)
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
