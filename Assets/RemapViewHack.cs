using UnityEngine;
using UnityEngine.InputSystem;

public class RemapViewHack : MonoBehaviour
{
   public MainMenuView MainMenuView;
	public InputActionReference cancelReference;
	MainMenuView prevView;
	// disable quitting remap menu when rebinding by removing prevView from MainMenu component
	private void OnEnable()
	{
		cancelReference.action.performed += EscWorks;
		prevView = MainMenuView.prevView;
		MainMenuView.prevView = null;
	}
	private void OnDisable()
	{
		cancelReference.action.performed -= EscWorks;
		MainMenuView.prevView = prevView;
	}
	void EscWorks(InputAction.CallbackContext ctx)
	{
		gameObject.SetActive(false);
	}
}
