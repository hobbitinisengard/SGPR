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
			case GameMode.Exhibition:
				menuView.GoToView(onExhibitionGoToThis);
				break;
			case GameMode.Multiplayer:
				menuView.GoToView(onMultiplayerGoToThis);
				break;
			default:
				break;
		}
	}
}
