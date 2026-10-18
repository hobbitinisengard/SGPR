using UnityEngine;

public class PreSplitScreenView : MonoBehaviour
{
	private void Start()
	{
		F.I.gameMode = GameMode.Splitscreen;
		F.I.splitScreenSelector.SwitchSplitScreenPlayers(true);
		F.I.splitScreenSelector.BindUI();
		F.I.splitScreenSelector.Refresh();
	}
}
