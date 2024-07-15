
public class RecordsView : MainMenuView
{
	public MainMenuView rankingView;
	public void GoToRanking(RankingType rankingType)
	{
		F.I.teams = rankingType.teams;
		F.I.scoringType = rankingType.scoringType;
		GoToView(rankingView);
	}
}
