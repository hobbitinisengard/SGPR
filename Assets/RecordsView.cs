
public class RecordsView : MainMenuView
{
	public MainMenuView rankingView;
	protected override void OnEnable()
	{
		base.OnEnable();
		ResultsView.Clear();
	}
}
