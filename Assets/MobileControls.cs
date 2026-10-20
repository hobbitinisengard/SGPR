using System.Collections.Generic;
using RVP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public sealed class MobileControls : MonoBehaviour
{
    public static MobileControls I { get; private set; }
    public static bool Enabled => Application.platform == RuntimePlatform.Android;
    static float nextPeripheralCheck;
    static bool editorDevicesConnected;
    public static bool EditorDevicesConnected
    {
        get
        {
            if (!Enabled) return true;
            if (Time.realtimeSinceStartup < nextPeripheralCheck) return editorDevicesConnected;
            nextPeripheralCheck = Time.realtimeSinceStartup + .5f;
            bool keyboard = false, mouse = false;
            using (var inputDevices = new AndroidJavaClass("android.view.InputDevice"))
                foreach (int id in inputDevices.CallStatic<int[]>("getDeviceIds"))
                    using (var device = inputDevices.CallStatic<AndroidJavaObject>("getDevice", id))
                    {
                        if (device == null || device.Call<bool>("isVirtual")) continue;
                        keyboard |= device.Call<int>("getKeyboardType") == 2; // Alphabetic physical keyboard.
                        mouse |= (device.Call<int>("getSources") & 0x2002) == 0x2002; // SOURCE_MOUSE.
                    }
            editorDevicesConnected = keyboard && mouse;
            return editorDevicesConnected;
        }
    }
    const string LayoutKey = "AndroidControls.Layout.";
    const string NitroModeKey = "AndroidControls.ToggleNitro";
    bool nitroHeld, nitroLatched;
    public bool ToggleNitro { get; private set; }
    public bool Nitro => (ToggleNitro ? nitroLatched : nitroHeld) && gas && !brake;
    public bool EditingLayout { get; private set; }
    MainMenuView layoutView;
    sealed class LayoutControl
    {
        public MobileTouchButton button;
        public Vector2 defaultPosition;
    }
    readonly List<LayoutControl> layoutControls = new();
    GameObject layoutToolbar;
    Text nitroModeLabel, resetLayoutLabel, saveLayoutLabel, layoutHint;
    public bool Stunts { get; private set; }
    public bool Driving { get; private set; }
    bool left, right, gas, brake, menuLeft, menuRight, menuUp, menuDown, menuEnter, menuShift, menuCtrl, menuTab;
    int lastMenuMask = -1;
    public Vector2 Drive => new Vector2((right ? 1 : 0) - (left ? 1 : 0), (gas ? 1 : 0) - (brake ? 1 : 0));
    RectTransform safeRoot;
    GameObject raceButtons, menuButtons, raceEscape;
    MobileTouchButton nitroButton;
    Keyboard menuKeyboard;
    readonly List<Key> menuKeys = new();
    Rect lastSafeArea;
    Vector2Int lastScreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (Enabled && !I) new GameObject("Android touch controls").AddComponent<MobileControls>();
    }
    void Awake()
    {
        I = this;
        ToggleNitro = PlayerPrefs.GetInt(NitroModeKey, 0) != 0;
        DontDestroyOnLoad(gameObject);
        menuKeyboard = InputSystem.AddDevice<Keyboard>("MobileMenuKeyboard");
        var canvasObject = GameObject.Find("Canvas");
        var input = canvasObject ? canvasObject.GetComponent<PlayerInput>() : null;
        if (input && Touchscreen.current != null)
            input.SwitchCurrentControlScheme("Touch", Touchscreen.current, menuKeyboard);
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        safeRoot = Panel("Safe area", transform);
        raceButtons = Panel("Race controls", safeRoot).gameObject;
        menuButtons = Panel("Menu controls", safeRoot).gameObject;
        // Steering on the right; stunts/nitro on the left. Throttle/brake stay high on the right.
        var leftButton = Button(raceButtons.transform, "\u25C0", new Vector2(1, 0), new Vector2(-322.5f, 135), new Vector2(165, 145));
        leftButton.readScreenTouches = true;
        leftButton.highlightWhenHeld = true;
        leftButton.highlightColor = new Color(1f, .4f, .05f, .85f);
        leftButton.heldChanged = value => left = value;
        var rightButton = Button(raceButtons.transform, "\u25B6", new Vector2(1, 0), new Vector2(-157.5f, 135), new Vector2(165, 145));
        rightButton.readScreenTouches = true;
        rightButton.highlightWhenHeld = true;
        rightButton.highlightColor = new Color(1f, .4f, .05f, .85f);
        rightButton.heldChanged = value => right = value;
        Button(raceButtons.transform, "R", new Vector2(0, 1), new Vector2(160, -155), new Vector2(145, 110)).tapped = ResetPlayerOnTrack;
        var gasButton = Button(raceButtons.transform, "GAS", new Vector2(1, 1), new Vector2(-240, -145), new Vector2(330, 145));
        gasButton.highlightWhenHeld = true;
        gasButton.highlightColor = new Color(.1f, .75f, .2f, .85f);
        gasButton.heldChanged = value => { gas = value; RefreshNitroHighlight(); };
        gasButton.releaseOnExit = true;
        var brakeButton = Button(raceButtons.transform, "BRAKE", new Vector2(1, 1), new Vector2(-240, -310), new Vector2(330, 145));
        brakeButton.highlightWhenHeld = true;
        brakeButton.highlightColor = new Color(.9f, .1f, .1f, .85f);
        brakeButton.heldChanged = value => { brake = value; RefreshNitroHighlight(); };
        nitroButton = Button(raceButtons.transform, "NITRO", new Vector2(0, 0), new Vector2(240, 135), new Vector2(330, 145));
        nitroButton.highlightColor = new Color(.9f, .1f, .1f, .85f);
        nitroButton.heldChanged = value => { nitroHeld = value; RefreshNitroHighlight(); };
        nitroButton.tapped = () =>
        {
            if (ToggleNitro) { nitroLatched = !nitroLatched; RefreshNitroHighlight(); }
        };
        var stuntButton = Button(raceButtons.transform, "SHIFT", new Vector2(0, 0), new Vector2(240, 300), new Vector2(330, 145));
        stuntButton.highlightWhenHeld = true;
        stuntButton.highlightColor = new Color(1f, .4f, .05f, .85f);
        stuntButton.heldChanged = value => Stunts = value;
        var escape = Button(safeRoot, "ESC", new Vector2(0, 1), new Vector2(80, -48), new Vector2(140, 80));
        escape.tapped = Back;
        raceEscape = escape.gameObject;
        Button(raceButtons.transform, "CAM", new Vector2(0, 1), new Vector2(240, -48), new Vector2(140, 80)).tapped = () =>
        {
            if (RaceManager.I && RaceManager.I.cam) RaceManager.I.cam.CycleFollowCamera();
        };
        Button(menuButtons.transform, "\u25C0", new Vector2(0, 0), new Vector2(105, 205), new Vector2(140, 90)).heldChanged = value => menuLeft = value;
        Button(menuButtons.transform, "\u25B6", new Vector2(0, 0), new Vector2(270, 205), new Vector2(140, 90)).heldChanged = value => menuRight = value;
        Button(menuButtons.transform, "ENTER", new Vector2(1, 0), new Vector2(-105, 425), new Vector2(140, 90)).heldChanged = value => menuEnter = value;
        Button(menuButtons.transform, "\u25B2", new Vector2(1, 0), new Vector2(-105, 315), new Vector2(140, 90)).heldChanged = value => menuUp = value;
        Button(menuButtons.transform, "\u25BC", new Vector2(1, 0), new Vector2(-105, 205), new Vector2(140, 90)).heldChanged = value => menuDown = value;
        Button(menuButtons.transform, "ESC", new Vector2(1, 0), new Vector2(-105, 95), new Vector2(140, 90)).tapped = Back;
        var shift = Button(menuButtons.transform, "UI SHIFT", new Vector2(0, 0), new Vector2(105, 95), new Vector2(145, 85));
        var ctrl = Button(menuButtons.transform, "UI CTRL", new Vector2(0, 0), new Vector2(270, 95), new Vector2(145, 85));
        var tab = Button(menuButtons.transform, "UI TAB", new Vector2(0, 0), new Vector2(435, 95), new Vector2(145, 85));
        // Sticky modifiers allow selecting a modifier and tapping a menu option with one finger.
        shift.tapped = () => { menuShift = !menuShift; shift.SetHighlighted(menuShift); };
        ctrl.tapped = () => { menuCtrl = !menuCtrl; ctrl.SetHighlighted(menuCtrl); };
        tab.tapped = () => { menuTab = !menuTab; tab.SetHighlighted(menuTab); };
        CreateLayoutToolbar();
    }
    static RectTransform Panel(string label, Transform parent)
    {
        var rect = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    MobileTouchButton Button(Transform parent, string label, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(MobileTouchButton));
        var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
        var control = obj.GetComponent<MobileTouchButton>(); control.SetHighlighted(false);
        var text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(obj.transform, false);
        var textRect = (RectTransform)text.transform; textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter; text.text = label; text.raycastTarget = false;
        if (parent == raceButtons.transform || (parent == safeRoot && label == "ESC"))
            layoutControls.Add(new LayoutControl { button = control, defaultPosition = position });
        return control;
    }
    void Update()
    {
        if (!F.I || !F.I.viewSwitcher) return;
        var race = RaceManager.I;
        bool racing = F.I.viewSwitcher.world.activeInHierarchy && !F.I.s_inEditor && race && race.playerCar &&
            race.playerCar.raceBox && race.playerCar.raceBox.enabled && !race.resultsSeq.gameObject.activeInHierarchy;
        bool driving = racing && !F.I.gamePaused && !EditingLayout;
        if (Driving != driving)
        {
            Driving = driving;
            nitroHeld = nitroLatched = false; left = right = gas = brake = Stunts = false;
            nitroButton.SetHighlighted(false);
        }
        raceButtons.SetActive(driving || EditingLayout);
        menuButtons.SetActive(!EditingLayout && !racing && F.I.viewSwitcher.menu.activeInHierarchy && !TouchScreenKeyboard.visible);
        raceEscape.SetActive(EditingLayout || (!menuButtons.activeSelf && !TouchScreenKeyboard.visible));
        if (EventSystem.current) EventSystem.current.sendNavigationEvents =
            (menuButtons.activeInHierarchy && menuEnter) || (F.I.s_inEditor && F.I.viewSwitcher.world.activeInHierarchy);
        // Keyboard input is only synthesized for menu controls, never for the race or editor.
        menuKeys.Clear();
        if (menuButtons.activeInHierarchy)
        {
            if (menuLeft) menuKeys.Add(Key.A);
            if (menuRight) menuKeys.Add(Key.D);
            if (menuUp) menuKeys.Add(Key.W);
            if (menuDown) menuKeys.Add(Key.S);
            if (menuEnter) menuKeys.Add(Key.Enter);
            if (menuShift) menuKeys.Add(Key.RightShift);
            if (menuCtrl) menuKeys.Add(Key.LeftCtrl);
            if (menuTab) menuKeys.Add(Key.Tab);
        }
        int mask = 0;
        foreach (var key in menuKeys) mask |= key == Key.A ? 1 : key == Key.D ? 2 : key == Key.W ? 4 : key == Key.S ? 8 : key == Key.RightShift ? 16 : key == Key.LeftCtrl ? 32 : key == Key.Tab ? 64 : 128;
        if (mask != lastMenuMask)
        {
            lastMenuMask = mask;
            InputSystem.QueueStateEvent(menuKeyboard, new KeyboardState(menuKeys.ToArray()));
        }
        if (Screen.safeArea != lastSafeArea || lastScreen != new Vector2Int(Screen.width, Screen.height))
        {
            lastSafeArea = Screen.safeArea; lastScreen = new Vector2Int(Screen.width, Screen.height);
            safeRoot.anchorMin = lastSafeArea.min / new Vector2(Screen.width, Screen.height);
            safeRoot.anchorMax = lastSafeArea.max / new Vector2(Screen.width, Screen.height);
            Canvas.ForceUpdateCanvases();
            ApplySavedLayout();
        }
    }
    void CreateLayoutToolbar()
    {
        layoutToolbar = Panel("Layout options", safeRoot).gameObject;
        var nitroMode = Button(layoutToolbar.transform, "Nitro mode", new Vector2(.5f, 1), new Vector2(-215, -65), new Vector2(340, 90));
        nitroModeLabel = nitroMode.GetComponentInChildren<Text>();
        nitroMode.tapped = () => SetToggleNitro(!ToggleNitro);
        var reset = Button(layoutToolbar.transform, "Reset layout", new Vector2(.5f, 1), new Vector2(155, -65), new Vector2(340, 90));
        resetLayoutLabel = reset.GetComponentInChildren<Text>();
        reset.tapped = ResetLayout;
        var done = Button(layoutToolbar.transform, "Save layout", new Vector2(.5f, 0), new Vector2(0, 80), new Vector2(280, 90));
        saveLayoutLabel = done.GetComponentInChildren<Text>();
        done.tapped = () => { if (layoutView) layoutView.GoBack(); };
        layoutHint = new GameObject("Layout hint", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        layoutHint.transform.SetParent(layoutToolbar.transform, false);
        var hintRect = (RectTransform)layoutHint.transform;
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(.5f, .5f);
        hintRect.sizeDelta = new Vector2(680, 140);
        layoutHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        layoutHint.fontSize = 30; layoutHint.alignment = TextAnchor.MiddleCenter; layoutHint.raycastTarget = false;
        layoutToolbar.SetActive(false);
    }
    public void BeginLayoutEditing(MainMenuView view)
    {
        if (EditingLayout || !Enabled) return;
        layoutView = view;
        EditingLayout = true;
        nitroHeld = nitroLatched = false;
        foreach (var item in layoutControls) item.button.SetLayoutEditing(true);
        RefreshNitroHighlight();
        UpdateLayoutLabels();
        layoutToolbar.SetActive(true);
    }
    public void EndLayoutEditing()
    {
        if (!EditingLayout) return;
        SaveLayout();
        EditingLayout = false;
        layoutView = null;
        foreach (var item in layoutControls) item.button.SetLayoutEditing(false);
        layoutToolbar.SetActive(false);
        nitroHeld = nitroLatched = false;
    }
    public void SetToggleNitro(bool value)
    {
        ToggleNitro = value;
        nitroHeld = nitroLatched = false;
        PlayerPrefs.SetInt(NitroModeKey, value ? 1 : 0);
        PlayerPrefs.Save();
        RefreshNitroHighlight();
        UpdateLayoutLabels();
    }
    void UpdateLayoutLabels()
    {
        if (!F.I || !nitroModeLabel) return;
        nitroModeLabel.text = F.I.LocStr(ToggleNitro ? "Android.NitroToggle" : "Android.NitroHold");
        resetLayoutLabel.text = F.I.LocStr("Android.ResetLayout");
        saveLayoutLabel.text = F.I.LocStr("Android.SaveLayout");
        layoutHint.text = F.I.LocStr("Android.LayoutHint");
    }
    void SaveLayout()
    {
        foreach (var item in layoutControls)
        {
            var rect = (RectTransform)item.button.transform;
            var area = ((RectTransform)rect.parent).rect;
            if (area.width <= 0 || area.height <= 0) continue;
            Vector2 position = rect.localPosition;
            string key = LayoutKey + item.button.name;
            PlayerPrefs.SetFloat(key + ".x", (position.x - area.xMin) / area.width);
            PlayerPrefs.SetFloat(key + ".y", (position.y - area.yMin) / area.height);
        }
        PlayerPrefs.Save();
    }
    void ApplySavedLayout()
    {
        foreach (var item in layoutControls)
        {
            string key = LayoutKey + item.button.name;
            if (!PlayerPrefs.HasKey(key + ".x") || !PlayerPrefs.HasKey(key + ".y")) continue;
            var rect = (RectTransform)item.button.transform;
            var area = ((RectTransform)rect.parent).rect;
            Vector2 target = new Vector2(
                area.xMin + Mathf.Clamp01(PlayerPrefs.GetFloat(key + ".x")) * area.width,
                area.yMin + Mathf.Clamp01(PlayerPrefs.GetFloat(key + ".y")) * area.height);
            rect.anchoredPosition += target - (Vector2)rect.localPosition;
            item.button.ClampToParent();
        }
    }
    void ResetLayout()
    {
        foreach (var item in layoutControls)
        {
            PlayerPrefs.DeleteKey(LayoutKey + item.button.name + ".x");
            PlayerPrefs.DeleteKey(LayoutKey + item.button.name + ".y");
            ((RectTransform)item.button.transform).anchoredPosition = item.defaultPosition;
        }
        PlayerPrefs.Save();
    }
    void RefreshNitroHighlight()
    {
        if (nitroButton) nitroButton.SetHighlighted(Nitro);
    }
    void ResetPlayerOnTrack()
    {
        var player = RaceManager.I ? RaceManager.I.playerCar : null;
        if (Driving && player && player.Owner && !player.followAI.selfDriving &&
            !F.I.chat.texting && Time.time - player.resetOnTrackTime > 5)
            player.ResetOnTrack();
    }
    void Back()
    {
        if (TouchScreenKeyboard.visible) return;
        var dialog = FindFirstObjectByType<YouSureDialog>();
        if (dialog) { dialog.HidePanel(); return; }
        if (F.I && F.I.viewSwitcher && F.I.viewSwitcher.world.activeInHierarchy && RaceManager.I && !F.I.s_inEditor)
        { RaceManager.I.hud.TogglePause(); return; }
        if (!F.I || !F.I.viewSwitcher) return;
        var views = F.I.viewSwitcher.menu.GetComponentsInChildren<MainMenuView>();
        if (views.Length > 0) views[views.Length - 1].GoBack();
    }
    void OnApplicationFocus(bool focus)
    {
        if (focus) return;
        if (EditingLayout) SaveLayout();
        left = right = gas = brake = Stunts = nitroHeld = nitroLatched = menuLeft = menuRight = menuUp = menuDown = menuEnter = false;
        if (nitroButton) nitroButton.SetHighlighted(false);
        lastMenuMask = -1;
        if (menuKeyboard != null) InputSystem.ResetDevice(menuKeyboard);
    }
    void OnDestroy()
    {
        if (menuKeyboard != null && menuKeyboard.added) InputSystem.RemoveDevice(menuKeyboard);
        if (I == this) I = null;
    }
}
