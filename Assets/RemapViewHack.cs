using UnityEngine;

public class RemapViewHack : MonoBehaviour
{
   public MainMenuView RemapMenuView;

	GameObject prevView;
	// disable quitting remap menu when rebinding by removing prevView from MainMenu component
	private void OnEnable()
	{
		prevView = RemapMenuView.prevView;
		RemapMenuView.prevView = null;
	}
	private void OnDisable()
	{
		RemapMenuView.prevView = prevView;
	}
}
