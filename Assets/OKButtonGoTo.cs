using UnityEngine;
using UnityEngine.UI;

public class OKButtonGoTo : MonoBehaviour
{
	public MainMenuView menuView;
	public MainMenuView onExhibitionGoToThis;
	public MainMenuView onMultiplayerGoToThis;
	private void OnEnable()
	{
		GetComponent<Button>().onClick.AddListener(GoToView);	
	}

	void GoToView()
	{
		switch (F.I.gameMode)
		{
			case MultiMode.Singleplayer:
				menuView.GoToView(onExhibitionGoToThis);
				break;
			case MultiMode.Multiplayer:
				menuView.GoToView(onMultiplayerGoToThis);
				break;
			default:
				break;
		}
	}
}
