using UnityEngine;
using UnityEngine.UI;

// Input is captured by the rebind operation; cancellation uses CancelRebindBtn.
// Keep menu navigation blocked while the overlay is visible.
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
