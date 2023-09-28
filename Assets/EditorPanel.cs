using RVP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SlideInOut;

public class EditorPanel : Sfxable
{
	const int connectorRadius = 5;
	public class Scalator
	{
		public Connector a, b;
		public float distance = 0;
		public void Reset(bool resetDistance = false)
		{
			if (a != null)
				a.Hide();
			if (b != null)
				b.Hide();
			a = b = null;
			if(resetDistance)
				distance = 0;
		}
	}

	public enum Mode
	{
		None, Terrain, Build, Connect, Arrow,
		SetCamera,
		Scalator
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
	public GameObject tileGroups;
	public GameObject arrowModel;
	GameObject arrow;
	public Vector3? placedConnector;
	public Vector3? floatingConnector;
	public bool d_placed;
	public bool d_floating;
	public bool d_anchor;
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
	int yRot = 0;
	int xRot = 0;
	int zRot = 0;
	Scalator scalator = new Scalator();
	bool mirrored;
	GameObject placedTilesContainer;
	GameObject racingLineContainer;
	Vector3 curPosition;
	List<Connector> connectors = new List<Connector>();
	bool selectingOtherConnector;
	Coroutine closingPathCo;
	Coroutine DisplayCo;
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
		if (scroll != 0)
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
	void DisplayMessageFor(string str, float timer)
	{
		if (DisplayCo != null)
			StopCoroutine(DisplayCo);
		DisplayCo = StartCoroutine(DisplayMessage(str,timer));
	}
	IEnumerator DisplayMessage(string str, float timer)
	{
		infoText.SetActive(true);
		infoText.GetComponent<TextMeshProUGUI>().text = str;
		yield return new WaitForSeconds(timer);
		infoText.SetActive(false);
	}
	void RotateCurrentTileAround(in Vector3 axis, float angle)
	{
		currentTile.transform.RotateAround(currentTile.transform.position, axis, angle);
	}
	void Update()
	{
		d_placed = placedConnector.HasValue;
		d_floating = floatingConnector.HasValue;
		d_anchor = anchor.HasValue;
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
							yRot = (yRot + dir * 45) % 360;
							euler.y = yRot;
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
					if(Input.GetKeyDown(KeyCode.Alpha1))
					{
						scalator.Reset(true);
						if(currentTile)
						{
							Destroy(currentTile.gameObject);
							InstantiateNewTile(currentTileButton.name);
						}
					}	
					float scroll = SetInvisibleLevelByScroll();

					
					if (scroll != 0 && Input.GetKey(KeyCode.Tab))
					{
						int dir = scroll > 0 ? -1 : 1;
						scalator.distance = Mathf.Clamp(scalator.distance + dir * 2.5f, 10, 200);
						if(currentTile)
						{
							currentTile.AdjustScale(scalator.distance);
						}
						DisplayMessageFor(scalator.distance.ToString("F1"), 1);
					}

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
						if (Input.GetKeyDown(KeyCode.LeftAlt))
						{ // pick tile
							HideCurrentTile();
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
							if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
								1 << Info.roadLayer))
							{
								var pickedTile = hit.transform.GetComponent<Tile>();
								if (pickedTile == null)
									pickedTile = hit.transform.parent.GetComponent<Tile>();
								mirrored = pickedTile.mirrored;
								if(Input.GetKey(KeyCode.LeftShift))
									scalator.distance = pickedTile.Length();
								SetCurrentTileTo(GetButtonForTile(pickedTile.transform.name));
								currentTile.transform.rotation = pickedTile.transform.rotation;
								invisibleLevel.position = new Vector3(0,pickedTile.transform.position.y, 0);
							}
						}
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
									if (hit.transform.gameObject.GetComponent<Tile>() == null)
										Destroy(hit.transform.parent.gameObject);
									else
										Destroy(hit.transform.gameObject);
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
								if(Input.GetKey(KeyCode.Z) && Input.GetKey(KeyCode.C))
								{ // normalize rotation
									currentTile.transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
								}
								RaycastHit hit;
								if (Input.GetMouseButtonDown(1))
								{ // Y ROTATION
									int dir = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
									if(Input.GetKey(KeyCode.Tab))
									{
										RotateCurrentTileAround(currentTile.transform.up, dir * 45);
									}
									else
									{
										RotateCurrentTileAround(Vector3.up, dir * 45);
										yRot = (yRot + dir * 45) % 360;
									}
								}
								if (scroll != 0 && (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.C)))
								{ // YAW, ROLL ROTATIONS
									int dir = scroll > 0 ? -1 : 1;
									if(Input.GetKey(KeyCode.Z))
									{
										RotateCurrentTileAround(currentTile.transform.right, dir * 5);
										//euler.x = ((currentTile.transform.GetComponent<MeshFilter>() == null) ? 0 : -90) + xRot;
										xRot = (xRot + dir * 5) % 360;
										DisplayMessageFor(xRot.ToString(), 1);
									}
									else if(Input.GetKey(KeyCode.C))
									{
										zRot = (zRot + dir * 5) % 360;
										RotateCurrentTileAround(currentTile.transform.forward, dir * 5);
										DisplayMessageFor(zRot.ToString(), 1);
									}
								}
								Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
								
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
								
								if (Physics.Raycast(ray, out hit, Mathf.Infinity,
									1 << Info.invisibleLevelLayer | 1 << Info.roadLayer | 1 << Info.terrainLayer))
								{
									curPosition = hit.point;
									if (hit.transform.gameObject.layer == Info.roadLayer
										&& currentTile.transform.childCount > 0)
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
						flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.connectorLayer;
							
						if (Input.GetMouseButtonDown(0))
						{
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
							if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1 << Info.connectorLayer))
							{
								var c = hit.transform.GetComponent<Connector>();
								if (!c.marked)
								{
									selectedConnector = c;
									flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);
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
									flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.connectorLayer;
									DisplayMessageFor("Selected", 1);
								}
							}
							else if (selectedCamera != null)
							{
								hit.transform.gameObject.GetComponent<Connector>().SetCamera(
									selectedCamera.GetComponent<TrackCamera>());
								flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.cameraLayer);
							}
						}
					}
				}
				break;
			case Mode.Scalator:
				{
					if (scalator.distance == 0) // searching to add connectors
					{
						Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
						if (Physics.Raycast(ray, out _, Mathf.Infinity, 1 << Info.roadLayer))
						{
							flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.connectorLayer;
							if(Input.GetMouseButtonDown(0))
							{
								if (Physics.Raycast(ray, out RaycastHit hit2, Mathf.Infinity, 1 << Info.connectorLayer))
								{
									var c = hit2.transform.GetComponent<Connector>();
									if(!c.marked)
									{
										c.Colorize(Connector.red);

										if (scalator.a == null)
										{
											scalator.a = c;
										}
										else
										{
											scalator.b = c;
											scalator.distance = Vector3.Distance(scalator.a.transform.position, scalator.b.transform.position);
											DisplayMessageFor("Measured", 1);
											SwitchTo(Mode.Build);
										}
									}
								}
							}
						}
						else
						{
							flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);
						}
					}
					else
					{
						scalator.Reset(true);
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
						cur.Colorize(Connector.green);
						cur.connection = selectedConnector;
						selectedConnector = null;
						selectingOtherConnector = false;
					}
					else
					{
						cur.Colorize(Connector.red);
						connectors.Add(cur);
						selectingOtherConnector = true;
						yield return null;
					}
				}
				if (cur.connection)
				{
					if (!cur.marked)
					{
						connectors.Add(cur);
						cur.Colorize(Connector.green);
					}
					cur = cur.connection; // now on the other tile

					cur.Paths(out var lpath, out var rpath);
					Lpath.AddRange(lpath);
					Rpath.AddRange(rpath);
					cur.Colorize(Connector.green);
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
			if (c.marked)
				c.Hide();
		}
		connectors.Clear();
	}
	GameObject GetButtonForTile(in string tileName)
	{

		for(int j=0; j<tileGroups.transform.childCount; ++j)
		{
			var group = tileGroups.transform.GetChild(j);
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
		SwitchCurrentPanelTo(currentTileButton.transform.parent.gameObject);
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
		currentTile = Instantiate(original, placedTilesContainer.transform)
			.transform.GetComponent<Tile>();
		currentTile.transform.position = curPosition;
		var rot = Quaternion.Euler(new Vector3(
			((currentTile.transform.GetComponent<MeshFilter>() != null) ? -90 : 0) + xRot, yRot, zRot));
		currentTile.transform.rotation = rot;
		currentTile.GetComponent<Tile>().panel = this;
		if (mirrored)
			currentTile.MirrorTile();

		currentTile.AdjustScale(scalator.distance);
		
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
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);
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
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer | 1<<Info.cameraLayer);
					break;
				case Mode.Scalator:
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);
					scalator.Reset();
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
				case Mode.Scalator:
					scalator.Reset();
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
	public void SwitchToScalator()
	{
		SwitchTo(Mode.Scalator);
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
