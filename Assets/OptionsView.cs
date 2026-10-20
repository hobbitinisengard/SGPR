using UnityEngine.UI;

public class OptionsView : MainMenuView
{
  public Button remapEntryBtn;
	private void Start()
	{
		remapEntryBtn.interactable = !MobileControls.Enabled;
	}
}
