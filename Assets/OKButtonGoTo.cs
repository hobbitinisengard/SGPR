using UnityEngine;
using UnityEngine.UI;

public class OKButtonGoTo : MonoBehaviour
{
	public MainMenuView menuView;
	public MainMenuView onExhibitionGoToThis;
	public MainMenuView onMultiplayerGoToThis;
	public MainMenuView onSplitscreenGoToThis;
	public MainMenuView onArcadeGoToThis;
	private void OnEnable()
	{
		GetComponent<Button>().onClick.AddListener(GoToView);	
	}

	void GoToView()
	{
		switch (F.I.gameMode)
		{
			case GameMode.Exhibition:
				menuView.GoToView(onExhibitionGoToThis);
				break;
			case GameMode.Multiplayer:
				menuView.GoToView(onMultiplayerGoToThis);
				break;
			case GameMode.Splitscreen:
				menuView.GoToView(onSplitscreenGoToThis); 
				break;
			case GameMode.Arcade:
				menuView.GoToView(onArcadeGoToThis);
				break;
			default:
				break;
		}
	}
}
