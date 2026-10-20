using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.UI;

public class RemapView : MonoBehaviour
{
	public Button CancelRebindBtn;
	public InputActionAsset actions;
	public Button RebindControllerMenu;
	
	public Transform[] buttonContainers;
	[Tooltip("Updated automatically by keyboard keys or controller input. Mouse input does not change it.")]
	public bool keyboardMode = true;
	public bool KeyboardMode => keyboardMode;
	public int CurrentSet => currentSet;
	int currentSet;
	readonly InputActionAsset[] inputSets = new InputActionAsset[SplitScreenRace.MaxPlayers];
	readonly List<RebindActionUI> buttons = new();
	readonly bool[] setUsesKeyboard = new bool[SplitScreenRace.MaxPlayers];
	bool refreshSetPending;
	bool connected;
	public Button removeBindingsButton;
	static string SetKey(int index) => "SplitScreen.InputSet." + index;
	static string SetDeviceKey(int index) => "SplitScreen.InputSetDevice." + index;

	static bool lastDeviceWasKeyboard = true;
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void TrackInputDevices()
	{
		lastDeviceWasKeyboard = true;
		// Also works with domain reload disabled, without duplicating the callback.
		InputSystem.onEvent -= DeviceInput;
		InputSystem.onEvent += DeviceInput;
	}
	static void DeviceInput(InputEventPtr inputEvent, InputDevice device)
	{
		if (inputEvent.handled || RebindActionUI.IsRebinding) return;
		bool keyboard = device is Keyboard;
		if (!keyboard && !(device is Gamepad) && !(device is Joystick)) return;
		if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>()) return;
		foreach (var control in inputEvent.EnumerateChangedControls(device))
		{
			if (control.synthetic || !(control is AxisControl axis)) continue;
			if (keyboard && !(control is KeyControl)) continue;
			if (!axis.ReadValueFromEvent(inputEvent, out float value)) continue;
			// React to presses and deliberate stick movement, not releases or drift.
			float threshold = control is ButtonControl button ? button.pressPointOrDefault : .5f;
			if (Mathf.Abs(value) >= threshold && Mathf.Abs(axis.ReadValue()) < threshold)
			{
				lastDeviceWasKeyboard = keyboard;
				return;
			}
		}
	}
	void UpdateDeviceMode()
	{
		if (!connected || RebindActionUI.IsRebinding || keyboardMode == lastDeviceWasKeyboard) return;
		// Navigation selects the next capture device, never the displayed binding set.
		keyboardMode = lastDeviceWasKeyboard;
	}

	public void OnEnable()
	{
		if (!actions) return;
		buttons.Clear();
		if (buttonContainers != null)
			foreach (var container in buttonContainers)
				if (container)
					foreach (var button in container.GetComponentsInChildren<RebindActionUI>(true))
						if (!buttons.Contains(button)) buttons.Add(button);
		for (int i = 0; i < setUsesKeyboard.Length; i++)
			setUsesKeyboard[i] = PlayerPrefs.GetInt(SetDeviceKey(i), 1) != 0;
		foreach (var button in buttons)
		{
			button.stopRebindEvent.AddListener(RebindStopped);
			button.prepareRebind += PrepareRebind;
			button.resolveCaptureBinding += ResolveCaptureBinding;
		}
		connected = true;
		if (removeBindingsButton) removeBindingsButton.onClick.AddListener(ClearVisibleBindings);
		if (CancelRebindBtn) CancelRebindBtn.onClick.AddListener(CancelRebinding);
		keyboardMode = lastDeviceWasKeyboard;
		InputSystem.onAfterUpdate += UpdateDeviceMode;
		InputSystem.onEvent += ClearShortcut;
		if (SplitScreenSelector.I && SplitScreenSelector.I.Icon1P2P) SplitScreenSelector.I.Icon1P2P.gameObject.SetActive(true);
		ShowSet();
	}
	public void ToggleSet(int dir)
	{
		if (!connected) return;
		CancelRebinding(); SaveCurrentSet();
		currentSet = F.Wraparound(currentSet + dir, 0, SplitScreenRace.MaxPlayers - 1);
		ShowSet();
	}
	InputActionAsset GetSet()
	{
		if (inputSets[currentSet]) return inputSets[currentSet];
		var asset = Instantiate(actions);
		asset.name = actions.name + " - Set " + (currentSet + 1);
		asset.Disable();
		ApplyInputSet(asset, currentSet, true);
		inputSets[currentSet] = asset;
		return asset;
	}
	public void ShowSet()
	{
		if (!connected || !actions) return;
		var asset = GetSet();
		foreach (var button in buttons)
		{
			if (!button || button.actionReference?.action == null) continue;
			var action = asset.FindAction(button.actionReference.action.id.ToString());
			int binding = FindBindingForDevice(action, button.bindingId, setUsesKeyboard[currentSet]);
			button.SetInputSet(asset, currentSet, binding >= 0 ? action.bindings[binding].id.ToString() : "");
			if (binding < 0)
				Debug.LogWarning($"RemapView: no {(setUsesKeyboard[currentSet] ? "Keyboard" : "Gamepad")} binding corresponding to '{button.name}' on '{action?.name}'. Check its Action and Binding in the Inspector.", button);
		}
		if (currentSet < SplitScreenSelector.I.PlayerIcons1P2P.Length)
			SplitScreenSelector.I.Icon1P2P.sprite = SplitScreenSelector.I.PlayerIcons1P2P[currentSet];
	}
	public void SetKeyboardMode(bool keyboard)
	{
		if (!connected) { keyboardMode = keyboard; return; }
		// An explicit selection affects the next capture, not existing assignments.
		keyboardMode = keyboard;
	}
	public void ToggleInputDevice() => SetKeyboardMode(!keyboardMode);
	int FindBindingForDevice(InputAction action, string originalId, bool keyboard)
	{
		if (action == null) return -1;
		int original = -1;
		for (int i = 0; i < action.bindings.Count; i++)
			if (action.bindings[i].id.ToString() == originalId) { original = i; break; }
		if (original < 0) return -1;
		var source = action.bindings[original];
		if (!source.isComposite && MatchesDevice(source, keyboard)) return original;
		for (int i = 0; i < action.bindings.Count; i++)
		{
			var candidate = action.bindings[i];
			if (source.isComposite)
			{
				if (!candidate.isComposite) continue;
				for (int part = i + 1; part < action.bindings.Count && action.bindings[part].isPartOfComposite; part++)
					if (MatchesDevice(action.bindings[part], keyboard)) return i;
			}
			else if (!candidate.isComposite && candidate.isPartOfComposite == source.isPartOfComposite &&
				(!source.isPartOfComposite || string.Equals(candidate.name, source.name, StringComparison.OrdinalIgnoreCase)) && MatchesDevice(candidate, keyboard)) return i;
		}
		return -1;
	}
	bool MatchesDevice(InputBinding binding, bool keyboard)
	{
		string group = keyboard ? "Keyboard" : "Gamepad";
		if ((binding.groups ?? "").Split(';').Contains(group)) return true;
		string path = binding.path ?? "";
		return keyboard ? path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) :
			path.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("<Joystick>/", StringComparison.OrdinalIgnoreCase);
	}
	void CancelRebinding()
	{
		foreach (var button in buttons) if (button) button.ongoingRebind?.Cancel();
	}
	void PrepareRebind(RebindActionUI button)
	{
		var asset = GetSet();
		var action = asset.FindAction(button.actionReference.action.id.ToString());
		int binding = FindBindingForDevice(action, button.bindingId, keyboardMode);
		if (binding < 0) binding = FindBindingForDevice(action, button.bindingId, !keyboardMode);
		button.SetInputSet(asset, currentSet, binding >= 0 ? action.bindings[binding].id.ToString() : "");
	}
	int ResolveCaptureBinding(RebindActionUI button, bool keyboard, string partName)
	{
		var action = GetSet().FindAction(button.actionReference.action.id.ToString());
		int index = FindBindingForDevice(action, button.bindingId, keyboard);
		if (index < 0 || !action.bindings[index].isComposite) return index;
		for (int part = index + 1; part < action.bindings.Count && action.bindings[part].isPartOfComposite; part++)
			if (string.Equals(action.bindings[part].name, partName, StringComparison.OrdinalIgnoreCase)) return part;
		return -1;
	}
	void RebindStopped(RebindActionUI button, InputActionRebindingExtensions.RebindingOperation operation)
	{
		if (button.inputSetIndex != currentSet) return;
		if (button.bindingCleared)
		{
			setUsesKeyboard[currentSet] = button.clearedKeyboardBinding;
			PlayerPrefs.SetInt(SetDeviceKey(currentSet), setUsesKeyboard[currentSet] ? 1 : 0);
		}
		else if (operation.completed && operation.selectedControl != null)
		{
			setUsesKeyboard[currentSet] = operation.selectedControl.device is Keyboard;
			PlayerPrefs.SetInt(SetDeviceKey(currentSet), setUsesKeyboard[currentSet] ? 1 : 0);
		}
		SaveCurrentSet();
		refreshSetPending = true;
	}
	void LateUpdate()
	{
		if (!refreshSetPending || RebindActionUI.IsRebinding) return;
		refreshSetPending = false;
		ShowSet();
	}
	void ClearShortcut(InputEventPtr inputEvent, InputDevice device) => TryClearVisibleBindings(inputEvent, device);
	internal bool TryClearVisibleBindings(InputEventPtr inputEvent, InputDevice device)
	{
		if (!connected || !isActiveAndEnabled || inputEvent.handled || !(device is Keyboard keyboard)) return false;
		if (!inputEvent.IsA<StateEvent>() && !inputEvent.IsA<DeltaStateEvent>()) return false;
		if (!keyboard.deleteKey.ReadValueFromEvent(inputEvent, out float value) || value < .5f || keyboard.deleteKey.isPressed) return false;
		bool shift = keyboard.leftShiftKey.ReadValueFromEvent(inputEvent, out float left) ? left >= .5f : keyboard.leftShiftKey.isPressed;
		shift |= keyboard.rightShiftKey.ReadValueFromEvent(inputEvent, out float right) ? right >= .5f : keyboard.rightShiftKey.isPressed;
		if (!shift) return false;
		inputEvent.handled = true;
		ClearVisibleBindings();
		return true;
	}
	public void ClearVisibleBindings()
	{
		if (!connected) return;
		var visible = buttons.Where(button => button && button.isActiveAndEnabled).ToArray();
		CancelRebinding();
		// Restore the displayed device's bindings; navigation may have used another device.
		ShowSet();
		var asset = GetSet();
		var cleared = new HashSet<Guid>();
		foreach (var button in visible)
		{
			if (!button.ResolveActionAndBinding(out var action, out int index) || action.actionMap?.asset != asset) continue;
			if (action.bindings[index].isComposite)
			{
				for (int part = index + 1; part < action.bindings.Count && action.bindings[part].isPartOfComposite; part++) Clear(action, part);
			}
			else Clear(action, index);
		}
		void Clear(InputAction action, int index)
		{
			if (!cleared.Add(action.bindings[index].id)) return;
			action.ApplyBindingOverride(index, new InputBinding { overridePath = "", overrideProcessors = "", overrideInteractions = "" });
		}
		SaveCurrentSet(); refreshSetPending = false;
		ShowSet();
	}
	void SaveCurrentSet()
	{
		var asset = inputSets[currentSet]; if (!asset) return;
		string json = asset.SaveBindingOverridesAsJson();
		PlayerPrefs.SetString(SetKey(currentSet), json);
		// Set 1 also remains the regular single-player/menu setup.
		if (currentSet == 0) PlayerPrefs.SetString("rebinds", json);
		var keyboard = SplitScreenKeyboardProfile.Load(currentSet);
		keyboard.ReadBindings(asset); keyboard.Save(currentSet);
	}
	public void OnDisable()
	{
		if (!connected) return;
		InputSystem.onAfterUpdate -= UpdateDeviceMode;
		InputSystem.onEvent -= ClearShortcut;
		if (removeBindingsButton) removeBindingsButton.onClick.RemoveListener(ClearVisibleBindings);
		if (CancelRebindBtn) CancelRebindBtn.onClick.RemoveListener(CancelRebinding);
		CancelRebinding(); SaveCurrentSet();
		foreach (var button in buttons)
			if (button)
			{
				button.stopRebindEvent.RemoveListener(RebindStopped);
				button.prepareRebind -= PrepareRebind;
				button.resolveCaptureBinding -= ResolveCaptureBinding;
				button.ClearInputSet();
			}
		// Publish set 1 to regular gameplay after editing, keeping the menu asset out of capture.
		if (actions && inputSets[0]) actions.LoadBindingOverridesFromJson(inputSets[0].SaveBindingOverridesAsJson());
		buttons.Clear(); connected = false; refreshSetPending = false;
		for (int i = 0; i < inputSets.Length; i++)
			if (inputSets[i]) { inputSets[i].Disable(); Destroy(inputSets[i]); inputSets[i] = null; }
		inputSets[0] = null;
		PlayerPrefs.Save();
		if (SplitScreenSelector.I && SplitScreenSelector.I.Icon1P2P) SplitScreenSelector.I.Icon1P2P.gameObject.SetActive(false);
	}
	// Called on the private PlayerInput copy at race start, for keyboards and pads.
	[Serializable] class SavedBinding { public string id = ""; public string path = ""; }
	[Serializable] class SavedBindings { public SavedBinding[] bindings = Array.Empty<SavedBinding>(); }
	public static void ApplyInputSet(InputActionAsset asset, int index, bool keyboard)
	{
		index = Mathf.Clamp(index, 0, SplitScreenRace.MaxPlayers - 1);
		string json = PlayerPrefs.GetString(SetKey(index), "");
		if (!string.IsNullOrEmpty(json)) asset.LoadBindingOverridesFromJson(json);
		foreach (var map in asset.actionMaps)
			foreach (var action in map.actions)
			{
				for (int binding = 0; binding < action.bindings.Count; binding++) RebindsLoad.NormalizeDriveBinding(action, binding);
				RebindsLoad.RepairDriveAxisPairs(action);
			}
		if (!keyboard) return;
		var profile = SplitScreenKeyboardProfile.Load(index);
		// Import Submit overrides saved before keyboard profiles had their own submitKey.
		string savedProfile = PlayerPrefs.GetString("SplitScreen.Keyboard." + index, "");
		if (!string.IsNullOrEmpty(json) && !savedProfile.Contains("\"submitKey\""))
		{
			var stored = JsonUtility.FromJson<SavedBindings>(json);
			var submit = asset.FindAction(F.I.enterRef.action.id.ToString());
			if (stored?.bindings != null && submit != null)
				foreach (var binding in submit.bindings)
				{
					if (binding.isComposite || !(binding.path?.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ?? false)) continue;
					var saved = stored.bindings.FirstOrDefault(entry => entry.id == binding.id.ToString());
					if (saved?.path?.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) == true)
					{ profile.submitKey = saved.path.Substring("<Keyboard>/".Length); break; }
				}
		}
		profile.Apply(asset);
	}
}

// Separate from menu bindings: each keyboard player owns a private gameplay asset.
[Serializable]
public sealed class SplitScreenKeyboardProfile
{
	public string[] keys;
	public string submitKey;
	static string DefaultSubmitKey(int index) => index == 0 ? "enter" : index.ToString();
	public static readonly string[] Labels = { "Accelerate", "Brake", "Left", "Right", "Stunts", "Boost", "Jump", "Roll left", "Roll right", "Horn", "Reset", "Look left", "Look right", "Look back", "Lights", "Camera", "Pause" };
	// Four disjoint regions of a standard keyboard; extra profiles start empty.
	static readonly string[][] Presets = {
		new[] { "upArrow", "downArrow", "leftArrow", "rightArrow", "rightShift", "rightCtrl", "rightAlt", "comma", "period", "slash", "backspace", "insert", "pageUp", "end", "home", "pageDown", "escape" },
		new[] { "w", "s", "a", "d", "leftShift", "leftCtrl", "space", "q", "e", "h", "r", "z", "c", "x", "f", "v", "tab" },
		new[] { "i", "k", "j", "l", "u", "o", "p", "n", "m", "semicolon", "y", "b", "minus", "equals", "t", "g", "backquote" },
		new[] { "numpad8", "numpad5", "numpad4", "numpad6", "numpad7", "numpad9", "numpad0", "numpad1", "numpad3", "numpadPlus", "numpadMinus", "numpadDivide", "numpadMultiply", "numpad2", "numpadPeriod", "numpadEnter", "numLock" }
	};
	static readonly string[][] LegacyPresets = {
		new[] { "upArrow", "downArrow", "leftArrow", "rightArrow", "rightShift", "rightCtrl", "rightAlt", "comma", "period", "slash", "backspace", "insert", "pageUp", "end", "home", "pageDown", "escape" },
		new[] { "w", "s", "a", "d", "leftShift", "leftCtrl", "space", "q", "e", "h", "r", "z", "c", "x", "f", "v", "escape" },
		new[] { "i", "k", "j", "l", "u", "o", "p", "n", "m", "semicolon", "y", "b", "slash", "period", "t", "g", "escape" },
		new[] { "numpad8", "numpad5", "numpad4", "numpad6", "numpad7", "numpad9", "numpad0", "numpad1", "numpad3", "numpadPlus", "numpadMinus", "insert", "pageUp", "end", "home", "numpadEnter", "escape" }
	};
	public static SplitScreenKeyboardProfile Load(int index)
	{
		var json = PlayerPrefs.GetString("SplitScreen.Keyboard." + index, "");
		if (!string.IsNullOrEmpty(json))
		{
			var saved = JsonUtility.FromJson<SplitScreenKeyboardProfile>(json);
			if (saved?.keys?.Length == Labels.Length)
			{
				// Upgrade only untouched old presets; preserve the user's custom keys.
				if (index >= 0 && index < LegacyPresets.Length && saved.keys.SequenceEqual(LegacyPresets[index]))
					saved.keys = (string[])Presets[index].Clone();
				else if (index >= Presets.Length && saved.keys.Take(16).All(string.IsNullOrEmpty) && saved.keys[16] == "escape")
					saved.keys[16] = "";
				if (saved.submitKey == null) saved.submitKey = DefaultSubmitKey(index);
				return saved;
			}
		}
		var profile = new SplitScreenKeyboardProfile { keys = new string[Labels.Length], submitKey = DefaultSubmitKey(index) };
		if (index >= 0 && index < Presets.Length) profile.keys = (string[])Presets[index].Clone();
		return profile;
	}
	public void Save(int index)
	{
		PlayerPrefs.SetString("SplitScreen.Keyboard." + index, JsonUtility.ToJson(this));
		PlayerPrefs.Save();
	}
	string Path(int index) => string.IsNullOrEmpty(keys[index]) ? "" : "<Keyboard>/" + keys[index];
	// Keep binding IDs intact so existing RebindActionUI buttons work for every set.
	public void Apply(InputActionAsset asset)
	{
		foreach (var map in asset.actionMaps)
			foreach (var action in map.actions)
				for (int i = 0; i < action.bindings.Count; i++)
				{
					var binding = action.bindings[i];
					if (!binding.isComposite && action.id == F.I.enterRef.action.id && IsKeyboardBinding(binding))
					{
						action.ApplyBindingOverride(i, new InputBinding { overridePath = string.IsNullOrEmpty(submitKey) ? "" : "<Keyboard>/" + submitKey, overrideProcessors = "", overrideInteractions = "" });
						continue;
					}
					int index = KeyIndex(action, binding);
					if (index < 0 || !IsKeyboardBinding(binding)) continue;
					action.ApplyBindingOverride(i, new InputBinding {
						overridePath = Path(index), overrideProcessors = "", overrideInteractions = ""
					});
				}
	}
	public void ReadBindings(InputActionAsset asset)
	{
		foreach (var map in asset.actionMaps)
			foreach (var action in map.actions)
				foreach (var binding in action.bindings)
				{
					if (!binding.isComposite && action.id == F.I.enterRef.action.id && IsKeyboardBinding(binding))
					{
						string submit = binding.effectivePath;
						if (string.IsNullOrEmpty(submit)) submitKey = "";
						else if (submit.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)) submitKey = submit.Substring("<Keyboard>/".Length);
						continue;
					}
					int index = KeyIndex(action, binding);
					if (index < 0 || !IsKeyboardBinding(binding)) continue;
					string path = binding.effectivePath;
					if (string.IsNullOrEmpty(path)) keys[index] = "";
					else if (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)) keys[index] = path.Substring("<Keyboard>/".Length);
				}
	}
	static bool IsKeyboardBinding(InputBinding binding) => !binding.isComposite &&
		((binding.path?.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) ?? false) ||
		 (binding.groups?.Split(';').Any(group => group == "Keyboard") ?? false));
	static int KeyIndex(InputAction action, InputBinding binding)
	{
		if (binding.isComposite) return -1;
		string part = binding.name?.ToLowerInvariant();
		if (action.id == F.I.driveRef.action.id)
			return part == "up" ? 0 : part == "down" ? 1 : part == "left" ? 2 : part == "right" ? 3 : -1;
		if (action.id == F.I.rollInput.action.id) return part == "negative" ? 7 : part == "positive" ? 8 : -1;
		if (action.id == F.I.lookAxisInput.action.id) return part == "negative" ? 11 : part == "positive" ? 12 : -1;
		if (action.id == F.I.evoInput.action.id) return 4;
		if (action.id == F.I.boostInput.action.id) return 5;
		if (action.id == F.I.altInputRef.action.id) return 6;
		if (action.id == F.I.honkInput.action.id) return 9;
		if (action.id == F.I.resetOnTrackInput.action.id) return 10;
		if (action.id == F.I.lookBackInput.action.id) return 13;
		if (action.id == F.I.lightsInput.action.id) return 14;
		if (action.name == "ChangeCamera") return 15;
		if (action.id == F.I.escRef.action.id) return 16;
		return -1;
	}
}
