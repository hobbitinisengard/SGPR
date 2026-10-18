using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class SplitScreenPlayerInfo
{
	public string name;
	public float score;
	public int cash = -1;
	public int Funds => cash >= 0 ? cash : Mathf.Max(0, Mathf.RoundToInt(score));
	public int car;
	public Livery sponsor = Livery.TGR;
	public int deviceId = -1;
	public int keyboardProfile;
}

[Serializable]
public class SplitScreenPlayerUI
{
	public GameObject root;
	public TMP_InputField nameInput;
	public TMP_Text scoreText, carText, controllerText, keyboardProfileText;
	public Button carButton;
}

// All UI objects are supplied by the scene. This component only binds and updates them.
public class SplitScreenSelector : TrackSelector
{
	public static SplitScreenSelector I;
	public GameObject ShortLeaderboardRowPrefab;
	public Image Icon1P2P;
	public Sprite[] PlayerIcons1P2P;
	[NonSerialized]
	public MainMenuView thisView;
	public Button splitScreenEntryBtn;
	public MainMenuView enterNameView;
	public EnterNameInputField registrationNameInput;
	public MainMenuView preSplitScreenView;
	public bool RegistrationActive { get; private set; }
	public bool RegistrationNavigationAllowed { get; private set; }
	public int RegistrationPlayerIndex => registrationPlayer;
	public bool IgnoreNameTypingThisFrame => RegistrationActive && registrationFrame == Time.frameCount;
	int registrationPlayer, registeredSet = -1, registeredDevice = -1, registrationFrame;
	bool registrationFinishing;
	float nextRegistrationTime;
	readonly InputActionAsset[] registrationSets = new InputActionAsset[SplitScreenRace.MaxPlayers];
	readonly string[] registrationDrafts = new string[SplitScreenRace.MaxPlayers];
	public bool IsRegistrationView(MainMenuView view) => RegistrationActive && view == enterNameView;
	public LeaderBoardTable leaderboard;
	public TextMeshProUGUI splitScreenPlayersNumberText, randomCarsText, randomTracksText, scoringText, roundText;
	public Button garageBtn;
	public MainMenuView garageView;
	public CarSelector carSelector;
	public int ActiveMenuPlayerIndex { get; private set; }
	public int GaragePlayerIndex { get; private set; } = -1;
	public bool GarageReturnAllowed { get; private set; }
	public bool GarageSubmitAllowed => lastMenuSubmitFrame == Time.frameCount && ActiveMenuPlayerIndex == GaragePlayerIndex;
	public SplitScreenPlayerInfo GaragePlayer => ValidPlayer(GaragePlayerIndex) ? F.I.splitScreenPlayers[GaragePlayerIndex] : null;
	readonly List<InputActionAsset> menuInputSets = new();
	readonly Dictionary<int, InputActionAsset> menuInputByPlayer = new();
	int pendingActivePlayer = -1;
	int lastMenuSubmitFrame = -1;
	bool menuInputConnected;

	[Header("Existing player rows (slot 0 to 9)")]
	public SplitScreenPlayerUI[] playerRows = new SplitScreenPlayerUI[SplitScreenRace.MaxPlayers];
	public Button playersButton, raceButton, lapsButton, raceTypeButton, rivalsButton, timeOfDayButton;
	public Button sponsorBtn;
	public Button teamsBtn;
	public TMP_Text teamsText;
	public Button randomCarsButton, randomTracksButton, scoringButton, cpuLevelButton, catchupButton, roadTypeButton;
	public TMP_Text roadTypeText;

	readonly List<Action> unbindUI = new();
	bool uiBound;
	bool raceReady;
	public int CurrentRound { get; private set; } = 1;
	public int TotalRounds => Mathf.Max(1, (int)F.I.Rounds);
	public bool SeriesCompleted => CurrentRound > TotalRounds;
	readonly Dictionary<string, Result> seriesResults = new();
	public List<Result> SeriesStandings => seriesResults.Values
		.OrderByDescending(result => F.I.teams ? TeamScore(result.sponsor) : result.score)
		.ThenBy(result => F.I.teams ? (int)result.sponsor : 0).ThenByDescending(result => result.score).ToList();
	public float TeamScore(Livery sponsor) => F.I.splitScreenPlayers.Where(player => player.sponsor == sponsor).Sum(player => player.score) +
		seriesResults.Values.Where(result => result.sponsor == sponsor && !F.I.splitScreenPlayers.Any(player => player.name == result.name)).Sum(result => result.score);
	public Livery CpuSponsor(int index)
	{
		var sponsors = F.I.splitScreenPlayers.Select(player => player.sponsor).Where(sponsor => sponsor != Livery.Random).Distinct().ToList();
		if (sponsors.Count == 0) sponsors.Add(Livery.TGR);
		if (sponsors.Count == 1) sponsors.Add((Livery)(1 + (int)sponsors[0] % F.I.Liveries));
		return sponsors[(index + 1) % sponsors.Count];
	}

	

	string L(string key) => F.I.LocStr(key);
	static void SetText(TMP_Text text, string value) { if (text) text.text = value; }
	protected override void Awake() { base.Awake(); I = this; thisView = GetComponent<MainMenuView>(); }
	protected override void OnEnable()
	{
		F.I.gameMode = GameMode.Splitscreen; F.I.s_spectator = false;
		if (F.I.splitScreenPlayers.Count > SplitScreenRace.MaxPlayers)
			F.I.splitScreenPlayers.RemoveRange(SplitScreenRace.MaxPlayers, F.I.splitScreenPlayers.Count - SplitScreenRace.MaxPlayers);
		F.I.s_cpuRivals = Mathf.Clamp(F.I.s_cpuRivals, 0, F.I.maxCarsInRace - F.I.splitScreenPlayers.Count);
		var used = new HashSet<string>();
		foreach (var player in F.I.splitScreenPlayers)
			if (InputSystem.GetDeviceById(player.deviceId) is Keyboard)
			{
				if (player.keyboardProfile < 0 || player.keyboardProfile >= SplitScreenRace.MaxPlayers || used.Contains(player.deviceId + ":" + player.keyboardProfile))
					player.keyboardProfile = Enumerable.Range(0, SplitScreenRace.MaxPlayers).First(profile => !used.Contains(player.deviceId + ":" + profile));
				used.Add(player.deviceId + ":" + player.keyboardProfile);
			}
		GaragePlayerIndex = -1;
		StartMenuInput();
		ShowActivePlayerIcon();
		BindUI();
		base.OnEnable();
		Refresh();
		InputSystem.onDeviceChange += DeviceChanged;
	}
	protected override void OnDisable()
	{
		base.OnDisable();
		if (GaragePlayerIndex < 0)
		{
			StopMenuInput();
			if (Icon1P2P) Icon1P2P.gameObject.SetActive(false);
		}
		InputSystem.onDeviceChange -= DeviceChanged;
	}
	void OnDestroy()
	{
		UnbindButtons();
		EndRegistration(); StopMenuInput();
		if (I == this) I = null;
	}
	public bool BeginRegistration(MainMenuView from)
	{
		if (F.I.gameMode != GameMode.Splitscreen) return false;
		if (!enterNameView || F.I.splitScreenPlayers.Count < 2 || F.I.splitScreenPlayers.Count > SplitScreenRace.MaxPlayers) return false;
		EndRegistration(); I = this;
		if (!thisView) thisView = GetComponent<MainMenuView>();
		preSplitScreenView = from; enterNameView.prevView = from;
		if (!registrationNameInput) registrationNameInput = enterNameView.GetComponentInChildren<EnterNameInputField>(true);
		if (!registrationNameInput || !thisView) return false;
		var template = F.I.enterRef.action.actionMap.asset;
		for (int i = 0; i < registrationSets.Length; i++)
		{
			registrationSets[i] = Instantiate(template);
			registrationSets[i].Disable();
			registrationSets[i].devices = null; registrationSets[i].bindingMask = null;
			foreach (var map in registrationSets[i].actionMaps) { map.devices = null; map.bindingMask = null; }
			RemapView.ApplyInputSet(registrationSets[i], i, true);
			if (i >= F.I.splitScreenPlayers.Count) continue;
			var player = F.I.splitScreenPlayers[i];
			registrationDrafts[i] = player.name == "PLAYER " + (i + 1) ? "" : player.name;
			player.deviceId = -1;
		}
		registrationPlayer = 0; registrationFinishing = false;
		registeredSet = registeredDevice = -1;
		RegistrationActive = true;
		registrationFrame = Time.frameCount; nextRegistrationTime = Time.unscaledTime + .2f;
		InputSystem.onEvent += RegistrationInput;
		InputSystem.onAfterUpdate += ProcessRegistrationInput;
		UpdateRegistrationIcon();
		return true;
	}
	void UpdateRegistrationIcon()
	{
		if (!Icon1P2P) return;
		Icon1P2P.gameObject.SetActive(true);
		if (PlayerIcons1P2P != null && registrationPlayer < PlayerIcons1P2P.Length)
			Icon1P2P.sprite = PlayerIcons1P2P[registrationPlayer];
	}
	public void RegistrationViewEnabled(MainMenuView view)
	{
		if (!IsRegistrationView(view)) return;
		UpdateRegistrationIcon();
		registrationNameInput.SetInputField(registrationDrafts[registrationPlayer] ?? "");
	}
	public void RegistrationViewDisabled(MainMenuView view)
	{
		if (IsRegistrationView(view)) EndRegistration();
	}
	void RegistrationInput(InputEventPtr inputEvent, InputDevice device)
	{
		if (!RegistrationActive || registrationFinishing || !enterNameView.gameObject.activeInHierarchy ||
			Time.unscaledTime < nextRegistrationTime || registeredSet >= 0 ||
			!(device is Keyboard || device is Gamepad || device is Joystick)) return;
		if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>()) return;
		// A physical pad is exclusive; a keyboard is shared through distinct input sets.
		for (int i = 0; i < registrationPlayer; i++)
		{
			var joined = F.I.splitScreenPlayers[i];
			if (!(device is Keyboard) && joined.deviceId == device.deviceId) return;
			if (!(device is Keyboard) || joined.deviceId != device.deviceId) continue;
			var usedSubmit = registrationSets[joined.keyboardProfile].FindAction(F.I.enterRef.action.id.ToString());
			foreach (var control in usedSubmit.controls)
				if (control.device == device && control is AxisControl axis && axis.ReadValueFromEvent(inputEvent, out float value) &&
					value >= (control is ButtonControl button ? button.pressPointOrDefault : .5f)) return;
		}
		for (int set = 0; set < registrationSets.Length; set++)
		{
			bool taken = false;
			for (int i = 0; i < registrationPlayer; i++)
			{
				var joined = F.I.splitScreenPlayers[i];
				// Keyboard and controller bindings in the same set can belong to different players.
				bool joinedWithKeyboard = InputSystem.GetDeviceById(joined.deviceId) is Keyboard;
				if (joined.keyboardProfile == set && joinedWithKeyboard == (device is Keyboard))
				{ taken = true; break; }
			}
			if (taken) continue;
			var submit = registrationSets[set].FindAction(F.I.enterRef.action.id.ToString());
			foreach (var control in submit.controls)
			{
				if (control.device != device || !(control is AxisControl axis)) continue;
				if (!axis.ReadValueFromEvent(inputEvent, out float value)) continue;
				float threshold = control is ButtonControl button ? button.pressPointOrDefault : .5f;
				if (value < threshold || axis.ReadValue() >= threshold) continue;
				registeredSet = set; registeredDevice = device.deviceId;
				return;
			}
		}
	}
	void ProcessRegistrationInput()
	{
		if (registeredSet < 0 || !RegistrationActive || registrationFinishing) return;
		int set = registeredSet, device = registeredDevice;
		registeredSet = registeredDevice = -1;
		if (registrationFrame == Time.frameCount || Time.unscaledTime < nextRegistrationTime) return;
		string driverName = registrationNameInput.GetInputField().Trim();
		if (string.IsNullOrWhiteSpace(driverName)) return;
		for (int i = 0; i < registrationPlayer; i++)
			if (string.Equals(F.I.splitScreenPlayers[i].name, driverName, StringComparison.OrdinalIgnoreCase)) return;
		var player = F.I.splitScreenPlayers[registrationPlayer];
		player.name = driverName; player.deviceId = device; player.keyboardProfile = set;
		registrationDrafts[registrationPlayer] = driverName;
		registrationFrame = Time.frameCount; nextRegistrationTime = Time.unscaledTime + .2f;
		registrationPlayer++;
		if (registrationPlayer >= F.I.splitScreenPlayers.Count)
		{
			registrationFinishing = true;
			RegistrationNavigationAllowed = true;
			enterNameView.GoToView(thisView);
			RegistrationNavigationAllowed = false;
			// Returning from the lobby goes to player-count selection, not the last name.
			thisView.prevView = preSplitScreenView;
		}
		else RegistrationViewEnabled(enterNameView);
	}
	public bool BackRegistration(MainMenuView view)
	{
		if (!IsRegistrationView(view)) return false;
		if (registrationFinishing) return true;
		registrationDrafts[registrationPlayer] = registrationNameInput.GetInputField();
		registeredSet = registeredDevice = -1;
		registrationFrame = Time.frameCount; nextRegistrationTime = Time.unscaledTime + .2f;
		if (registrationPlayer > 0)
		{
			registrationPlayer--;
			for (int i = registrationPlayer; i < F.I.splitScreenPlayers.Count; i++) F.I.splitScreenPlayers[i].deviceId = -1;
			RegistrationViewEnabled(view);
		}
		else
		{
			var previous = preSplitScreenView;
			registrationFinishing = true;
			RegistrationNavigationAllowed = true;
			view.GoToView(previous);
			RegistrationNavigationAllowed = false;
		}
		return true;
	}
	void EndRegistration()
	{
		InputSystem.onEvent -= RegistrationInput;
		InputSystem.onAfterUpdate -= ProcessRegistrationInput;
		RegistrationActive = false; RegistrationNavigationAllowed = false;
		registeredSet = registeredDevice = -1;
		foreach (var asset in registrationSets) if (asset) Destroy(asset);
		Array.Clear(registrationSets, 0, registrationSets.Length);
		if (Icon1P2P) Icon1P2P.gameObject.SetActive(false);
	}
	void ShowActivePlayerIcon()
	{
		RefreshSponsor();
		if (leaderboard) leaderboard.HighlightSplitScreenPlayer(ActiveMenuPlayerIndex);
		if (!Icon1P2P || !ValidPlayer(ActiveMenuPlayerIndex)) return;
		Icon1P2P.gameObject.SetActive(true);
		if (PlayerIcons1P2P != null && ActiveMenuPlayerIndex < PlayerIcons1P2P.Length) Icon1P2P.sprite = PlayerIcons1P2P[ActiveMenuPlayerIndex];
	}
	void StartMenuInput()
	{
		StopMenuInput();
		for (int i = 0; i < F.I.splitScreenPlayers.Count; i++)
		{
			int index = i; var player = F.I.splitScreenPlayers[i];
			var device = InputSystem.GetDeviceById(player.deviceId); if (device == null) continue;
			var asset = Instantiate(F.I.enterRef.action.actionMap.asset); asset.Disable();
			asset.devices = new[] { device }; asset.bindingMask = InputBinding.MaskByGroup(device is Keyboard ? "Keyboard" : "Gamepad");
			foreach (var map in asset.actionMaps) { map.devices = null; map.bindingMask = null; }
			RemapView.ApplyInputSet(asset, player.keyboardProfile, device is Keyboard);
			var move = asset.FindAction("Global/Move");
			if (device is Keyboard && move != null)
			{
				var drive = asset.FindAction(F.I.driveRef.action.id.ToString());
				for (int b = 0; b < move.bindings.Count; b++)
				{
					var binding = move.bindings[b];
					if (!binding.isPartOfComposite || !(binding.path?.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ?? false)) continue;
					// Explicit UI navigation overrides win; otherwise use this player's directional keys.
					if (binding.overridePath != null) continue;
					foreach (var part in drive.bindings)
						if (part.isPartOfComposite && string.Equals(part.name, binding.name, StringComparison.OrdinalIgnoreCase) &&
							(part.path?.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ?? false))
						{ move.ApplyBindingOverride(b, part.effectivePath); break; }
				}
			}
			if (move != null) { move.performed += context => MenuMove(index, context); move.Enable(); }
			if (!(device is Keyboard))
			{
				var trackMove = asset.FindAction(F.I.move2Ref.action.id.ToString());
				if (trackMove != null && trackMove != move) { trackMove.performed += context => MenuMove(index, context); trackMove.Enable(); }
			}
			var submit = asset.FindAction(F.I.enterRef.action.id.ToString());
			submit.wantsInitialStateCheck = false;
			submit.performed += context => MenuSubmit(index); submit.Enable();
			menuInputSets.Add(asset); menuInputByPlayer[index] = asset;
		}
		menuInputConnected = true;
		InputSystem.onEvent += MenuActivity;
		InputSystem.onAfterUpdate += UpdateActiveMenuPlayer;
	}
	void StopMenuInput()
	{
		menuInputConnected = false;
		InputSystem.onEvent -= MenuActivity;
		InputSystem.onAfterUpdate -= UpdateActiveMenuPlayer;
		pendingActivePlayer = -1;
		menuInputByPlayer.Clear();
		foreach (var asset in menuInputSets) if (asset) { asset.Disable(); Destroy(asset); }
		menuInputSets.Clear();
	}
	void MenuActivity(InputEventPtr inputEvent, InputDevice device)
	{
		if (inputEvent.handled || !menuInputConnected || !(device is Keyboard || device is Gamepad || device is Joystick)) return;
		if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>()) return;
		foreach (var control in inputEvent.EnumerateChangedControls(device))
		{
			if (control.synthetic || !(control is AxisControl axis) || !axis.ReadValueFromEvent(inputEvent, out float value)) continue;
			float threshold = control is ButtonControl button ? button.pressPointOrDefault : .5f;
			if (Mathf.Abs(value) < threshold || (Mathf.Abs(axis.ReadValue()) >= threshold && value * axis.ReadValue() >= 0)) continue;
			for (int i = 0; i < F.I.splitScreenPlayers.Count; i++)
			{
				if (!IsMenuPlayerAllowed(i) || F.I.splitScreenPlayers[i].deviceId != device.deviceId) continue;
				if (device is Keyboard)
				{
					if (!menuInputByPlayer.TryGetValue(i, out var asset)) continue;
					var navigation = asset.FindAction("Global/Move");
					var submit = asset.FindAction(F.I.enterRef.action.id.ToString());
					var drive = asset.FindAction(F.I.driveRef.action.id.ToString());
					if (!(navigation?.controls.Contains(control) ?? false) && !(submit?.controls.Contains(control) ?? false) &&
						!(drive?.controls.Contains(control) ?? false)) continue;
				}
				pendingActivePlayer = i;
				return;
			}
		}
	}
	void UpdateActiveMenuPlayer()
	{
		if (pendingActivePlayer < 0) return;
		int index = pendingActivePlayer; pendingActivePlayer = -1;
		if (!IsMenuPlayerAllowed(index)) return;
		ActiveMenuPlayerIndex = index;
		ShowActivePlayerIcon();
	}
	bool IsMenuPlayerAllowed(int index) => menuInputConnected && F.I.gameMode == GameMode.Splitscreen &&
		(GaragePlayerIndex >= 0 ? index == GaragePlayerIndex && garageView && garageView.gameObject.activeInHierarchy : thisView && thisView.gameObject.activeInHierarchy);
	void MenuMove(int index, InputAction.CallbackContext context)
	{
		if (!IsMenuPlayerAllowed(index)) return;
		Vector2 direction = context.ReadValue<Vector2>(); if (direction.sqrMagnitude < .25f) return;
		ActiveMenuPlayerIndex = index; ShowActivePlayerIcon();
		if (GaragePlayerIndex >= 0) { if (carSelector) carSelector.MoveSelection(direction); return; }
		var globalMove = F.I.enterRef.action.actionMap.FindAction("Move");
		// The main UI already handles its own navigation keys. Forward additional players' keys once.
		if (globalMove != null && globalMove.enabled && globalMove.controls.Contains(context.control)) return;
		if (!EventSystem.current || !EventSystem.current.currentSelectedGameObject) return;
		var data = new AxisEventData(EventSystem.current) { moveVector = direction,
			moveDir = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? (direction.x > 0 ? MoveDirection.Right : MoveDirection.Left) : direction.y > 0 ? MoveDirection.Up : MoveDirection.Down };
		ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, data, ExecuteEvents.moveHandler);
	}
	void MenuSubmit(int index)
	{
		if (!IsMenuPlayerAllowed(index)) return;
		ActiveMenuPlayerIndex = index; lastMenuSubmitFrame = Time.frameCount; ShowActivePlayerIcon();
		if (GaragePlayerIndex >= 0) { if (carSelector) carSelector.BuySelectedVehicle(); return; }
		if (garageBtn && EventSystem.current && EventSystem.current.currentSelectedGameObject == garageBtn.gameObject) OpenGarage();
	}
	public void OpenGarage()
	{
		if (!garageView || GaragePlayerIndex >= 0) return;
		thisView.GoToView(garageView);
	}
	public bool PrepareGarage(MainMenuView destination)
	{
		if (GaragePlayerIndex >= 0 || !ValidPlayer(ActiveMenuPlayerIndex)) return false;
		if (F.I.enterRef.action.WasPressedThisFrame() && lastMenuSubmitFrame != Time.frameCount) return false;
		if (!carSelector) carSelector = destination.GetComponentInChildren<CarSelector>(true);
		if (!carSelector) return false;
		garageView = destination; GaragePlayerIndex = ActiveMenuPlayerIndex;
		carSelector.SetSplitScreenPlayer(GaragePlayerIndex);
		ShowActivePlayerIcon(); return true;
	}
	public void ReturnFromGarage()
	{
		GarageReturnAllowed = true;
		garageView.GoToView(thisView);
		GarageReturnAllowed = false;
	}
	public int GarageCost(int carIndex)
	{
		var player = GaragePlayer;
		if (player == null || carIndex == player.car) return 0;
		return F.I.cars[carIndex].price - Mathf.FloorToInt(Mathf.Max(0, F.I.cars[player.car].price) * Championships.TradeInFraction);
	}
	public bool BuyGarageCar(int carIndex)
	{
		var player = GaragePlayer;
		if (player == null || carIndex < 0 || carIndex >= F.I.cars.Length) return false;
		if (player.car == carIndex) return true;
		if (F.I.scoringType == ScoringType.Championship)
		{
			int cost = GarageCost(carIndex);
			if (F.I.cars[carIndex].price < 0 || cost > player.Funds) return false;
			player.cash = player.Funds - cost;
		}
		player.car = carIndex;
		return true;
	}
	void Bind(Button button, UnityAction action)
	{
		if (!button) return;
		button.onClick.AddListener(action);
		unbindUI.Add(() => { if (button) button.onClick.RemoveListener(action); });
	}
	void UnbindButtons()
	{
		foreach (var unbind in unbindUI) unbind();
		unbindUI.Clear();
	}
	public void BindUI()
	{
		// PreSplitScreenView also calls this while this selector is inactive.
		if (uiBound) return;
		uiBound = true;
		Bind(playersButton, () => SwitchSplitScreenPlayers()); Bind(raceButton, StartLocalRace);
		Bind(lapsButton, () => SwitchLaps()); Bind(raceTypeButton, () => SwitchRaceType());
		Bind(rivalsButton, () => SwitchRivals()); Bind(timeOfDayButton, () => SwitchDayNight());
		Bind(randomCarsButton, () => SwitchRandomCar()); Bind(randomTracksButton, () => SwitchRandomTrack());
		Bind(scoringButton, () => SwitchScoring(false)); Bind(cpuLevelButton, () => SwitchCPULevel());
		Bind(catchupButton, () => SwitchCatchup()); Bind(roadTypeButton, () => SwitchRoadType());
		Bind(sponsorBtn, () => SwitchSponsor());
		Bind(teamsBtn, () => SwitchTeams());
		Bind(garageBtn, OpenGarage);
		for (int i = 0; i < playerRows.Length; i++)
		{
			int index = i; var row = playerRows[i]; if (row == null) continue;
			Bind(row.carButton, () => CycleCar(index));
			if (row.nameInput)
			{
				UnityAction<string> changed = value => SetPlayerName(index, value);
				row.nameInput.onValueChanged.AddListener(changed);
				unbindUI.Add(() => { if (row.nameInput) row.nameInput.onValueChanged.RemoveListener(changed); });
			}
		}
	}
	void DeviceChanged(InputDevice device, InputDeviceChange change) => Refresh();
	int Direction() => F.I.shiftRef.action.IsPressed() ? -1 : 1;
	bool ValidPlayer(int index) => index >= 0 && index < F.I.splitScreenPlayers.Count;
	void AddPlayer()
	{
		int i = F.I.splitScreenPlayers.Count;
		var devices = SplitScreenRace.AvailableDevices().OrderBy(device => device is Keyboard && Gamepad.all.Count >= 2 ? 1 : 0);
		var device = devices.FirstOrDefault(candidate => F.I.splitScreenPlayers.All(player => player.deviceId != candidate.deviceId))
			?? devices.FirstOrDefault(candidate => candidate is Keyboard);
		string name = i == 0 ? F.I.playerData.playerName : "PLAYER " + (i + 1);
		F.I.splitScreenPlayers.Add(new SplitScreenPlayerInfo { name = string.IsNullOrWhiteSpace(name) ? "PLAYER " + (i + 1) : name,
			keyboardProfile = i, cash = 0, car = F.I.s_playerCarIdx, deviceId = device?.deviceId ?? -1, sponsor = F.RandomLivery() });
	}
	void RefreshPlayers()
	{
		for (int i = 0; i < playerRows.Length; i++)
		{
			var row = playerRows[i]; if (row == null) continue;
			bool active = ValidPlayer(i); if (row.root) row.root.SetActive(active);
			if (!active) continue;
			var player = F.I.splitScreenPlayers[i];
			if (row.nameInput) row.nameInput.SetTextWithoutNotify(player.name);
			SetText(row.scoreText, player.score.ToString("N0"));
			SetText(row.carText, F.I.randomCars ? L("Random") : player.car >= 0 && player.car < F.I.cars.Length ? L(F.I.cars[player.car].name) : "-");
			if (row.carButton) row.carButton.interactable = !F.I.randomCars;
			var device = InputSystem.GetDeviceById(player.deviceId);
			SetText(row.controllerText, device != null && device.added ? device.displayName + " #" + device.deviceId : L("Select controller"));
			bool keyboard = device is Keyboard;
			SetText(row.keyboardProfileText, L("Keyboard profile") + " " + (player.keyboardProfile + 1));
		}
	}
	public void SetPlayerName(int index, string value)
	{
		if (!ValidPlayer(index)) return;
		F.I.splitScreenPlayers[index].name = value.Trim(); Refresh();
	}
	public void CycleCar(int index)
	{
		if (!ValidPlayer(index) || F.I.randomCars) return;
		if (F.I.scoringType == ScoringType.Championship) { ActiveMenuPlayerIndex = index; OpenGarage(); return; }
		var cars = Enumerable.Range(0, F.I.cars.Length).Where(i => F.I.cars[i].unlocked).ToArray(); if (cars.Length == 0) return;
		int current = Array.IndexOf(cars, F.I.splitScreenPlayers[index].car);
		F.I.splitScreenPlayers[index].car = cars[(current + Direction() + cars.Length) % cars.Length]; Refresh();
	}
	protected override void SetTrackShaenigans()
	{
		base.SetTrackShaenigans();
		Refresh();
	}
	public void SelectTrack(string id)
	{
		if (id == null || !F.I.tracks.TryGetValue(id, out var track) || !track.valid || !track.unlocked || track.isSkatePark) return;
		F.I.s_trackName = id;
		if (trackContent && FindSelectedTrackEqualToTrackname()) SetTrackShaenigans();
		else Refresh();
	}
	public void Refresh()
	{
		if (!Enum.IsDefined(typeof(CpuLevel), F.I.s_cpuLevel)) F.I.s_cpuLevel = CpuLevel.Medium;
		RefreshSponsor();
		TMP_Text teamLabel = teamsText;
		if (!teamLabel && teamsBtn) teamLabel = teamsBtn.GetComponentInChildren<TMP_Text>(true);
		SetText(teamLabel, L("TEAMS") + ": " + L(F.I.teams ? "Yes" : "No"));
		if (splitScreenEntryBtn) splitScreenEntryBtn.interactable = F.I.splitScreenPlayers.Count >= 2;
		if (F.I.s_raceType == RaceType.Knockout) F.I.s_laps = F.I.s_cpuRivals + F.I.splitScreenPlayers.Count - 1;
		if (lapsButton) lapsButton.interactable = F.I.s_raceType != RaceType.Knockout;
		SetText(splitScreenPlayersNumberText, L("SPLIT SCREEN PLAYERS") + ": " + F.I.splitScreenPlayers.Count);
		SetText(raceTypeButtonText, L(F.I.s_raceType.ToString()));
		SetText(lapsButtonText, L("Laps") + ": " + F.I.s_laps);
		SetText(rivalsButtonText, L("Rivals") + ": " + F.I.s_cpuRivals);
		SetText(nightButtonText, L(F.I.s_timeOfDay.ToString()));
		SetText(randomCarsText, L("Cars") + ": " + L(F.I.randomCars ? "Random" : "Select"));
		SetText(randomTracksText, L("Tracks") + ": " + L(F.I.randomTracks ? "Random" : "Select"));
		SetText(scoringText, L(F.I.scoringType.ToString())); SetText(roundText, L("Round") + ": " + Mathf.Min(CurrentRound, TotalRounds) + "/" + TotalRounds);
		SetText(LevelButtonText, L("CPU") + ": " + L(F.I.s_cpuLevel.ToString()));
		SetText(catchupButtonText, L("Catchup") + ": " + L(F.I.catchup ? "Yes" : "No"));
		SetText(roadTypeText ? roadTypeText : wayButtonText, L("Tex") + ": " + L(F.I.s_roadType.ToString()));
		RefreshPlayers();
		if (leaderboard) leaderboard.RefreshSplitScreen(ShortLeaderboardRowPrefab, ActiveMenuPlayerIndex);
		raceReady = SplitScreenRace.ValidatePlayers(out string error);
		var names = F.I.splitScreenPlayers.Select(player => player.name).ToArray();
		if (names.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
		{ raceReady = false; error = "Enter a different name for each player"; }
		foreach (var player in F.I.splitScreenPlayers)
			if (InputSystem.GetDeviceById(player.deviceId) is Keyboard && player.keyboardProfile >= 0 && player.keyboardProfile < SplitScreenRace.MaxPlayers &&
				SplitScreenKeyboardProfile.Load(player.keyboardProfile).keys.Take(4).Any(string.IsNullOrEmpty))
			{ raceReady = false; error = "Assign acceleration, brake and steering keys to each keyboard profile"; }
		if (!F.I.tracks.TryGetValue(F.I.s_trackName ?? "", out var selected) || !selected.valid || !selected.unlocked || selected.isSkatePark)
		{ raceReady = false; error = "Select a track"; }
		if (F.I.teams && (F.I.splitScreenPlayers.Any(player => player.sponsor == Livery.Random) ||
			(F.I.s_cpuRivals == 0 && F.I.splitScreenPlayers.Select(player => player.sponsor).Distinct().Count() < 2)))
		{ raceReady = false; error = L("You need at least two teams"); }
		if (raceButton) raceButton.interactable = raceReady;
	}
	public void StartLocalRace()
	{
		Refresh(); if (!raceReady || !thisView) return;
		if (SeriesCompleted) ResetSeries();
		if (F.I.randomTracks)
		{
			var available = F.I.tracks.Where(pair => pair.Value.valid && pair.Value.unlocked && !pair.Value.isSkatePark && pair.Key.Length > 3).Select(pair => pair.Key).ToArray();
			if (available.Length == 0) return;
			F.I.s_trackName = available[UnityEngine.Random.Range(0, available.Length)];
		}
		var cars = Enumerable.Range(0, F.I.cars.Length).Where(i => F.I.cars[i].unlocked).ToArray();
		if (F.I.randomCars && cars.Length > 0) foreach (var player in F.I.splitScreenPlayers) player.car = cars[UnityEngine.Random.Range(0, cars.Length)];
		F.I.s_playerCarIdx = F.I.splitScreenPlayers[0].car;
		F.I.s_cpuRivals = Mathf.Clamp(F.I.s_cpuRivals, 0, SplitScreenRace.MaxPlayers - F.I.splitScreenPlayers.Count);
		thisView.ToRaceScene();
	}
	public void SwitchSplitScreenPlayers(bool init = false)
	{
		if (!init)
		{
			if (Direction() > 0 && F.I.splitScreenPlayers.Count < SplitScreenRace.MaxPlayers) AddPlayer();
			else if (Direction() < 0 && F.I.splitScreenPlayers.Count > 0) F.I.splitScreenPlayers.RemoveAt(F.I.splitScreenPlayers.Count - 1);
		}
		if (splitScreenEntryBtn) splitScreenEntryBtn.interactable = F.I.splitScreenPlayers.Count >= 2;
		F.I.s_cpuRivals = Mathf.Min(F.I.s_cpuRivals, SplitScreenRace.MaxPlayers - F.I.splitScreenPlayers.Count); Refresh();
	}
	public new void SwitchRaceType(bool init = false) { if (!init) F.I.s_raceType = (RaceType)F.Wraparound((int)F.I.s_raceType + Direction(), 0, F.I.RaceTypes - 1); Refresh(); }
	public new void SwitchLaps(bool init = false) { if (!init && F.I.s_raceType != RaceType.Knockout) F.I.s_laps = F.Wraparound(F.I.s_laps + Direction(), 1, 99); Refresh(); }
	public new void SwitchRivals(bool init = false) { if (!init) F.I.s_cpuRivals = F.Wraparound(F.I.s_cpuRivals + Direction(), 0, SplitScreenRace.MaxPlayers - F.I.splitScreenPlayers.Count); Refresh(); }
	public new void SwitchDayNight(bool init = false) { if (!init) F.I.s_timeOfDay = (TimeOfDay)F.Wraparound((int)F.I.s_timeOfDay + Direction(), 0, Info.TimeOfDays - 1); Refresh(); }
	public new void SwitchCPULevel(bool init = false) { if (!init) F.I.s_cpuLevel = (CpuLevel)F.Wraparound((int)F.I.s_cpuLevel + Direction(), 0, 2); Refresh(); }
	public new void SwitchCatchup(bool init = false) { if (!init) F.I.catchup = !F.I.catchup; Refresh(); }
	public void SwitchTeams(bool init = false)
	{
		if (!init)
		{
			F.I.teams = !F.I.teams;
			ResetSeries();
		}
		if (F.I.teams)
			foreach (var player in F.I.splitScreenPlayers)
				if (player.sponsor == Livery.Random) player.sponsor = Livery.TGR;
		Refresh();
	}
	public new void SwitchSponsor(bool init = false)
	{
		if (!ValidPlayer(ActiveMenuPlayerIndex)) return;
		if (!init)
		{
			var player = F.I.splitScreenPlayers[ActiveMenuPlayerIndex];
			player.sponsor = (Livery)F.Wraparound((int)player.sponsor + Direction(), F.I.teams ? 1 : 0, F.I.Liveries);
			if (seriesResults.TryGetValue(player.name, out var standing)) standing.sponsor = player.sponsor;
		}
		Refresh();
	}
	void RefreshSponsor()
	{
		bool valid = ValidPlayer(ActiveMenuPlayerIndex);
		if (sponsorBtn) sponsorBtn.interactable = valid;
		TMP_Text label = sponsorButtonText;
		if (!label && sponsorBtn) label = sponsorBtn.GetComponentInChildren<TMP_Text>(true);
		SetText(label, L("Sponsor") + ": " + (valid ? L(F.I.splitScreenPlayers[ActiveMenuPlayerIndex].sponsor.ToString()) : ""));
	}
	public override void SwitchRoadType(bool init = false) { if (!init) F.I.s_roadType = (PavementType)F.Wraparound((int)F.I.s_roadType + Direction(), 0, F.I.pavementTypes + 1); F.I.randomPavement = F.I.s_roadType == PavementType.Random; Refresh(); }
	public void GetPlayerName(string value) => SetPlayerName(0, value);
	public override void ResetButtons() => Refresh();
	public void SwitchRandomCar(bool init = false) { if (!init) F.I.randomCars = !F.I.randomCars; Refresh(); }
	public void SwitchRandomTrack(bool init = false) { if (!init) F.I.randomTracks = !F.I.randomTracks; if (trackContent) SetTrackShaenigans(); else Refresh(); }
	public void SwitchScoring(bool init) { if (!init) F.I.scoringType = (ScoringType)(((int)F.I.scoringType + 1) % Enum.GetValues(typeof(ScoringType)).Length); Refresh(); }
	public void SwitchRound(bool init = false)
	{
		if (!init)
		{
			F.I.Rounds = (byte)F.Wraparound(TotalRounds + Direction(), 1, 99);
			ResetSeries();
		}
		Refresh();
	}
	void ResetSeries()
	{
		CurrentRound = 1;
		seriesResults.Clear();
		foreach (var player in F.I.splitScreenPlayers)
		{
			player.score = 0;
			player.cash = 0;
		}
	}
	public void MoveToSelectedTrack() => SelectTrack(F.I.s_trackName);
	public void EnableSelectionOfTracks(bool enabled)
	{
		if (!trackContent) return;
		foreach (var button in trackContent.GetComponentsInChildren<Button>(true)) button.interactable = enabled;
	}
	public static void RecordResults()
	{
		var results = ResultsView.SortedResultsByFinishPos.ToArray();
		for (int position = 0; position < results.Length; position++)
		{
			var result = results[position];
			int reward = result.disqualified ? 0 : F.I.scoringType == ScoringType.Championship ? ResultsView.CalculateChampionshipReward(result).Total :
				F.I.scoringType == ScoringType.Victory ? (position == 0 ? 1 : 0) : Mathf.Max(0, 10 - position);
			if (!I.seriesResults.TryGetValue(result.name, out var standing))
				I.seriesResults[result.name] = standing = new Result { name = result.name, sponsor = result.sponsor };
			standing.sponsor = result.sponsor;
			standing.SetPostRaceScore(standing.score + reward);
			var player = F.I.splitScreenPlayers.FirstOrDefault(player => player.name == result.name);
			if (player != null)
			{
				if (F.I.scoringType == ScoringType.Championship) player.cash = player.Funds + reward;
				player.score += reward;
			}
		}
		I.CurrentRound++;
	}
}

