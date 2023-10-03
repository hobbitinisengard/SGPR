using PathCreation;
using RVP;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
		Scalator,
		FillTool,
		StuntZonesTool
	}
	public Mode mode { get; private set; }
	public Material renderTextureMat;
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
	public TextMeshProUGUI trackAuthorInputField;
	public TextMeshProUGUI trackAuthorInputFieldPlaceholder;
	public TextMeshProUGUI trackDifficultyInputField;
	public TextMeshProUGUI trackDifficultyInputFieldPlaceholder;
	public TMP_Dropdown carGroupDropdown;
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
	public GameObject fillMenu;
	public PathCreator pathCreator;
	public GameObject replayCamerasContainer;
	public Image connectButtonImage;
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
	private Transform selectedFlag;
	List<Vector3> Lpath = new List<Vector3>(100);
	List<Vector3> Rpath = new List<Vector3>(100);
	Vector3 windExternal, windRandom;
	void Awake()
	{
		mode = Mode.Build;
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
	async void Update()
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
						if (currentTile)
						{

							Destroy(currentTile.gameObject);
							InstantiateNewTile(currentTileButton.name);
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
									InvalidateConnections();
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
										InvalidateConnections();
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
										xRot = (xRot + dir * 5) % 360;
										if(xRot % 90 == 0)
										{
											xRot = 0;
											var euler = currentTile.transform.eulerAngles;
											euler.x = ((currentTile.transform.GetComponent<MeshFilter>() == null) ? 0 : -90) + xRot;
											currentTile.transform.rotation = Quaternion.Euler(euler);
										}
										else
										{
											RotateCurrentTileAround(currentTile.transform.right, dir * 5);
										}
										DisplayMessageFor(xRot.ToString(), 1);
									}
									else if(Input.GetKey(KeyCode.C))
									{
										zRot = (zRot + dir * 5) % 360;
										if (zRot % 90 == 0)
										{
											zRot = 0;
											var euler = currentTile.transform.eulerAngles;
											euler.z = zRot;
											currentTile.transform.rotation = Quaternion.Euler(euler);
										}
										else
										{
											RotateCurrentTileAround(currentTile.transform.forward, dir * 5);
										}
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
							else if (selectedCamera)
							{
								hit.transform.gameObject.GetComponent<Connector>().SetCamera(
									selectedCamera.GetComponent<TrackCamera>());
								flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);
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
						if (Physics.Raycast(ray, out _, Mathf.Infinity, 1 << Info.roadLayer | 1<<Info.connectorLayer))
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
			case Mode.FillTool:
				if (Input.GetMouseButtonDown(0))
				{
					Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
					if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity, 1 << Info.flagLayer))
					{
						selectedFlag = hit.transform;
						fillMenu.SetActive(true);
					}
				}
				break;
			case Mode.StuntZonesTool:
				{
					if (Input.GetMouseButtonDown(0))
					{
						Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
						var hits = Physics.RaycastAll(ray, Mathf.Infinity, 1 << Info.connectorLayer);
						foreach(var h in hits)
						{
							var c = h.transform.GetComponent<Connector>();
							c.isStuntZone = !c.isStuntZone;
							c.Colorize(c.isStuntZone ? Connector.red : Connector.blue);
						}
					}
				}
				break;
			default:
				break;
		}
	}
	async Task SetFlagsTexture(string texturePath)
	{
		if (texturePath.Length > 0)
		{
			var mr = selectedFlag.GetComponent<Renderer>();
			mr.material = new Material(mr.material);
			selectedFlag.name = texturePath;
			mr.material.mainTexture = await F.GetRemoteTexture(texturePath);
			selectedFlag.FindParentComponent<Tile>().url = texturePath;
		}
	}
	public async void SetFillLocally()
	{
		string texturePath = StandaloneFileBrowser.OpenFilePanel("Load image (square shaped)",
			UnityEngine.Application.streamingAssetsPath, "jpg", false)[0];

		await SetFlagsTexture(texturePath);
	}
	public async void SetFillFromURL(string path)
	{
		await SetFlagsTexture(path);
	}
	
	public void AddTrackCamera()
	{
		AddTrackCamera(flyCamera.transform.position);
	}
	public void AddTrackCamera(Vector3 position)
	{
		selectedCamera = Instantiate(cameraPrefab, replayCamerasContainer.transform);
		selectedCamera.transform.position = position;
	}
	class LoopReplacement
	{
		public int offset;
		public Vector3[] points;

	}

	IEnumerator ClosingPath()
	{
		connectors.Clear();
		Lpath.Clear();
		Rpath.Clear();
		List<LoopReplacement> replacements = new List<LoopReplacement>();
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
						if (!cur.marked)
						{
							cur.Colorize(Connector.red);
							connectors.Add(cur);
						}
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
					if (cur.transform.parent.name.Contains("loop"))
					{
						replacements.Add(new LoopReplacement
						{
							offset = Lpath.Count,
							points = cur.PathsExtra()
						});
					}
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

		foreach (var r in replacements)
		{
			for(int i= 0; i<r.points.Length; ++i)
			{
				racingLine[r.offset+i].x = r.points[i].x;
				racingLine[r.offset+i].y = r.points[i].y;
				racingLine[r.offset+i].z = r.points[i].z;
				racingLine[r.offset + i].w = 1;
			}
		}

		foreach (var pos in racingLine)
		{
			GameObject r = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			r.transform.position = new Vector3(pos.x, pos.y, pos.z);
			r.transform.parent = racingLineContainer.transform;
			r.GetComponent<MeshRenderer>().material.color = new Color32((byte)(255 * pos.w), 255, 255, 255);
		}
		BezierPath bezierPath = new BezierPath(racingLine.Select(v => new Vector3(v.x,v.y,v.z)).ToArray(), true, PathSpace.xyz);
		pathCreator.bezierPath = bezierPath;
		connectButtonImage.color = Color.green;


	}
	void InvalidateConnections()
	{
		Lpath.Clear();
		Rpath.Clear();
		connectButtonImage.color = Color.yellow;
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
		for(int i=0; i< racingLineContainer.transform.childCount; ++i)
		{
			Destroy(racingLineContainer.transform.GetChild(i).gameObject);
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
	void InstantiateNewTile(string tilename)
	{
		var rot = Quaternion.Euler(new Vector3(
			((currentTile.transform.GetComponent<MeshFilter>() != null) ? -90 : 0) + xRot, yRot, zRot));

		InstantiateNewTile(tilename, curPosition, rot, mirrored, scalator.distance);
		
	}
	void InstantiateNewTile(string name, Vector3 position, Quaternion rotation, 
		bool mirror, float distance, string url = null)
	{
		GameObject original;
		if (cachedTiles.ContainsKey(name))
		{
			original = cachedTiles[name];
		}
		else
		{
			original = Resources.Load<GameObject>(Info.editorTilesPath + name);
			cachedTiles.Add(name, original);
		}
		currentTile = Instantiate(original, placedTilesContainer.transform)
			.transform.GetComponent<Tile>();
		currentTile.transform.position = position;

		currentTile.transform.rotation = rotation;
		currentTile.GetComponent<Tile>().panel = this;
		if (mirror)
			currentTile.MirrorTile();

		currentTile.AdjustScale(distance);
		if(url!=null)
		{
			currentTile.transform.GetChild(0).GetChild(0);
		}
		currentTile.url = url;
		currentTile.name = name;
	}
	public void SwitchToStuntzones()
	{
		SwitchTo(Mode.StuntZonesTool);
	}
	public void SwitchToFill()
	{
		SwitchTo(Mode.FillTool);
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
				case Mode.FillTool:
					fillMenu.SetActive(false);
					break;
				case Mode.StuntZonesTool:
					
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);

					// unmark all colliders
					for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
					{
						var tile = placedTilesContainer.transform.GetChild(i);
						for (int j = 1; j < tile.transform.childCount; ++j)
						{
							var c = tile.transform.GetChild(j).GetComponent<Connector>();
							c.Colorize(Connector.blue);
							c.DisableCollider();
						}
					}
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
				case Mode.StuntZonesTool:
					{
						flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.connectorLayer;

						// mark all colliders red or blue
						for (int i=0; i<placedTilesContainer.transform.childCount; ++i)
						{
							var tile = placedTilesContainer.transform.GetChild(i);
							for (int j = 1; j < tile.transform.childCount; ++j)
							{
								var c = tile.transform.GetChild(j).GetComponent<Connector>();
								c.Colorize(c.isStuntZone ? Connector.red : Connector.blue);
								c.EnableColliderForStuntZoneMode();
							}
						}
					}
					break;
				default:
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

	
	public void SaveTrack()
	{
		TrackSavable TRACK = new TrackSavable();

		TRACK.desc = trackDescInputField.text;
		TRACK.author = trackAuthorInputField.text;
		TRACK.unlocked = 1;
		TRACK.difficulty = Convert.ToInt32(trackDifficultyInputField.text);
		TRACK.envir = Info.tracks[Info.s_trackName].envir;
		TRACK.prefCarGroup = (Info.CarGroup)carGroupDropdown.value;
		TRACK.windExternal = windExternal;
		TRACK.windRandom = windRandom;
		if (Lpath.Count > 20)
		{
			TRACK.Lpath = Lpath.ToArray();
			TRACK.Rpath = Rpath.ToArray();
			TRACK.valid = 1;
		}
		else
			TRACK.valid = 0;

		int stuntyCount=0,loopCount=0, jumpCount=0, jumpyCount=0, windyCount=0, 
			crossCount=0, pitsCount=0, icyCount=0, sandyCount=0, grassyCount=0;
		string prevTileName = "";
		for(int i=0; i<placedTilesContainer.transform.childCount; ++i)
		{
			var tile = placedTilesContainer.transform.GetChild(i).GetComponent<Tile>();
			if(tile.placed)
			{
				int id = -1;
				for(int j=0; j<TRACK.tileNames.Count; ++j)
				{
					if (TRACK.tileNames[j] == tile.transform.name)
						id = j;
				}
				if(id == -1)
				{
					TRACK.tileNames.Add(tile.transform.name);
					id = TRACK.tileNames.Count - 1;
				}

				if (stuntyCount < 10 && tile.transform.name.Contains("slope"))
					++stuntyCount;
				if (loopCount == 0 && tile.transform.name == "loop")
					++loopCount;
				if (jumpyCount < 5 && tile.transform.name == "jump")
					++jumpyCount;
				if (windyCount < 6
					&& (tile.transform.name.Contains("45") || tile.transform.name.Contains("90"))
						&& (prevTileName.Contains("45") || prevTileName.Contains("90")))
					++windyCount;
				if (crossCount < 4 && tile.transform.name == "crossing")
					++crossCount;
				if (pitsCount == 0 && tile.transform.name == "pits")
					++pitsCount;
				if (jumpCount == 0 && tile.transform.name.Contains("jump"))
					++jumpCount;
				if (icyCount == 0 && tile.transform.name.Contains("ice"))
					++icyCount;
				if (sandyCount < 10 && tile.transform.name.Contains("sand"))
					++sandyCount;
				if (grassyCount < 10 && tile.transform.name.Contains("dirt"))
					++grassyCount;

				TileSavable tSavable = new TileSavable();
				tSavable.name_id = id;
				tSavable.position = tile.transform.position;
				tSavable.rotation = tile.transform.rotation;
				tSavable.length = tile.Length();
				tSavable.url = tile.url;
				tSavable.mirrored = tile.mirrored;
				if(tile.transform.childCount > 1)
				{ // set up connectors
					tSavable.connectors = new ConnectorSavable[tile.transform.childCount - 1];
					for(int j=1; j<tile.transform.childCount; ++j)
					{
						var c = tile.transform.GetChild(j).GetComponent<Connector>();
						tSavable.connectors[j - 1].isStuntZone = c.isStuntZone;
						tSavable.connectors[j - 1].cameraID = c.trackCamera.transform.GetSiblingIndex();
						tSavable.connectors[j - 1].connectionData = new Vector2Int(
							c.connection.transform.parent.GetSiblingIndex(),
							c.connection.transform.GetSiblingIndex()); // <- connector index as explicit child index
					}
				}
				TRACK.tiles.Add(tSavable);
				prevTileName = tile.transform.name;
			}
		}

		int camsLen = replayCamerasContainer.transform.childCount;
		TRACK.replayCams = new Vector3[camsLen];
		for (int i = 0; i < camsLen; ++i)
		{
			var cam = replayCamerasContainer.transform.GetChild(i).transform.position;
			TRACK.replayCams[i].Set(cam.x, cam.y, cam.z);
		}


		List<int> icons = new List<int>();
		if (stuntyCount >= 10)
			icons.Add(0); // as in Info.IconNames
		if (loopCount >=1)
			icons.Add(1);
		if (jumpyCount >= 5)
			icons.Add(2);
		if (windyCount >= 6)
			icons.Add(3);
		if (crossCount >= 4)
			icons.Add(4);
		if (pitsCount >= 1)
			icons.Add(5);
		if (jumpCount == 0)
			icons.Add(6);
		if (icyCount > 0)
			icons.Add(7);
		if (sandyCount >= 10)
			icons.Add(8);
		if (grassyCount >= 10)
			icons.Add(9);
		TRACK.icons = icons.ToArray();

		TRACK.heights = terrainEditor.GetCurrentHeights();

		// save image
		Texture2D tex = renderTextureMat.mainTexture as Texture2D;
		byte[] textureData = tex.EncodeToPNG();
		File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, trackName + ".png"), textureData);

		// save track
		string trackJson = JsonUtility.ToJson(TRACK);
		File.WriteAllText(Path.Combine(Application.streamingAssetsPath, trackName + ".json"), trackJson);
	}
	public IEnumerator LoadTrack()
	{
		string trackJson = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, Info.s_trackName + ".json"));
		TrackSavable TRACK = JsonUtility.FromJson<TrackSavable>(trackJson);

		// ---------------------

		trackDescInputField.text = TRACK.desc;
		trackDescInputFieldPlaceholder.text = TRACK.desc;

		trackAuthorInputField.text = TRACK.author;
		trackAuthorInputFieldPlaceholder.text = TRACK.author;

		trackDifficultyInputField.text = TRACK.difficulty.ToString();
		trackDifficultyInputFieldPlaceholder.text = TRACK.difficulty.ToString();

		carGroupDropdown.value = (int)TRACK.prefCarGroup;
		windExternal = TRACK.windExternal;
		windRandom = TRACK.windRandom;

		if (TRACK.Lpath.Length > 20)
		{
			Lpath.AddRange(TRACK.Lpath);
			Rpath.AddRange(TRACK.Rpath);
		}

		for(int i=0; i<placedTilesContainer.transform.childCount; ++i)
		{ // remove leftover tiles in container
			Destroy(placedTilesContainer.transform.GetChild(i).gameObject);
		}
		for (int i = 0; i < replayCamerasContainer.transform.childCount; ++i)
		{ // remove leftover replay cams in container
			Destroy(replayCamerasContainer.transform.GetChild(i).gameObject);
		}
		
		yield return null; // update containers

		foreach (var tile in TRACK.tiles)
		{ // add tiles to scene
			InstantiateNewTile(TRACK.tileNames[tile.name_id], tile.position, tile.rotation, tile.mirrored, tile.length, tile.url);
			currentTile.SetPlaced();
		}

		foreach (var cam in TRACK.replayCams)
		{
			AddTrackCamera(cam);
		}

		for (int i=0; i<TRACK.tiles.Count; ++i)
		{ // set up connectors properly
			Transform tile = placedTilesContainer.transform.GetChild(i);
			for(int j=0; j < TRACK.tiles[i].connectors.Length; ++j)
			{
				var c = tile.GetChild(1 + j).GetComponent<Connector>();
				c.isStuntZone = TRACK.tiles[i].connectors[j].isStuntZone;
				c.trackCamera = replayCamerasContainer.transform.
					GetChild(TRACK.tiles[i].connectors[j].cameraID).GetComponent<TrackCamera>();

				if(TRACK.tiles[i].connectors[j].connectionData != Vector2Int.zero)
				{
					var cData = TRACK.tiles[i].connectors[j].connectionData;
					c.connection = placedTilesContainer.transform.GetChild(cData.x).GetChild(cData.y).GetComponent<Connector>();
					c.DisableCollider();
				}
			}
		}
		terrainEditor.SetHeights(TRACK.heights);
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
