using UnityEngine;

// The operation itself handles Escape, including when the Cancel action is disabled.
// This overlay only prevents the same press from leaving the remap menu.
public class RemapViewHack : MonoBehaviour
{
	public MainMenuView MainMenuView;
	bool previousForbidden;
	private void OnEnable()
	{
		if (!MainMenuView) return;
		previousForbidden = MainMenuView.prevViewForbidden;
		MainMenuView.prevViewForbidden = true;
	}
	private void OnDisable()
	{
		if (MainMenuView) MainMenuView.prevViewForbidden = previousForbidden;
	}
}
