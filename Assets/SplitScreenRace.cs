using System;
using System.Collections.Generic;
using System.Linq;
using RVP;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Local players share menus and results; only the running race has viewports.
public sealed class SplitScreenRace : MonoBehaviour
{
	public const int MaxPlayers = 10;
	public static SplitScreenRace I { get; private set; }
	public VehicleParent[] Cars { get; private set; } = Array.Empty<VehicleParent>();
	CameraControl[] cameras;
	SGP_HUD[] huds;
	readonly List<GameObject> ownedObjects = new();
	readonly List<(Transform child, Transform parent, int sibling)> sharedHudElements = new();
	RectTransform[] viewports;
	int screenWidth, screenHeight;
	Vector2 viewportSize;
	RaceManager race;
	Transform hudParent;
	int hudSibling;
	Vector2 hudAnchorMin, hudAnchorMax, hudSize, hudPosition, hudPivot;
	Vector2 hudLayoutSize;
	Vector3 hudScale;
	Rect cameraRect;
	int cameraMask;
	bool ending;
	public static InputDevice[] AvailableDevices() => InputSystem.devices
		.Where(device => device is Keyboard || device is Gamepad || device is Joystick).ToArray();
	public static bool ValidatePlayers(out string error)
	{
		error = "";
		var players = F.I.splitScreenPlayers;
		if (players.Count < 2 || players.Count > MaxPlayers) { error = "Select 2-10 players"; return false; }
		var used = new HashSet<int>();
		var keyboards = new HashSet<string>();
		foreach (var player in players)
		{
			if (player.car < 0 || player.car >= F.I.cars.Length) { error = "Select a vehicle for each player"; return false; }
			var device = InputSystem.GetDeviceById(player.deviceId);
			if (device == null || !device.added || !(device is Keyboard || device is Gamepad || device is Joystick))
			{ error = "Assign a connected keyboard or gamepad to each player"; return false; }
			if (device is Keyboard)
			{
				if (player.keyboardProfile < 0 || player.keyboardProfile >= MaxPlayers ||
					!keyboards.Add(device.deviceId + ":" + player.keyboardProfile))
				{ error = "Select a different keyboard profile for each keyboard player"; return false; }
			}
			else if (!used.Add(device.deviceId)) { error = "Assign a different gamepad to each player"; return false; }
		}
		return true;
	}
	public bool Prepare(RaceManager manager)
	{
		if (!ValidatePlayers(out string error)) { Debug.LogWarning(error); return false; }
		I = this;
		race = manager;
		ending = false;
		int count = F.I.splitScreenPlayers.Count;
		Cars = new VehicleParent[count]; cameras = new CameraControl[count]; huds = new SGP_HUD[count];
		var menuInput = GameObject.Find("Canvas").GetComponent<PlayerInput>();
		F.I.playerInputs = new PlayerInput[MaxPlayers];
		cameraRect = race.cam.GetComponent<Camera>().rect;
		cameraMask = race.cam.GetComponent<Camera>().cullingMask;
		viewports = new RectTransform[count];
		var hudRect = (RectTransform)race.hud.transform;
		Canvas.ForceUpdateCanvases();
		hudLayoutSize = hudRect.rect.size;
		hudParent = hudRect.parent; hudSibling = hudRect.GetSiblingIndex();
		hudAnchorMin = hudRect.anchorMin; hudAnchorMax = hudRect.anchorMax;
		hudSize = hudRect.sizeDelta; hudPosition = hudRect.anchoredPosition; hudPivot = hudRect.pivot; hudScale = hudRect.localScale;
		// These animations/messages are siblings of SGPHUD in Demo. Move them
		// into the template so Unity remaps every clone's serialized references.
		foreach (var child in new[] { race.hud.infoText.transform, race.hud.lapRecordSeq.transform,
			race.hud.ptsAnim.transform, race.hud.StuntInfo.transform }.Distinct())
			if (!child.IsChildOf(hudRect)) sharedHudElements.Add((child, child.parent, child.GetSiblingIndex()));
		foreach (var item in sharedHudElements) item.child.SetParent(hudRect, false);
		var inactive = new GameObject("Split screen construction"); inactive.SetActive(false);
		var templateRollers = race.hud.GetComponentsInChildren<Roller>(true);
		// Clone the untouched HUD before connecting any cars or adding progress icons.
		for (int i = 0; i < count; i++)
		{
			var inputObject = new GameObject("PlayerInput " + (i + 1)); inputObject.SetActive(false);
			inputObject.transform.SetParent(transform, false); ownedObjects.Add(inputObject);
			var input = inputObject.AddComponent<PlayerInput>();
			input.actions = Instantiate(menuInput.actions);
			input.actions.Disable();
			RemapView.ApplyInputSet(input.actions, F.I.splitScreenPlayers[i].keyboardProfile,
				InputSystem.GetDeviceById(F.I.splitScreenPlayers[i].deviceId) is Keyboard);
			input.actions.FindAction(F.I.driveRef.action.id.ToString()).wantsInitialStateCheck = true;
			input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
			input.defaultActionMap = F.I.driveRef.action.actionMap.name;
			input.neverAutoSwitchControlSchemes = true;
			inputObject.SetActive(true);
			InputDevice device = InputSystem.GetDeviceById(F.I.splitScreenPlayers[i].deviceId);
			string scheme = input.actions.controlSchemes.First(s => s.SupportsDevice(device)).name;
			if (device is Keyboard && Mouse.current != null) input.SwitchCurrentControlScheme(scheme, device, Mouse.current);
			else input.SwitchCurrentControlScheme(scheme, device);
			input.ActivateInput(); F.I.playerInputs[i] = input;
			cameras[i] = i == 0 ? race.cam : Instantiate(race.cam.gameObject, inactive.transform).GetComponent<CameraControl>();
			if (i > 0)
			{
				cameras[i].gameObject.name = "Player camera " + (i + 1);
				cameras[i].gameObject.tag = "Untagged";
				cameras[i].GetComponent<AudioListener>().enabled = false;
				cameras[i].transform.SetParent(transform, true); ownedObjects.Add(cameras[i].gameObject);
			}
			Camera camera = cameras[i].GetComponent<Camera>();
			camera.rect = Viewport(i, count);
			input.camera = camera;
			huds[i] = i == 0 ? race.hud : Instantiate(race.hud.gameObject, inactive.transform).GetComponent<SGP_HUD>();
			if (i > 0)
			{
				var clonedRollers = huds[i].GetComponentsInChildren<Roller>(true);
				for (int roller = 0; roller < templateRollers.Length; roller++)
					clonedRollers[roller].InitializeClone(templateRollers[roller]);
			}
			huds[i].gameObject.SetActive(false);
			huds[i].primaryHud = i == 0; huds[i].raceManager = race;
			var canvasObject = new GameObject("Player HUD " + (i + 1), typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
			// Keep each overlay canvas at the scene root, outside the main menu canvas.
			ownedObjects.Add(canvasObject);
			var canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -1;
			var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			var originalScaler = hudParent.GetComponentInParent<CanvasScaler>();
			scaler.referenceResolution = originalScaler ? originalScaler.referenceResolution : new Vector2(1920, 1080);
			scaler.matchWidthOrHeight = 0.5f;
			var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
			viewport.SetParent(canvasObject.transform, false); viewport.anchorMin = camera.rect.min; viewport.anchorMax = camera.rect.max;
			viewport.offsetMin = viewport.offsetMax = Vector2.zero; viewports[i] = viewport;
			var rect = (RectTransform)huds[i].transform; rect.SetParent(viewport, false);
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero;
			// Keep the original layout dimensions; fit the whole HUD by scaling it uniformly.
			rect.sizeDelta = hudLayoutSize;
		}
		Canvas.ForceUpdateCanvases(); ResizeHuds(); Destroy(inactive); SGP_HUD.I = race.hud;
		return true;
	}
	static Rect Viewport(int index, int count)
	{
		if (count == 2) return new Rect(0, index == 0 ? 0.5f : 0, 1, 0.5f);
		if (count == 3 && index == 0) return new Rect(0, 0.5f, 1, 0.5f);
		if (count == 3) return new Rect((index - 1) * 0.5f, 0, 0.5f, 0.5f);
		// Balance rows and use the whole screen, including an incomplete final row.
		int rows = count <= 6 ? 2 : 3;
		int columns = Mathf.CeilToInt(count / (float)rows);
		int row = index / columns;
		int inRow = Mathf.Min(columns, count - row * columns);
		return new Rect((index % columns) / (float)inRow, 1 - (row + 1f) / rows, 1f / inRow, 1f / rows);
	}
	public void Bind(VehicleParent car)
	{
		int index = car.localPlayerIndex;
		if (index < 0 || index >= Cars.Length) return;
		Cars[index] = car; car.localCamera = cameras[index]; car.localHud = huds[index];
		car.basicInput.playerInput = F.I.playerInputs[index];
		car.followAI.SetCPU(false);
		if (index == 0) race.playerCar = car;
		huds[index].gameObject.SetActive(true); // Initialize cloned InfoText/Awake before Connect resets it.
		cameras[index].Connect(car); huds[index].Connect(car);
	}
	public void AddProgress(VehicleParent car) { if (huds != null) foreach (var hud in huds) hud.AddToProgressBar(car); }
	public void BroadcastRaceMessage(Message message)
	{
		if (huds == null) return;
		foreach (var hud in huds)
			if (hud && hud.infoText) hud.infoText.AddMessage(new Message(message.text, message.type));
	}
	public void RemoveProgress(VehicleParent car) { if (huds != null) foreach (var hud in huds) if (hud) hud.RemoveFromProgressBar(car); }
	void Update()
	{
		if (huds != null && (Screen.width != screenWidth || Screen.height != screenHeight ||
			(viewports[0].rect.size - viewportSize).sqrMagnitude > .01f)) ResizeHuds();
		if (ending || Cars.Length == 0 || Cars.Any(car => !car)) return;
		foreach (var car in Cars)
			if (car.originalVehiclePhysics == null) return;
		if (Cars.Any(car => car.raceBox.enabled)) return;
		ending = true;
		race.hud.endraceTimer.gameObject.SetActive(false);
		foreach (var hud in huds) hud.gameObject.SetActive(false);
		for (int i = 1; i < cameras.Length; i++)
		{
			cameras[i].enabled = false; cameras[i].GetComponent<Camera>().enabled = false;
		}
		cameras[0].GetComponent<Camera>().rect = cameraRect;
		race.PlayFinishSeq();
	}
	void ResizeHuds()
	{
		screenWidth = Screen.width; screenHeight = Screen.height;
		viewportSize = viewports[0].rect.size;
		for (int i = 0; i < huds.Length; i++)
		{
			Vector2 available = viewports[i].rect.size;
			float width = Mathf.Max(.001f, hudLayoutSize.x * Mathf.Abs(hudScale.x));
			float height = Mathf.Max(.001f, hudLayoutSize.y * Mathf.Abs(hudScale.y));
			float scale = Mathf.Min(available.x / width, available.y / height);
			// Use some of the HUD's empty margins on compact viewports, retaining its layout.
			float aspect = available.x / Mathf.Max(.001f, available.y);
			float compactness = Mathf.InverseLerp(16f / 9f, 1f, aspect);
			scale *= Mathf.Lerp(1f, 1.33f, compactness);
			var rect = (RectTransform)huds[i].transform;
			rect.anchoredPosition = Vector2.zero;
			rect.localScale = hudScale * scale;
		}
	}
	public void Cleanup()
	{
		if (huds == null) { if (I == this) I = null; return; }
		if (race && hudParent)
		{
			foreach (var hud in huds) if (hud) { hud.Disconnect(); hud.gameObject.SetActive(false); }
			var rect = (RectTransform)race.hud.transform; rect.SetParent(hudParent, false); rect.SetSiblingIndex(hudSibling);
			rect.anchorMin = hudAnchorMin; rect.anchorMax = hudAnchorMax; rect.pivot = hudPivot;
			rect.sizeDelta = hudSize; rect.anchoredPosition = hudPosition; rect.localScale = hudScale;
			foreach (var item in sharedHudElements) if (item.child && item.parent) { item.child.SetParent(item.parent, false); item.child.SetSiblingIndex(item.sibling); }
			race.cam.GetComponent<Camera>().rect = cameraRect; race.cam.GetComponent<Camera>().cullingMask = cameraMask;
			SGP_HUD.I = race.hud;
		}
		foreach (var input in F.I.playerInputs) if (input) { input.DeactivateInput(); Destroy(input.actions); }
		foreach (var obj in ownedObjects) if (obj) { obj.SetActive(false); Destroy(obj); }
		F.I.playerInputs = new PlayerInput[MaxPlayers];
		ownedObjects.Clear(); sharedHudElements.Clear(); Cars = Array.Empty<VehicleParent>(); huds = null; cameras = null;
		if (I == this) I = null;
	}
}
