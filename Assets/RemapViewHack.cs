using UnityEngine;
using UnityEngine.InputSystem;

public class RemapViewHack : MonoBehaviour
{
   public MainMenuView MainMenuView;
	MainMenuView prevView;
	// disable quitting remap menu when rebinding by removing prevView from MainMenu component
	private void OnEnable()
	{
		F.I.escRef.action.performed += EscWorks;
		prevView = MainMenuView.prevView;
		MainMenuView.prevView = null;
	}
	private void OnDisable()
	{
		F.I.escRef.action.performed -= EscWorks;
		MainMenuView.prevView = prevView;
	}
	void EscWorks(InputAction.CallbackContext ctx)
	{
		gameObject.SetActive(false);
	}
}
