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
		if (F.I.gameMode == GameMode.Splitscreen)
		{
			ResultsView.Clear();
			GoToView(SplitScreenSelector.I.thisView);
			return;
		}
		GoToView(rankingView);
	}
	protected override void OnEnable()
	{
		if (F.I.gameMode == GameMode.Splitscreen)
		{
			prevView = SplitScreenSelector.I.thisView;
			prevViewForbidden = false;
		}
		if (Championships.Active && Championships.Current.Completed)
		{
			prevView = rankingView;
			prevViewForbidden = false;
		}
		base.OnEnable();
	}
	public void PrepareUsingSplitScreen()
	{
		var players = SplitScreenSelector.I.SeriesStandings;
		int bestScore = players.Count > 0 ? players[0].score : 0;
		var winners = players.Where(player => player.score == bestScore).ToList();
		if (F.I.teams)
		{
			var teams = players.GroupBy(player => player.sponsor).ToList();
			int bestTeamScore = teams.Count > 0 ? teams.Max(team => team.Sum(player => player.score)) : 0;
			winners = teams.Where(team => team.Sum(player => player.score) == bestTeamScore).SelectMany(team => team).ToList();
		}
		description.text = string.Join(", ", winners.Select(player => player.name)) + " " +
			F.I.LocStr(winners.Count == 1 ? "WINS THE GAME!" : "WIN THE GAME!");
		result123Obj.gameObject.SetActive(false);
		succOverObj.gameObject.SetActive(true);
		bool localWinner = winners.Any(winner => F.I.splitScreenPlayers.Any(player => player.name == winner.name));
		succOverObj.sprite = succOverSprites[localWinner ? 1 : 0];
		music = localWinner ? goodendingClip : gameoverClip;
	}
	public void PrepareUsingChampionship()
	{
		rankingView = F.I.rankingView;
		rankingView.SetRankingType(ScoringType.Championship, false, GameMode.Championships);
		int position = Championships.Position;
		ChampionshipUI.SetText(description.gameObject,
			string.Format(F.I.LocStr("Champ.PodiumResult"), Championships.Current.driverName, position));
		succOverObj.gameObject.SetActive(false);
		result123Obj.gameObject.SetActive(true);
		result123Obj.sprite = result123Sprites[position - 1];
		music = goodendingClip;
	}
	public void PrepareUsingArcade(bool continuationCheck)
	{

		var players = ResultsView.SortedResultsByScore;
		rankingView.sortedResults = players;
		

		if (continuationCheck)
		{
			description.text = "";
			result123Obj.gameObject.SetActive(false);
			succOverObj.gameObject.SetActive(true);
			succOverObj.sprite = succOverSprites[1];
			music = goodendingClip;
		}
		else
		{
			description.text = "";
			result123Obj.gameObject.SetActive(false);
			succOverObj.gameObject.SetActive(true);
			succOverObj.sprite = succOverSprites[0];
			music = gameoverClip;
		}
	}
	public void PrepareViewUsingMultiplayer()
	{
		var players = ResultsView.SortedResultsByScore;
		rankingView.sortedResults = players;
		rankingView.SetRankingType(F.I.scoringType, F.I.teams, GameMode.Multiplayer);

		if (F.I.teams)
		{
			List<Result> winnerPlayers = new();
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
