using UnityEngine;

public class PreSplitScreenView : MonoBehaviour
{
	private void OnEnable()
	{
		F.I.gameMode = GameMode.Splitscreen;
		F.I.scoringType = ScoringType.Points;
		F.I.splitScreenSelector.SwitchSplitScreenPlayers(true);
		F.I.splitScreenSelector.BindUI();
		F.I.splitScreenSelector.Refresh();
	}
}
