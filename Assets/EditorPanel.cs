using Newtonsoft.Json;
using PathCreation;
using RVP;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct ReplayCamStruct
{
	public int dist;
	public TrackCamera cam;
}
public class EditorPanel : Sfxable
{
	const int connectorRadius = 5;
	/// <summary>
	/// Connector selector
	/// </summary>
	public class CSelector
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
			if (resetDistance)
				distance = 0;
		}
	}

	public enum Mode
	{
		None, Terrain, Build, Connect, Arrow,
		SetCamera,
		Scalator,
		FillTool,
		StuntZonesTool,
		Cross
	}


	public Mode mode { get; private set; }
	[NonSerialized]
	public List<int> stuntpointsContainer = new List<int>();
	[NonSerialized]
	public List<int> waypointsContainer = new List<int>(32);
	public List<ReplayCamStruct> replayCamsContainer = new List<ReplayCamStruct>(32);
	public GameObject waypointTriggerPrefab;
	public GameObject pathFollower;
	public RenderTexture renderTexture;
	public GameObject YouSurePanel;
	public TextMeshProUGUI trackName;
	public Sprite elementSprite;
	public Sprite selectedElSprite;
	public GameObject TilesMain;
	public SC_TerrainEditor terrainEditor;
	public FlyCamera flyCamera;
	public RaceManager raceManager;
	public GameObject savePanel;
	public GameObject toolsPanel;
	public TMP_InputField trackNameInputField;
	public TextMeshProUGUI trackNameInputFieldPlaceholder;
	public TMP_InputField trackDescInputField;
	public TextMeshProUGUI trackDescInputFieldPlaceholder;
	public TMP_InputField trackAuthorInputField;
	public TextMeshProUGUI trackAuthorInputFieldPlaceholder;
	public TMP_Dropdown trackDifficultyDropdown;
	public TMP_Dropdown carGroupDropdown;
	public Transform invisibleLevel;
	public GameObject tileGroups;
	public GameObject arrowModel;
	GameObject arrow;
	public Vector3? placedConnector;
	public Vector3? floatingConnector;
	public GameObject cameraMenu;
	public GameObject cameraPrefab;
	public GameObject infoText;
	public GameObject fillMenu;
	public PathCreator pathCreator;
	public GameObject replayCamerasContainer;
	public Image connectButtonImage;
	public Slider WindExtX;
	public Slider WindExtZ;
	public Slider WindRanX;
	public Slider WindRanZ;
	public Button terrainBtn;
	public float SecurityR;
	public float SideExt;
	public float SideInt;
	public int iterations = 500;
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
	int yRot = 0;
	int xRot = 0;
	int zRot = 0;
	CSelector selector = new CSelector();
	bool curMirror;
	public GameObject placedTilesContainer { get; private set; }
	GameObject racingLineContainer;
	public bool initialized { get; private set; }
	Vector3 curPosition;
	List<Connector> connectors = new List<Connector>();
	bool selectingOtherConnector;
	Coroutine closingPathCo;
	Coroutine DisplayCo;
	private Connector selectedConnector;
	private GameObject selectedCamera;
	private GameObject newCamera;
	private Transform selectedFlag;
	Vector4[] racingLine;
	Vector3 windExternal, windRandom;
	const int maxWind = 300;
	GameObject skybox;
	GameObject envir;
	Terrain terrain;
	Predicate<Connector> isStuntZonePred = delegate (Connector c) { return c.isStuntZone; };
	Predicate<Connector> neverMark = delegate (Connector c) { return false; };
	private Vector3 intersectionSnapLocation = -Vector3.one;
	private bool isPathClosed;
	public TrackHeader.Record[] records = TrackHeader.Record.RecordTemplate();
	private GameObject nightSkybox;

	public bool loadingTrack { get; private set; }

	void Awake()
	{
		if (!initialized)
			Initialize();
	}
	private void OnDisable()
	{
		flyCamera.enabled = false;
	}
	private void Initialize()
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
		placedTilesContainer = new GameObject("placedTilesContainer");
		racingLineContainer = new GameObject("racingLineContainer");
		initialized = true;
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
	public void SetWindExtX(float sliderVal)
	{
		windExternal.x = maxWind * sliderVal;
		ApplyWindToCloths();
	}
	public void SetWindExtZ(float sliderVal)
	{
		windExternal.z = maxWind * sliderVal;
		ApplyWindToCloths();
	}
	public void SetWindRanX(float sliderVal)
	{
		windRandom.x = maxWind * sliderVal;
		ApplyWindToCloths();
	}
	public void SetWindRanZ(float sliderVal)
	{
		windRandom.z = maxWind * sliderVal;
		ApplyWindToCloths();
	}
	void ApplyWindToCloths()
	{
		for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
		{
			if (placedTilesContainer.transform.GetChild(i).name.Contains("flag"))
			{
				var cloth = placedTilesContainer.transform.GetChild(i).GetChild(0).GetChild(0).GetComponent<Cloth>();
				cloth.randomAcceleration = windRandom;
				cloth.externalAcceleration = windExternal;
			}
		}
	}
	void HideCurrentTile()
	{
		if (currentTile)
			currentTile.gameObject.SetActive(false);
	}
	private void OnEnable()
	{
		YouSurePanel.SetActive(false);
		flyCamera.enabled = true;
		if (lastEditorCameraPosition.HasValue)
		{
			flyCamera.transform.SetPositionAndRotation(lastEditorCameraPosition.Value, lastEditorCameraRotation);
		}
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
	public void DisplayMessageFor(string str, float timer)
	{
		if (DisplayCo != null)
			StopCoroutine(DisplayCo);
		DisplayCo = StartCoroutine(DisplayMessage(str, timer));
	}
	IEnumerator DisplayMessage(string str, float timer)
	{
		infoText.GetComponent<TextMeshProUGUI>().text = str;
		yield return new WaitForSeconds(timer);
		infoText.GetComponent<TextMeshProUGUI>().text = "";
	}
	void RotateCurrentTileAround(in Vector3 axis, float angle)
	{
		currentTile.transform.RotateAround(currentTile.transform.position, axis, angle);
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			Info.s_isNight = !Info.s_isNight;
			raceManager.SetPartOfDay();
			skybox.GetComponent<SkyboxController>().nightTimeLights.SetActive(Info.s_isNight);
			for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
			{
				placedTilesContainer.transform.GetChild(i).GetComponent<Tile>().UpdateLights();
			}
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			SwitchTo(Mode.None);
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
					if (Input.GetKeyDown(KeyCode.Alpha1))
					{
						selector.Reset(true);
						if (currentTile)
						{
							Destroy(currentTile.gameObject);
							InstantiateNewTile(currentTileButton.name);
						}
					}
					float scroll = SetInvisibleLevelByScroll();

					if (scroll != 0 && Input.GetKey(KeyCode.Tab))
					{
						int dir = scroll > 0 ? -1 : 1;

						selector.distance = Mathf.Clamp(selector.distance + dir * 2.5f, 10, 200);
						if (currentTile)
						{

							Destroy(currentTile.gameObject);
							InstantiateNewTile(currentTileButton.name);
						}
						DisplayMessageFor(selector.distance.ToString("F1"), 1);
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
								curMirror = pickedTile.mirrored;
								if(pickedTile.transform.localScale.z != 1)
								{
									selector.distance = pickedTile.Length();
									DisplayMessageFor(selector.distance.ToString(), 3);
								}
								SetCurrentTileTo(GetButtonForTile(pickedTile.transform.name), pickedTile.Length(), pickedTile.transform.rotation);
								invisibleLevel.position = new Vector3(0, pickedTile.transform.position.y, 0);
							}
						}
						if (Input.GetKey(KeyCode.X))
						{ // REMOVING 
							HideCurrentTile();
							Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
							if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
								1 << Info.roadLayer))
							{
								HideCurrentTile();
								if (Input.GetMouseButtonDown(0))
								{
									SetPathClosed(false);
									if (hit.transform.gameObject.GetComponent<Tile>() == null)
										Destroy(hit.transform.parent.gameObject);
									else
										Destroy(hit.transform.gameObject);
								}
							}
						}
						else if (currentTile)
						{// PLACING 
							if (!currentTile.gameObject.activeSelf)
							{
								currentTile.gameObject.SetActive(true);
								return;
							}
							if (Input.GetKeyDown(KeyCode.Q))
							{
								curMirror = currentTile.MirrorTile();
							}
							//debug_p = Camera.main.WorldToScreenPoint(currentTile.transform.position);
							if (Input.GetMouseButtonDown(0))
							{
								// PLACE TILE
								SetPathClosed(false);
								currentTile.GetComponent<Tile>().SetPlaced();
								InstantiateNewTile(currentTileButton.name);
								placedConnector = null;
								floatingConnector = null;
								anchor = null;
								intersectionSnapLocation = -Vector3.one;
								return;
							}

							if (Input.GetKey(KeyCode.Z) && Input.GetKey(KeyCode.C))
							{ // normalize rotation
								currentTile.transform.rotation = Quaternion.Euler(xRot, yRot, zRot);
							}
							RaycastHit hit;
							if (Input.GetMouseButtonDown(1))
							{ // Y ROTATION
								int dir = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
								if (Input.GetKey(KeyCode.Tab))
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
								int dir = 1 * (scroll > 0 ? -1 : 1);
								if (Input.GetKey(KeyCode.Z))
								{
									xRot = (xRot + dir) % 360;
									if (xRot % 90 == 0)
									{
										xRot = 0;
										var euler = currentTile.transform.eulerAngles;
										euler.x = ((currentTile.transform.GetComponent<MeshFilter>() == null) ? 0 : -90) + xRot;
										currentTile.transform.rotation = Quaternion.Euler(euler);
									}
									else
									{
										RotateCurrentTileAround(currentTile.transform.right, dir);
									}
									DisplayMessageFor(xRot.ToString(), 1);
								}
								else if (Input.GetKey(KeyCode.C))
								{
									zRot = (zRot + dir) % 360;
									if (zRot % 90 == 0)
									{
										zRot = 0;
										var euler = currentTile.transform.eulerAngles;
										euler.z = zRot;
										currentTile.transform.rotation = Quaternion.Euler(euler);
									}
									else
									{
										RotateCurrentTileAround(currentTile.transform.forward, dir);
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

							if (intersectionSnapLocation != -Vector3.one)
							{
								curPosition = intersectionSnapLocation;
								currentTile.transform.position = curPosition;
							}
							else if (Physics.Raycast(ray, out hit, Mathf.Infinity,
								1 << Info.invisibleLevelLayer | 1 << Info.roadLayer | 1 << Info.terrainLayer))
							{ // tile preview location
								curPosition = hit.point;
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
										|| (anchor.HasValue && Vector3.Distance(hit.point, anchor.Value) > 4 * connectorRadius))
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
		case Mode.Cross:
			{
				if (selector.a == null || selector.b == null) // searching to add connectors
				{
					if (Input.GetMouseButtonDown(0))
					{
						Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

						if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1 << Info.connectorLayer))
						{
							var c = hit.transform.GetComponent<Connector>();
							if (!c.marked)
							{
								c.Colorize(Connector.red);

								if (selector.a == null)
								{
									selector.a = c;
								}
								else
								{
									selector.b = c;

									Vector3 centerA = selector.a.transform.parent.position;
									Vector3 centerB = selector.b.transform.parent.position;
									Vector3 VecA = (selector.a.transform.position - centerA).normalized;
									Vector3 VecB = (selector.b.transform.position - centerB).normalized;

									if (LineLineIntersection(out Vector3 intersection, centerA, VecA * 1000, centerB, VecB * 1000))
									{
										intersectionSnapLocation = intersection;
										SwitchTo(Mode.Build);
									}
								}
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
				if (selector.distance == 0) // searching to add connectors
				{
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					if (Physics.Raycast(ray, out _, Mathf.Infinity, 1 << Info.roadLayer | 1 << Info.connectorLayer))
					{
						flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.connectorLayer;
						if (Input.GetMouseButtonDown(0))
						{
							if (Physics.Raycast(ray, out RaycastHit hit2, Mathf.Infinity, 1 << Info.connectorLayer))
							{
								var c = hit2.transform.GetComponent<Connector>();
								if (!c.marked)
								{
									c.Colorize(Connector.red);

									if (selector.a == null)
									{
										selector.a = c;
									}
									else
									{
										selector.b = c;
										selector.distance = Vector3.Distance(selector.a.transform.position, selector.b.transform.position);
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
					selector.Reset(true);
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
					foreach (var h in hits)
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
	/// <summary>
	/// Returns center of connector's tile surface
	/// </summary>
	//Vector3 GetCenter(Connector c)
	//{
	//	Vector3 center = Vector3.zero;
	//	GameObject tileMain = c.transform.parent.GetChild(0).gameObject;
	//	tileMain.layer = Info.selectionLayer;
	//	Vector3 rayPoint = tileMain.transform.position;
	//	rayPoint.y += 100;
	//	if (Physics.Raycast(rayPoint, Vector3.down, out RaycastHit hit, Mathf.Infinity, 1 << Info.selectionLayer))
	//	{
	//		center = rayPoint;
	//		center.y = hit.point.y;
	//	}
	//	else
	//	{
	//		Debug.LogError("GetCenter failed");
	//	}
	//	tileMain.layer = Info.roadLayer;
	//	return center;
	//}
	/// <param name="intersection">returned intersection</param>
	/// <param name="linePoint1">start location of the line 1</param>
	/// <param name="lineDirection1">direction of line 1</param>
	/// <param name="linePoint2">start location of the line 2</param>
	/// <param name="lineDirection2">direction of line2</param>
	public static bool LineLineIntersection(out Vector3 intersection,
		 Vector3 linePoint1, Vector3 lineDirection1,
		 Vector3 linePoint2, Vector3 lineDirection2)
	{

		Vector3 lineVec3 = linePoint2 - linePoint1;
		Vector3 crossVec1and2 = Vector3.Cross(lineDirection1, lineDirection2);
		Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineDirection2);
		float planarFactor = 0;// Vector3.Dot(lineVec3, crossVec1and2);

		//is coplanar, and not parallel
		if (Mathf.Abs(planarFactor) < 0.0001f
				  && crossVec1and2.sqrMagnitude > 0.0001f)
		{
			float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
			intersection = linePoint1 + (lineDirection1 * s);
			return true;
		}
		else
		{
			intersection = Vector3.zero;
			return false;
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
			Info.documents_sgpr_path, "jpg", false)[0];

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
		List<Vector3> Lpath = new List<Vector3>(100);
		List<Vector3> Rpath = new List<Vector3>(100);
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

			//Debug.Log("elements traversed: " + i);
			if (i > 100 || i < 2)
			{
				yield break;
			}
		}

		K1999 k1999 = new K1999(SecurityR, SideExt, SideInt, iterations);
		k1999.LoadData(Lpath, Rpath);
		k1999.CalcRaceLine();
		racingLine = k1999.GetRacingLine(Lpath, Rpath);

		if (replacements != null)
		{
			foreach (var r in replacements)
			{
				for (int i = 0; i < r.points.Length; ++i)
				{
					racingLine[r.offset + i].x = r.points[i].x;
					racingLine[r.offset + i].y = r.points[i].y;
					racingLine[r.offset + i].z = r.points[i].z;
					racingLine[r.offset + i].w = 0; // as if no turns -> makes car go fast
				}
			}
		}
		BezierPath bezierPath = new BezierPath(racingLine.ToArray(), true, PathSpace.xyz);
		pathCreator.bezierPath = bezierPath;
		SetPathClosed(true);
		isPathClosed = true;
		connectButtonImage.color = Color.green;
		pathFollower.SetActive(true);

		// destroy old castable points
		for (int i = 0; i < racingLineContainer.transform.childCount; ++i)
		{
			GameObject.Destroy(racingLineContainer.transform.GetChild(i).gameObject);
		}
		// Create castable points
		float progress = 0;
		if (pathCreator.path.length > 10000)
			Debug.LogError("Path > 10000");
		else
		{
			for (int i = 0; i < 10000 && progress < pathCreator.path.length; ++i)
			{
				GameObject castable = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				Destroy(castable.GetComponent<MeshRenderer>());
				castable.transform.position = pathCreator.path.GetPointAtDistance(progress);
				castable.transform.parent = racingLineContainer.transform;
				var col = castable.GetComponent<SphereCollider>();
				col.radius = 1;
				col.isTrigger = true;
				castable.layer = 16;//Info.racingLineLayer;
				castable.name = progress.ToString(CultureInfo.InvariantCulture);
				progress += 3;
			}

			// generate stuntpoints for cars
			stuntpointsContainer.Clear();
			replayCamsContainer.Clear();
			foreach (var c in connectors)
			{
				if (c.isStuntZone || c.trackCamera != null)
				{
					var hits = Physics.OverlapSphere(c.transform.position + Vector3.up, 30, 1 << Info.racingLineLayer);
					float min = 999;
					int closestIdx = 0;
					for (int i = 0; i < hits.Length; ++i)
					{
						float distance = Vector3.Distance(hits[i].transform.position, c.transform.position);
						if (distance < min)
						{
							min = distance;
							closestIdx = i;
						}
					}
					if (c.isStuntZone)
						stuntpointsContainer.Add(int.Parse(hits[closestIdx].name));
					if (c.trackCamera != null)
					{
						replayCamsContainer.Add(new ReplayCamStruct { cam = c.trackCamera, dist = int.Parse(hits[closestIdx].name) });
					}
				}
			}

			// generate waypoints for cars
			waypointsContainer.Clear();
			float maxDot = Mathf.Cos(10 * Mathf.Deg2Rad);
			Vector3 curWayDir = pathCreator.path.GetDirectionAtDistance(0);
			waypointsContainer.Add(0);
			for (int i = 5; i < pathCreator.path.length; i += 10)
			{
				Vector3 newWayDir = pathCreator.path.GetDirectionAtDistance(i);
				float dot = Vector3.Dot(F.Vec3Flatten(curWayDir), F.Vec3Flatten(newWayDir));
				if (Mathf.Abs(dot) <= maxDot)
				{
					waypointsContainer.Add(i);
					curWayDir = newWayDir;
				}
			}
		}
	}
	void SetPathClosed(bool val)
	{
		isPathClosed = val;
		connectButtonImage.color = val ? Color.green : Color.yellow;
		pathFollower.SetActive(val);
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

		for (int j = 0; j < tileGroups.transform.childCount; ++j)
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
		SetCurrentTileTo(button, null, null);
	}
	public void SetCurrentTileTo(GameObject button, float? Length = null, Quaternion? rotation = null)
	{
		// play GUI animation
		DeselectIconAndRemoveCurrentTile();
		if (currentTileButton)
			currentTileButton.transform.parent.gameObject.SetActive(false);
		currentTileButton = button;
		currentTileButton.GetComponent<Image>().sprite = selectedElSprite;
		SwitchCurrentPanelTo(currentTileButton.transform.parent.gameObject);
		StartCoroutine(AnimateButton(button.transform.GetChild(0)));

		//PlaySFX("click");
		InstantiateNewTile(button.name, Length, rotation);
	}
	void InstantiateNewTile(string name, float? distance = null, Quaternion? rotation = null, Vector3? position = null,
		bool? mirror = null, string url = null)
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


		currentTile.transform.position = position == null ? curPosition : position.Value;

		if (rotation == null)
			rotation = Quaternion.Euler(new Vector3(
				((currentTile.transform.GetComponent<MeshFilter>() != null) ? -90 : 0) + xRot, yRot, zRot));

		currentTile.transform.rotation = rotation.Value;
		currentTile.GetComponent<Tile>().panel = this;

		if (mirror == null)
			mirror = curMirror;

		if (mirror.Value)
			currentTile.MirrorTile();

		if (distance == null)
			distance = selector.distance;
		currentTile.AdjustScale(distance.Value);

		if (url != null)
		{
			selectedFlag = currentTile.transform.GetChild(0).GetChild(0);
			SetFillFromURL(url);
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
	public void SwitchToCross()
	{
		SwitchTo(Mode.Cross);
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
					anchor = null;
					floatingConnector = null;
					placedConnector = null;
					intersectionSnapLocation = -Vector3.one;
					currentTilesPanel.gameObject.SetActive(false);
					break;
				case Mode.Connect:
					pathFollower.SetActive(false);
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
					UnmarkAllAndDisableCollidersOfConnectedConnectors();
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer | 1 << Info.cameraLayer);
					break;
				case Mode.Scalator:
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);
					selector.Reset();
					break;
				case Mode.FillTool:
					fillMenu.SetActive(false);
					break;
				case Mode.StuntZonesTool:

					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);

					UnmarkAllAndDisableCollidersOfConnectedConnectors();

					break;
				case Mode.Cross:
					selector.Reset();
					flyCamera.transform.GetComponent<Camera>().cullingMask &= ~(1 << Info.connectorLayer);
					break;
				default:
					break;
			}
			switch (mode)
			{ // open new mode

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
					selector.Reset();
					break;
				case Mode.None:
					break;
				case Mode.SetCamera:
					cameraMenu.gameObject.SetActive(true);
					flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.cameraLayer;
					EnableAllCollidersAndMark(neverMark);
					break;
				case Mode.StuntZonesTool:
					flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.connectorLayer;
					EnableAllCollidersAndMark(isStuntZonePred);
					break;
				case Mode.Cross:
					flyCamera.transform.GetComponent<Camera>().cullingMask |= 1 << Info.connectorLayer;
					break;
				default:
					break;
			}
			this.mode = mode;
		}
	}


	void EnableAllCollidersAndMark(Predicate<Connector> p)
	{
		for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
		{
			var tile = placedTilesContainer.transform.GetChild(i);
			for (int j = 1; j < tile.transform.childCount; ++j)
			{
				var c = tile.transform.GetChild(j).GetComponent<Connector>();
				c.Colorize(p(c) ? Connector.red : Connector.blue);
				c.EnableColliderForStuntZoneMode();
			}
		}
	}
	void UnmarkAllAndDisableCollidersOfConnectedConnectors()
	{
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
		toolsPanel.SetActive(false);
		savePanel.SetActive(!savePanel.activeSelf);
		SwitchTo(Mode.None);
	}
	public void ToggleToolsPanel()
	{
		savePanel.SetActive(false);
		toolsPanel.SetActive(!toolsPanel.activeSelf);
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
		raceManager.BackToMenu();
	}
	public void QuickSave()
	{
		string name = trackName.text;
		if (name[0] == '*')
			trackName.text = trackName.text[1..];
	}
	public void SaveTrack()
	{
		if (trackNameInputField.text.Length <= 3)
			trackNameInputField.text = "Untitled";
		trackName.text = trackNameInputField.text;

		TrackSavableData TRACK = new TrackSavableData();
		TRACK.windExternal = windExternal;
		TRACK.windRandom = windRandom;

		int stuntyCount = 0, loopCount = 0, jumpCount = 0, jumpyCount = 0, windyCount = 0,
			crossCount = 0, pitsCount = 0, icyCount = 0, sandyCount = 0, grassyCount = 0;
		string prevTileName = "";

		TRACK.tileNames = new List<string>();
		TRACK.tiles = new List<TileSavable>(placedTilesContainer.transform.childCount);
		for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
		{
			var tile = placedTilesContainer.transform.GetChild(i).GetComponent<Tile>();
			if (tile.placed)
			{
				int id = -1;
				for (int j = 0; j < TRACK.tileNames.Count; ++j)
				{
					if (TRACK.tileNames[j] == tile.transform.name)
						id = j;
				}
				if (id == -1)
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
				if (tile.transform.childCount > 1)
				{ // set up connectors
					tSavable.connectors = new ConnectorSavable[tile.transform.childCount - 1];
					for (int j = 1; j < tile.transform.childCount; ++j)
					{
						var c = tile.transform.GetChild(j).GetComponent<Connector>();
						tSavable.connectors[j - 1] = new ConnectorSavable();
						tSavable.connectors[j - 1].isStuntZone = c.isStuntZone;
						tSavable.connectors[j - 1].cameraID = (c.trackCamera == null) ? -1 : c.trackCamera.transform.GetSiblingIndex();
						if (c.connection)
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
		if (loopCount >= 1)
			icons.Add(1);
		if (jumpyCount >= 5)
			icons.Add(2);
		if (windyCount >= 6)
			icons.Add(3);
		if (crossCount >= 4)
			icons.Add(4);
		if (pitsCount == 0)
			icons.Add(5);
		if (jumpCount == 0)
			icons.Add(6);
		if (icyCount > 0)
			icons.Add(7);
		if (sandyCount >= 10)
			icons.Add(8);
		if (grassyCount >= 10)
			icons.Add(9);


		TRACK.heights = GetHeightsmap();

		// save image
		Texture2D tex = F.toTexture2D(renderTexture);
		byte[] textureData = tex.EncodeToPNG();
		string path = Path.Combine(Info.documents_sgpr_path, trackName.text + ".png");
		File.WriteAllBytes(path, textureData);

		// save track editor data
		string trackJson = JsonConvert.SerializeObject(TRACK);
		path = Path.Combine(Info.documents_sgpr_path, trackName.text + ".data");
		File.WriteAllText(path, trackJson);

		// save track header
		TrackHeader header = new()
		{
			unlocked = true,
			preferredCarClass = (Info.CarGroup)carGroupDropdown.value,
			difficulty = trackDifficultyDropdown.value,
			envir = Info.tracks[Info.s_trackName].envir,
			author = trackAuthorInputField.text,
			icons = icons.ToArray(),
			desc = trackDescInputField.text,
			records = this.records,
		};
		header.valid = racingLine != null && racingLine.Length > 8 && header.records != null;
		if (!header.valid)
		{
			DisplayMessageFor("Drive at least once to validate track", 3);
		}
		trackJson = JsonConvert.SerializeObject(header);
		path = Path.Combine(Info.documents_sgpr_path, trackName.text + ".track");
		File.WriteAllText(path, trackJson);
		if (!Info.tracks.ContainsKey(trackName.text))
			Info.tracks.Add(trackName.text, header);
		else
			Info.tracks[trackName.text] = header;
	}
	public void SetPylonVisibility(bool isVisible)
	{
		for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
		{
			var tile = placedTilesContainer.transform.GetChild(i).gameObject;
			if (tile.name.Contains("pylon") || tile.name == "reflector")
				tile.SetActive(isVisible);
		}
	}
	//HeightsSavableWrapper ConvertHeights(float[,] heights)
	//{
	//	int len = heights.GetLength(0);
	//	List<List<float>> list = new List<List<float>>(len);
	//	for(int y=0; y< len; ++y)
	//	{
	//		List<float> row = new List<float>(len);
	//		for(int x=0; x< len; ++x)
	//		{
	//			row.Add(heights[x, y]);
	//		}
	//		list.Add(row);
	//	}
	//	var wrapper = new HeightsSavableWrapper();
	//	wrapper.heights = list;
	//	return wrapper;
	//}
	//float[,] ConvertHeights(in HeightsSavableWrapper h)
	//{
	//	float[,] heights = new float[h.heights.Count, h.heights.Count];
	//	for (int y = 0; y < h.heights.Count; ++y)
	//	{
	//		for (int x = 0; x < h.heights.Count; ++x)
	//		{
	//			heights[x, y] = h.heights[y][x];
	//		}
	//	}
	//	return heights;
	//}
	public float[,] GetHeightsmap()
	{
		if (terrain != null)
		{
			var resXY = terrain.terrainData.heightmapResolution;
			var heights = terrain.terrainData.GetHeights(0, 0, resXY, resXY);
			foreach (var h in heights)
			{
				if (h != 0.5f)
					return heights;
			}
		}
		Debug.Log("Getting flat (null) hmap ");
		return null;
	}
	public void SetHeightsmap(float[,] hmap)
	{
		if (terrain != null)
		{
			if (hmap == null)
			{
				Debug.Log("Setting flat (null) hmap");
				var resXY = terrain.terrainData.heightmapResolution;
				hmap = new float[resXY, resXY];

				for (int i = 0; i < resXY; i++)
				{
					for (int j = 0; j < resXY; j++)
					{
						hmap[i, j] = 0.5f;
					}
				}
			}
			terrain.terrainData.SetHeights(0, 0, hmap);
		}
	}
	public IEnumerator LoadTrack()
	{
		loadingTrack = true;
		records = TrackHeader.Record.RecordTemplate();
		if (!initialized)
			Initialize();
		int skyboxNumber = Info.skys[(int)Info.tracks[Info.s_trackName].envir];
		string envirName = Info.tracks[Info.s_trackName].envir.ToString();
		if (skybox != null)
			Destroy(skybox);
		skybox = Instantiate(Resources.Load<GameObject>("envirs/" + "sky" + skyboxNumber.ToString()));
		if(Info.s_isNight)
		{
			if (nightSkybox == null)
				nightSkybox = Instantiate(Resources.Load<GameObject>("envirs/" + "sky6"));
			nightSkybox.SetActive(true);
		}
		else
		{
			if (nightSkybox != null)
				nightSkybox.SetActive(false);
		}
		if (envir != null)
			Destroy(envir);
		envir = Instantiate(Resources.Load<GameObject>("envirs/" + envirName));
		if (terrain != null)
			Destroy(terrain.gameObject);

		var terrainTr = envir.transform.Find("Terrain");
		if (terrainTr)
		{
			terrain = terrainTr.GetComponent<Terrain>();
		}

		terrainBtn.gameObject.SetActive(terrain != null);

		if (Info.s_isNight)
		{
			var lights = envir.transform.Find("Lights");
			if (lights != null)
				lights.gameObject.SetActive(true);
		}
		trackName.text = Info.s_trackName;
		string path = Path.Combine(Info.documents_sgpr_path, Info.s_trackName + ".data");

		terrainEditor.SetTerrain(terrain);

		invisibleLevel.localScale = Info.invisibleLevelDimensions[(int)Info.tracks[Info.s_trackName].envir];


		for (int i = 0; i < placedTilesContainer.transform.childCount; ++i)
		{ // remove leftover tiles in container
			Destroy(placedTilesContainer.transform.GetChild(i).gameObject);
		}
		for (int i = 0; i < replayCamerasContainer.transform.childCount; ++i)
		{ // remove leftover replay cams in container
			Destroy(replayCamerasContainer.transform.GetChild(i).gameObject);
		}

		trackNameInputField.text = trackName.text;
		trackNameInputFieldPlaceholder.text = trackName.text;

		yield return null; // update containers

		if (!File.Exists(path))
		{
			Debug.LogWarning("No data file found, path:" + path);
			SetHeightsmap(null);
			loadingTrack = false;
			yield break;
		}

		string trackJson = File.ReadAllText(path);
		TrackSavableData TRACK = JsonConvert.DeserializeObject<TrackSavableData>(trackJson);


		trackDescInputField.text = Info.tracks[Info.s_trackName].desc;
		trackDescInputFieldPlaceholder.text = Info.tracks[Info.s_trackName].desc;

		trackAuthorInputField.text = Info.tracks[Info.s_trackName].author;
		trackAuthorInputFieldPlaceholder.text = Info.tracks[Info.s_trackName].author;

		trackDifficultyDropdown.value = Info.tracks[Info.s_trackName].difficulty;

		carGroupDropdown.value = (int)Info.tracks[Info.s_trackName].preferredCarClass;

		windExternal = TRACK.windExternal;
		windRandom = TRACK.windRandom;


		if (TRACK.replayCams != null)
		{
			foreach (var cam in TRACK.replayCams)
			{
				AddTrackCamera(cam);
			}
		}

		if (TRACK.tiles != null)
		{
			foreach (var tile in TRACK.tiles)
			{ // add tiles to scene
				InstantiateNewTile(TRACK.tileNames[tile.name_id], tile.length, tile.rotation, tile.position, tile.mirrored, tile.url);
				currentTile.SetPlaced();
			}
			for (int i = 0; i < TRACK.tiles.Count; ++i)
			{ // set up connectors properly
				Transform tile = placedTilesContainer.transform.GetChild(i);
				if (TRACK.tiles[i].connectors == null)
					continue;
				for (int j = 0; j < TRACK.tiles[i].connectors.Length; ++j)
				{
					var c = tile.GetChild(1 + j).GetComponent<Connector>();

					c.isStuntZone = TRACK.tiles[i].connectors[j].isStuntZone;

					if (TRACK.tiles[i].connectors[j].cameraID != -1)
					{
						c.SetCamera(replayCamerasContainer.transform.
							GetChild(TRACK.tiles[i].connectors[j].cameraID).GetComponent<TrackCamera>());
					}

					if (TRACK.tiles[i].connectors[j].connectionData != Vector2Int.zero)
					{
						var cData = TRACK.tiles[i].connectors[j].connectionData;
						c.connection = placedTilesContainer.transform.GetChild(cData.x).GetChild(cData.y).GetComponent<Connector>();
						c.DisableCollider();
					}
				}
			}
		}
		currentTile = null;
		windExternal = TRACK.windExternal;
		windRandom = TRACK.windRandom;
		WindExtX.value = windExternal.x / maxWind;
		WindExtZ.value = windExternal.z / maxWind;
		WindRanX.value = windRandom.x / maxWind;
		WindRanZ.value = windRandom.z / maxWind;
		SetHeightsmap(TRACK.heights);
		SwitchToConnect();
		loadingTrack = false;
	}
	public void OpenLoadTrackFileBrowser()
	{
		if (savePanel.activeSelf || toolsPanel.activeSelf)
			return;

		string filepath = StandaloneFileBrowser.OpenFilePanel("Select track",
			Info.documents_sgpr_path, "track", false)[0];
		if (filepath.Length > 0)
		{
			string newTrackName = Path.GetFileNameWithoutExtension(filepath);
			if (Info.tracks.ContainsKey(newTrackName))
			{
				Info.s_trackName = newTrackName;
				StartCoroutine(LoadTrack());
			}
		}
	}
	public void ToFreeRoamButton()
	{
		SwitchTo(Mode.Arrow);
	}
	public void ToValidate()
	{
		SwitchTo(Mode.None);
		Info.s_laps = 3;
		if (trackName.text.Length == 3)
		{
			DisplayMessageFor("Save track!", 2);
			return;
		}
		if (isPathClosed)
		{
			flyCamera.enabled = false;
			StartCoroutine(raceManager.StartRace());
		}
		else
		{
			DisplayMessageFor("Path isn't closed!", 2);
			return;
		}
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
