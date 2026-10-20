public sealed class RemapAndroidView : MainMenuView
{
    protected override void OnEnable()
    {
        base.OnEnable();
        if (MobileControls.Enabled && MobileControls.I) MobileControls.I.BeginLayoutEditing(this);
    }
    protected override void OnDisable()
    {
        if (MobileControls.I) MobileControls.I.EndLayoutEditing();
        base.OnDisable();
    }
}
