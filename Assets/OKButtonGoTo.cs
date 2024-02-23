using UnityEngine;
using UnityEngine.UI;

public class OKButtonGoTo : MonoBehaviour
{
	public MainMenuView menuView;
	public GameObject onExhibitionGoToThis;
	public GameObject onMultiplayerGoToThis;
	private void OnEnable()
	{
		GetComponent<Button>().onClick.AddListener(GoToView);	
	}

	void GoToView()
	{
		switch (Info.gameMode)
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
