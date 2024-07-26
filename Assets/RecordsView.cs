
public class RecordsView : MainMenuView
{
	public MainMenuView rankingView;
	protected override void OnEnable()
	{
		base.OnEnable();
		ResultsView.Clear();
	}
	public void GoToRanking(RankingType rankingType)
	{
		F.I.teams = rankingType.teams;
		F.I.scoringType = rankingType.scoringType;
		GoToView(rankingView);
	}
}
