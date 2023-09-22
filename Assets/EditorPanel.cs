using RVP;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorPanel : Sfxable
{
	static readonly int connectorRadius = 5;
	public enum Mode
	{
		None, Terrain, Build, Connect, Arrow,
		SetCamera
	}
	Mode mode = Mode.Build;
	public GameObject YouSurePanel;
	public Text trackName;
	public Sprite elementSprite;
	public Sprite selectedElSprite;
	public GameObject TilesMain;
	public SC_TerrainEditor terrainEditor;
	public FlyCamera flyCamera;
	public RaceManager raceManager;
	public GameObject savePanel;
	public TextMeshProUGUI trackNameInputField;
	public TextMeshProUGUI trackNameInputFieldPlaceholder;
	public TextMeshProUGUI trackDescInputField;
	public TextMeshProUGUI trackDescInputFieldPlaceholder;

	public Transform invisibleLevel;
	public Transform[] tileGroups;
	public GameObject arrowModel;
	GameObject arrow;
	public Vector3? placedConnector;
	public Vector3? floatingConnector;
	public GameObject cameraMenu;
	public GameObject cameraPrefab;
	public GameObject infoText;
	Vector3? lastEditorCameraPosition;
	Quaternion lastEditorCameraRotation;
	Dictionary<string, GameObject> cachedTiles = new Dictionary<string, GameObject>();
	GameObject currentTileButton;
	Transform currentTilesPanel;
	UITest uiTest;
	Coroutine hideCo;
	AnimationCurve buttonAnimationCurve = new AnimationCurve();
	Tile currentTile;
	Vector3? anchor;
	private bool pointingOnRoad;
	int tileRotation = 0;
	int xRotation = 0;
	bool mirrored;
	GameObject placedTilesContainer;
	GameObject racingLineContainer;
	private Vector3 curPosition;
	List<Connector> connectors = new List<Connector>();
	private bool selectingOtherConnector;
	private Coroutine closingPathCo;
	private Connector selectedConnector;
	private GameObject selectedCamera;
	private GameObject newCamera;

	void Awake()
	{
		uiTest = GetComponent<UITest>();
		currentTilesPanel = TilesMain.transform;
		if (Info.s_inEditor)
		{
			flyCamera.enabled = true;
		}
		buttonAnimationCurve.AddKey(new Keyframe(0, 1));
		buttonAnimationCurve.AddKey(new Keyframe(.25f, .8f));
		buttonAnimationCurve.AddKey(new Keyframe(.5f, 1));
	}
	void DeselectIconAndRemoveCurrentTile()
	{
		if (currentTile)
		{
			Destroy(currentTile.gameObject);
			if (currentTileButton != null)
				currentTileButton.GetComponent<Image>().sprite = elementSprite;
		}
	}
	void HideCurrentTile()
	{
		if (currentTile)
			currentTile.gameObject.SetActive(false);
	}
	void ShowCurrentTile()
	{
		if (currentTile)
			currentTile.gameObject.SetActive(true);
	}
	private void OnEnable()
	{
		flyCamera.enabled = true;
		if (lastEditorCameraPosition.HasValue)
		{
			flyCamera.transform.position = lastEditorCameraPosition.Value;
			flyCamera.transform.rotation = lastEditorCameraRotation;
		}
	}
	private void Start()
	{
		placedTilesContainer = new GameObject("placedTilesContainer");
		racingLineContainer = new GameObject("racingLineContainer");
	}
	float SetInvisibleLevelByScroll()
	{
		float scroll = Input.mouseScrollDelta.y;
		if (scroll != 0 && !Input.GetKey(KeyCode.LeftShift))
		{
			var p = invisibleLevel.position;
			if (scroll > 0)
				p.y += Input.GetKey(KeyCode.LeftShift) ? 5 : 1;
			else
				p.y -= Input.GetKey(KeyCode.LeftShift) ? 5 : 1;
			invisibleLevel.position = p;
		}
		return scroll;
	}
	IEnumerator DisplayMessageFor(string str, float timer)
	{
		infoText.SetActive(true);
		infoText.GetComponent<TextMeshProUGUI>().text = str;
		yield return new WaitForSeconds(timer);
		infoText.SetActive(false);
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			ShowYouSurePanel();
		}
		if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.S))
			QuickSave();
		switch (mode)
		{
			case Mode.Arrow:
				{
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
						1 << Info.roadLayer | 1 << Info.terrainLayer))
					{
						arrow.transform.position = hit.point;
						if (Input.GetMouseButtonDown(1))
						{
							var euler = arrow.transform.eulerAngles;
							int dir = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
							tileRotation = (tileRotation + dir * 45) % 360;
							euler.y = tileRotation;
							arrow.transform.rotation = Quaternion.Euler(euler);
						}
						if (Input.GetMouseButtonDown(0))
						{ // GO TO DRIVE VIEW
							lastEditorCameraPosition = flyCamera.transform.position;
							lastEditorCameraRotation = flyCamera.transform.rotation;
							var arrow_pos = arrow.transform.position;
							var arrow_rot = arrow.transform.rotation;
							flyCamera.enabled = false;
							SwitchTo(Mode.None);
							raceManager.StartFreeRoam(arrow_pos, arrow_rot);
							return;
						}
					}
				}
				break;
			case Mode.Build:
				{
					float scroll = SetInvisibleLevelByScroll();

					if (uiTest.PointerOverUI())
					{
						HideCurrentTile();

						if (Input.GetMouseButtonDown(1))
							SwitchCurrentPanelTo(TilesMain);

						if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
						{
							DeselectIconAndRemoveCurrentTile();
						}
					}
					else
					{ // MOUSE OVER THE EDITOR
						if (Input.GetKey(KeyCode.X))
						{ // REMOVING 
							HideCurrentTile();
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
							if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
								1 << Info.roadLayer))
							{
								pointingOnRoad = true;
								HideCurrentTile();
								if (Input.GetMouseButtonDown(0))
								{
									Destroy(hit.transform.parent.gameObject);
								}
							}
						}
						else
						{ // PLACING 
							ShowCurrentTile();

							if (currentTile)
							{
								if (Input.GetKeyDown(KeyCode.Q))
								{
									mirrored = currentTile.MirrorTile();
								}
								//debug_p = Camera.main.WorldToScreenPoint(currentTile.transform.position);
								if (Input.GetMouseButtonDown(0))
								{
									if (!pointingOnRoad)
									{ // PLACE TILE
										currentTile.GetComponent<Tile>().SetPlaced();
										InstantiateNewTile(currentTileButton.name);
										placedConnector = null;
										floatingConnector = null;
										anchor = null;
										return;
									}
								}
								if (Input.GetMouseButtonDown(1))
								{
									var euler = currentTile.transform.eulerAngles;
									int dir = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
									tileRotation = (tileRotation + dir * 45) % 360;
									euler.y = tileRotation;
									currentTile.transform.rotation = Quaternion.Euler(euler);
								}
								if (scroll != 0 && Input.GetKey(KeyCode.LeftShift))
								{
									var euler = currentTile.transform.eulerAngles;
									int dir = scroll > 0 ? -1 : 1;
									xRotation = (xRotation + dir * 5) % 90;
									euler.x = xRotation;
									currentTile.transform.rotation = Quaternion.Euler(euler);
								}

								Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
								RaycastHit hit;

								if (Input.GetKey(KeyCode.LeftControl))
								{ // pick height
									if (Physics.Raycast(ray, out hit, Mathf.Infinity,
										1 << Info.roadLayer | 1 << Info.terrainLayer))
									{
										var p = invisibleLevel.position;
										p.y = hit.point.y;
										invisibleLevel.position = p;
									}
								}
								if (Input.GetKey(KeyCode.LeftAlt))
								{ // pick tile
									HideCurrentTile();
									if (Physics.Raycast(ray, out hit, Mathf.Infinity,
										1 << Info.roadLayer))
									{
										var pickedTile = hit.transform.GetComponent<Tile>();
										mirrored = pickedTile.mirrored;
										SetCurrentTileTo(GetButtonForTile(pickedTile.transform.name));
										currentTile.transform.rotation = pickedTile.transform.rotation;
									}
								}
								else if (Physics.Raycast(ray, out hit, Mathf.Infinity,
									1 << Info.invisibleLevelLayer | 1 << Info.roadLayer | 1 << Info.terrainLayer))
								{
									curPosition = hit.point;
									if (hit.transform.gameObject.layer == Info.roadLayer)
									{
										pointingOnRoad = true;
									}
									else
									{
										pointingOnRoad = false;
										if (anchor == null && placedConnector != null && floatingConnector != null)
										{
											currentTile.transform.position += placedConnector.Value - floatingConnector.Value;
											//Debug.DrawRay(currentTile.transform.position, placedConnector.Value - floatingConnector.Value, Color.red, 1);
											//Debug.Break();
											anchor = currentTile.transform.position;
											//var p = invisibleLevel.position;
											//p.y = placedConnector.Value.y;
											//invisibleLevel.position = p;
										}
										else
										{
											if (anchor.HasValue)
											{
												if ((placedConnector == null && floatingConnector == null)
												|| (anchor.HasValue && Vector3.Distance(hit.point, anchor.Value) > connectorRadius))
												{
													anchor = null;
													currentTile.transform.position = hit.point;
												}
											}
											else
												currentTile.transform.position = hit.point;
										}
									}
								}
							}
						}
					}
				}
				break;
			case Mode.Connect:
				{
					if (selectingOtherConnector)
					{
						if (Input.GetMouseButtonDown(0))
						{
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
							if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1 << Info.connectorLayer))
							{
								var c = hit.transform.GetComponent<Connector>();
								if (!c.visible)
								{
									selectedConnector = c;
								}
							}
						}
					}
				}
				break;
			case Mode.SetCamera:
				{
					if (Input.GetMouseButtonDown(0))
					{
						Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
						if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1 << Info.cameraLayer | 1 << Info.connectorLayer))
						{
							if (hit.transform.gameObject.layer == Info.cameraLayer)
							{
								if (Input.GetKey(KeyCode.X))
									Destroy(hit.transform.gameObject);
								else
								{
									selectedCamera = hit.transform.gameObject;
									StartCoroutine(DisplayMessageFor("Selected", 1));
								}
							}
							else if (selectedCamera != null)
							{
								hit.transform.gameObject.GetComponent<Connector>().SetCamera(
									selectedCamera.GetComponent<TrackCamera>());
							}
						}
					}
				}
				break;
			default:
				break;
		}
	}
	public void AddTrackCamera()
	{
		selectedCamera = Instantiate(cameraPrefab);
		selectedCamera.transform.position = flyCamera.transform.position;
	}

	IEnumerator ClosingPath()
	{
		connectors.Clear();
		List<Vector3> Lpath = new List<Vector3>(100);
		List<Vector3> Rpath = new List<Vector3>(100);
		Transform startline = null;
		for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
		{
			if (placedTilesContainer.transform.GetChild(i).name == "startline")
			{
				startline = placedTilesContainer.transform.GetChild(i);
				break;
			}
		}
		if (startline == null)
		{
			Debug.Log("No startline");
			yield break;
		}
		// get connector-B of startline

		Connector begin = startline.GetChild(2).GetComponent<Connector>();
		Connector cur = begin;
		{
			int i = 0;
			while (cur != null && i < 10000)
			{
				if (cur.connection == null)
				{
					if (selectedConnector)
					{
						cur.Show(Connector.green);
						cur.connection = selectedConnector;
						selectedConnector = null;
						selectingOtherConnector = false;
					}
					else
					{
						cur.Show(Connector.red);
						connectors.Add(cur);
						selectingOtherConnector = true;
						yield return null;
					}
				}
				if (cur.connection)
				{
					if (!cur.visible)
						connectors.Add(cur);
					cur = cur.connection; // now on the other tile

					cur.Paths(out var lpath, out var rpath);
					Lpath.AddRange(lpath);
					Rpath.AddRange(rpath);
					cur.Show(Connector.green);
					connectors.Add(cur);
					cur = cur.Opposite(); // now on the opposite side of the tile
					if (cur == begin)
					{
						break;
					}
					i++;
				}
			}

			Debug.Log("elements traversed: " + i);
			if (i > 100 || i < 2)
			{
				yield break;
			}
		}
		//for(int i=0; i<Lpath.Count; ++i)
		//{
		//	GameObject L = new GameObject();
		//	L.transform.position = Lpath[i];
		//	L.transform.parent = racingLineContainer.transform;
		//	L.name = "L" + i.ToString();
		//	GameObject R = new GameObject();
		//	R.transform.position = Rpath[i];
		//	R.transform.parent = racingLineContainer.transform;
		//	R.name = "R" + i.ToString();
		//}
		K1999 k1999 = new K1999();
		k1999.LoadData(Lpath, Rpath);
		k1999.CalcRaceLine();
		Vector4[] racingLine = k1999.GetRacingLine(Lpath, Rpath);

		foreach (var pos in racingLine)
		{
			GameObject r = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			r.transform.position = new Vector3(pos.x, pos.y, pos.z);
			r.transform.parent = racingLineContainer.transform;
			r.GetComponent<MeshRenderer>().material.color = new Color32((byte)(255 * pos.w), 255, 255, 255);
			Debug.Log(pos.w);
		}
	}
	void ClearConnectors()
	{
		selectedConnector = null;
		selectingOtherConnector = false;
		if (closingPathCo != null)
			StopCoroutine(closingPathCo);
		foreach (var c in connectors)
		{
			if (c.visible)
				c.Hide();
		}
		connectors.Clear();
	}
	GameObject GetButtonForTile(in string tileName)
	{
		foreach (var group in tileGroups)
		{
			for (int i = 0; i < group.childCount; ++i)
			{
				if (group.GetChild(i).name == tileName)
					return group.GetChild(i).gameObject;
			}
		}
		Debug.LogError("No tile named:" + tileName);
		return null;
	}
	public void SetCurrentTileTo(GameObject button)
	{
		DeselectIconAndRemoveCurrentTile();
		if (currentTileButton)
			currentTileButton.transform.parent.gameObject.SetActive(false);
		currentTileButton = button;

		currentTileButton.GetComponent<Image>().sprite = selectedElSprite;
		currentTileButton.transform.parent.gameObject.SetActive(true);
		StartCoroutine(AnimateButton(button.transform.GetChild(0)));
		//PlaySFX("click");
		InstantiateNewTile(button.name);
	}
	void InstantiateNewTile(string tileName)
	{
		GameObject original;
		if (cachedTiles.ContainsKey(tileName))
		{
			original = cachedTiles[tileName];
		}
		else
		{
			original = Resources.Load<GameObject>(Info.editorTilesPath + tileName);
			cachedTiles.Add(tileName, original);
		}

		var rot = Quaternion.Euler(new Vector3(xRotation, tileRotation, 0));
		currentTile = Instantiate(original, curPosition, rot, placedTilesContainer.transform)
			.transform.GetComponent<Tile>();
		currentTile.GetComponent<Tile>().panel = this;
		if (mirrored)
			currentTile.MirrorTile();
		currentTile.name = tileName;
	}

	public void SwitchTo(Mode mode)
	{
		if (this.mode == mode && this.mode != Mode.None && this.mode != Mode.Connect)
		{
			SwitchTo(Mode.None);
		}
		else
		{
			switch (this.mode)
			{ // close current mode
				case Mode.None:
					break;
				case Mode.Terrain:
					terrainEditor.enabled = false;
					break;
				case Mode.Build:
					currentTilesPanel.gameObject.SetActive(false);
					break;
				case Mode.Connect:
					ClearConnectors();
					break;
				case Mode.Arrow:
					if (arrow)
						Destroy(arrow);
					break;
				case Mode.SetCamera:
					selectedCamera = null;
					if (newCamera)
						Destroy(newCamera.gameObject);
					cameraMenu.gameObject.SetActive(false);
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.cameraLayer);
					break;
				default:
					break;
			}
			switch (mode)
			{ // open new mode
				case Mode.SetCamera:
					cameraMenu.gameObject.SetActive(true);
					flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.cameraLayer;
					break;
				case Mode.Terrain:
					terrainEditor.enabled = true;
					break;
				case Mode.Build:
					currentTilesPanel.gameObject.SetActive(true);
					break;
				case Mode.Connect:
					closingPathCo = StartCoroutine(ClosingPath());
					break;
				case Mode.Arrow:
					arrow = Instantiate(arrowModel);
					break;
				case Mode.None:
					break;
			}
			this.mode = mode;
		}
	}

	public void SwitchToTerrainEdit()
	{
		SwitchTo(Mode.Terrain);
	}
	public void SwitchToBuild()
	{
		SwitchTo(Mode.Build);
	}
	public void SwitchToConnect()
	{
		SwitchTo(Mode.Connect);
	}
	public void SwitchToCamera()
	{
		SwitchTo(Mode.SetCamera);
	}
	public void ToggleSavePanel()
	{
		savePanel.SetActive(!savePanel.activeSelf);
		if (savePanel.activeSelf)
			SwitchTo(Mode.None);
	}

	IEnumerator AnimateButton(Transform button)
	{
		float timer = 0;
		for (int i = 0; i < 10000; ++i)
		{
			if (timer > 0.5f)
				yield break;
			button.localScale = buttonAnimationCurve.Evaluate(timer) * Vector3.one;
			timer += Time.deltaTime;
			yield return null;
		}
	}
	public void SwitchCurrentPanelTo(GameObject panel)
	{
		currentTilesPanel.gameObject.SetActive(false);
		currentTilesPanel = panel.transform;
		panel.SetActive(true);
	}
	public void CloseEditor()
	{
		HidePanel();
	}
	public void QuickSave()
	{
		string name = trackName.text;
		if (name[0] == '*')
			trackName.text = trackName.text[1..];
	}
	//public void ShowSavePanel()
	//{
	//	string[] originalpath = StandaloneFileBrowser.OpenFolderPanel(
	//		"Select folder to save this track in ..", Info.LoadLastFolderPath(), false);
	//	string path = originalpath[0];
	//	if (SaveTrack(path))
	//	{
	//		Info.SaveLastFolderPath(path);
	//	}
	//}\

	// Stunty - presence of driveable barriers
	// Loop - presence of >0 loops
	// Jumpy - presence of >0 jump
	// Windy - presence of 6 consequtive turn tiles
	// Intersecting - presence of >4 crossings
	// No_pit
	// No_jumps
	// Icy - road with icy surface
	// Sandy - road with sandy surface
	// Offroad - drive through terrain
	// Road diff = length*coeff +(No_pit?2)*(No_jumps 
	private bool SaveTrack()
	{
		// 1. txt:
		// nazwa+opis
		// flagi - 
		// lista nazw u¿ytych tilesów
		// lista wszystkich tilesów (id, pozycja, rotacja)
		// lista kamer
		// idealne linie (0,20,40,80,100)
		// zdjêcie
		// heightmap
		return false;
	}
	public void ToFreeRoamButton()
	{
		SwitchTo(Mode.Arrow);
	}
	public void ToValidate()
	{

	}
	public void HidePanel()
	{
		F.PlaySlideOutOnChildren(YouSurePanel.transform);
		if (hideCo != null)
			StopCoroutine(hideCo);
		hideCo = StartCoroutine(HidePanelIn(YouSurePanel, 0.6f));
	}
	IEnumerator HidePanelIn(GameObject panel, float timer)
	{
		for (int i = 0; i < 10000; ++i)
		{
			if (timer < 0)
			{
				panel.SetActive(false);
				yield break;
			}
			timer -= Time.deltaTime;
			yield return null;
		}
	}
	public void ShowYouSurePanel()
	{
		if (!YouSurePanel.activeSelf)
			YouSurePanel.SetActive(true);
		YouSurePanel.transform.GetChild(1).GetComponent<MainMenuButton>().Select();
	}
}
