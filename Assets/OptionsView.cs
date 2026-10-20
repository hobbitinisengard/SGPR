using UnityEngine.UI;

public class OptionsView : MainMenuView
{
    public Button remapEntryBtn;
    public MainMenuView remapView;
    public MainMenuView androidRemapView;
    public void OpenRemapView()
    {
        GoToView(MobileControls.Enabled ? androidRemapView : remapView);
    }
}
