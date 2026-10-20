using System;
using System.Collections.Generic;
using UnityEngine;

public class SC_TerrainEditor : MonoBehaviour
{
	public enum DeformMode { RaiseLower, Flatten, Smooth, Set }
	DeformMode deformMode = DeformMode.RaiseLower;
	//string[] deformModeNames = new string[] { "Raise Lower", "Flatten", "Smooth" };
	public SliderValue areaSliderVal;
	public SliderValue strengthSliderVal;
	public GameObject terrainSculptor;
	[NonSerialized]
	Terrain terrain;
	public Texture2D deformTexture;
	//public float strength = 1;
	//public float area = 1;
	//public bool showHelp;

	Transform buildTarget;
	Vector3 buildTargPos;
	Light spotLight;
	LineRenderer brushOutline;
	Material brushOutlineMaterial;
	const int BrushOutlineSegments = 96;
	readonly Vector3[] brushOutlinePoints = new Vector3[BrushOutlineSegments];

	void CreateBrushOutline()
	{
		GameObject outline = new GameObject("Terrain brush outline");
		outline.layer = F.I.terrainLayer; // Visible wherever the edited terrain is rendered; no collider.
		brushOutline = outline.AddComponent<LineRenderer>();
		brushOutlineMaterial = new Material(Resources.Load<Shader>("TerrainBrushOutline"));
		brushOutline.sharedMaterial = brushOutlineMaterial;
		brushOutline.useWorldSpace = true;
		brushOutline.loop = true;
		brushOutline.positionCount = BrushOutlineSegments;
		brushOutline.widthMultiplier = 0.12f;
		brushOutline.startColor = brushOutline.endColor = new Color(0.2f, 1f, 1f, 0.95f);
		brushOutline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		brushOutline.receiveShadows = false;
		brushOutline.enabled = false;
	}
	void LateUpdate()
	{
		if (!brushOutline) return;
		bool visible = terrain && tData && newTex && buildTarget && onTerrain &&
			!onWindow && uiTest && !uiTest.PointerOverUI();
		brushOutline.enabled = visible;
		if (!visible) return;
		Vector3 position = buildTarget.position - terrain.GetPosition();
		GetBrushArea(position, out _, out _, out int startX, out int startZ,
			out int width, out int height);
		// Match the heightmap samples actually edited, including edge clamping
		// and different terrain dimensions on the X and Z axes.
		Vector3 origin = terrain.GetPosition();
		float spacingX = tData.size.x / (tData.heightmapResolution - 1);
		float spacingZ = tData.size.z / (tData.heightmapResolution - 1);
		float radiusX = (width - 1) * spacingX * 0.5f;
		float radiusZ = (height - 1) * spacingZ * 0.5f;
		float centerX = startX * spacingX + radiusX;
		float centerZ = startZ * spacingZ + radiusZ;
		for (int i = 0; i < BrushOutlineSegments; i++)
		{
			float angle = i * (2 * Mathf.PI / BrushOutlineSegments);
			float x = centerX + Mathf.Cos(angle) * radiusX;
			float z = centerZ + Mathf.Sin(angle) * radiusZ;
			float y = tData.GetInterpolatedHeight(x / tData.size.x, z / tData.size.z);
			brushOutlinePoints[i] = origin + new Vector3(x, y + 0.04f, z);
		}
		// Keep the outline legible when the editor camera is far from the brush.
		float cameraDistance = Camera.main ? Vector3.Distance(Camera.main.transform.position,
			origin + new Vector3(centerX, terrain.SampleHeight(buildTarget.position), centerZ)) : 0;
		brushOutline.widthMultiplier = Mathf.Clamp(cameraDistance * 0.0015f, 0.12f, 0.5f);
		brushOutline.SetPositions(brushOutlinePoints);
	}
	void OnDestroy()
	{
		if (brushOutline) Destroy(brushOutline.gameObject);
		if (brushOutlineMaterial) Destroy(brushOutlineMaterial);
	}


	//GUI
	//Rect windowRect = new Rect(10, 10, 400, 185);
	bool onWindow = false;
	bool onTerrain;
	Texture2D newTex;
	float strengthSave;

	//Raycast
	private RaycastHit hit;

	//Deformation variables
	private int xRes;
	private int zRes;
	private float[,] saved;
	float flattenTarget = 0;
	Color[] craterData;

	TerrainData tData;
	private UITest uiTest;

	public int d_x;
	public int d_z;
	public int d_startX;
	public int d_startZ;
	public int d_width;
	public int d_height;
	float pickedHeight;
	EditorPanel editorPanel;
	const int UndoCapacity = 5;
	const int UndoTileSize = 32;
	sealed class HeightPatch
	{
		public int x, z;
		public float[,] heights;
	}
	sealed class TerrainOperation
	{
		public readonly Dictionary<int, HeightPatch> patches = new Dictionary<int, HeightPatch>();
	}
	readonly List<TerrainOperation> undoHistory = new List<TerrainOperation>(UndoCapacity);
	TerrainOperation currentOperation;
	bool undoShortcutHeld;
	bool blockStrokeUntilRelease;

	bool UndoShortcutPressed => F.I.ctrlInputRef && F.I.altInputRef &&
		F.I.ctrlInputRef.action.IsPressed() && F.I.altInputRef.action.IsPressed();

	void BeginTerrainOperation()
	{
		if (!terrain || !tData || blockStrokeUntilRelease) return;
		currentOperation = new TerrainOperation();
		// Sample flatten height once per stroke, using the click's actual position.
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out var terrainHit, Mathf.Infinity, 1 << F.I.terrainLayer))
		{
			Vector3 local = terrainHit.point - terrain.GetPosition();
			flattenTarget = tData.GetInterpolatedHeight(Mathf.Clamp01(local.x / tData.size.x),
				Mathf.Clamp01(local.z / tData.size.z)) / tData.size.y;
		}
	}
	void FinishTerrainOperation()
	{
		if (currentOperation != null && currentOperation.patches.Count > 0)
		{
			if (undoHistory.Count == UndoCapacity) undoHistory.RemoveAt(0);
			undoHistory.Add(currentOperation);
		}
		currentOperation = null;
	}
	void CaptureOriginalHeights(int startX, int startZ, int width, int height)
	{
		int resolution = tData.heightmapResolution;
		int tilesPerRow = (resolution + UndoTileSize - 1) / UndoTileSize;
		for (int tileZ = startZ / UndoTileSize; tileZ <= (startZ + height - 1) / UndoTileSize; tileZ++)
		{
			for (int tileX = startX / UndoTileSize; tileX <= (startX + width - 1) / UndoTileSize; tileX++)
			{
				int key = tileZ * tilesPerRow + tileX;
				if (currentOperation.patches.ContainsKey(key)) continue;
				int x = tileX * UndoTileSize, z = tileZ * UndoTileSize;
				currentOperation.patches.Add(key, new HeightPatch
				{
					x = x, z = z,
					heights = tData.GetHeights(x, z, Mathf.Min(UndoTileSize, resolution - x),
						Mathf.Min(UndoTileSize, resolution - z))
				});
			}
		}
	}
	void UndoTerrainOperation()
	{
		// Undo during a drag also restores that drag and waits for mouse release.
		FinishTerrainOperation();
		blockStrokeUntilRelease = Input.GetMouseButton(0);
		if (!tData || undoHistory.Count == 0) return;
		int last = undoHistory.Count - 1;
		foreach (HeightPatch patch in undoHistory[last].patches.Values)
			tData.SetHeightsDelayLOD(patch.x, patch.z, patch.heights);
		tData.SyncHeightmap();
		undoHistory.RemoveAt(last);
	}


	float strengthNormalized
	{
		get
		{
			return (strengthSliderVal.slider.value) / 9.0f;
		}
	}
	private void OnEnable()
	{
		terrainSculptor.SetActive(true);
		undoShortcutHeld = UndoShortcutPressed;
		F.I.ctrlInputRef.action.performed += CtrlPressed;
	}

	private void CtrlPressed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
	{
		if (UndoShortcutPressed) return;
		
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		hit = new RaycastHit();
		if (Physics.Raycast(ray, out hit, Mathf.Infinity))
		{
			pickedHeight = hit.point.y;
			editorPanel.DisplayMessageFor(pickedHeight.ToString("N2"), 3);
		}
	}

	private void OnDisable()
	{
		FinishTerrainOperation();
		blockStrokeUntilRelease = Input.GetMouseButton(0);
		F.I.ctrlInputRef.action.performed -= CtrlPressed;
		terrainSculptor.SetActive(false);
		if (brushOutline) brushOutline.enabled = false;
	}
	public void SetTerrain(Terrain terrain)
	{
		if (this.terrain != terrain)
		{
			currentOperation = null;
			undoHistory.Clear();
			blockStrokeUntilRelease = Input.GetMouseButton(0);
		}
		this.terrain = terrain;
		tData = terrain ? terrain.terrainData : null;
		if (tData) xRes = zRes = tData.heightmapResolution;
	}
	void Start()
	{
		editorPanel = GetComponent<EditorPanel>();
		areaSliderVal.slider.onValueChanged.AddListener(delegate { brushScaling(); });
		strengthSliderVal.slider.onValueChanged.AddListener(delegate { brushScaling(); });
		uiTest = GetComponent<UITest>();
		//Create build target object
		GameObject tmpObj = new GameObject("BuildTarget");
		buildTarget = tmpObj.transform;

		//Add Spot Light to build target
		GameObject spotLightObj = new GameObject("SpotLight");
		spotLightObj.transform.SetParent(buildTarget);
		spotLightObj.transform.localPosition = new Vector3(0, 2, 0);
		spotLightObj.transform.localEulerAngles = new Vector3(90, 0, 0);
		spotLight = spotLightObj.AddComponent<Light>();
		spotLight.type = LightType.Spot;
		spotLight.range = 20;
		CreateBrushOutline();

		tData = terrain.terrainData;
		if (tData)
		{
			//Save initial height data
			xRes = tData.heightmapResolution;
			zRes = tData.heightmapResolution;
			saved = new float[xRes, zRes];

			for (int i = 0; i < xRes; i++)
			{
				for (int j = 0; j < xRes; j++)
				{
					saved[i, j] = 0.5f;
				}
			}
		}

		terrain.gameObject.layer = F.I.terrainLayer;
		strengthSliderVal.slider.value = 13;
		areaSliderVal.slider.value = 13;
		brushScaling();
	}
	void Update()
	{
		if (!MobileControls.EditorDevicesConnected) return;
		if (!Input.GetMouseButton(0))
		{
			FinishTerrainOperation();
			blockStrokeUntilRelease = false;
		}
		bool undoPressed = UndoShortcutPressed;
		if (undoPressed && !undoShortcutHeld) UndoTerrainOperation();
		undoShortcutHeld = undoPressed;
		if (!undoPressed && Input.GetMouseButtonDown(0) && uiTest && !uiTest.PointerOverUI())
			BeginTerrainOperation();
		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.R))
		{
			ResetTerrain();
		}
	}
	void FixedUpdate()
	{
		if (!MobileControls.EditorDevicesConnected) return;
		if (uiTest.PointerOverUI() || terrain == null)
		{
			spotLight.spotAngle = 0;
			return;
		}
		else
			spotLight.spotAngle = areaSliderVal.slider.value;

		raycastHit();
		wheelValuesControl();

		if (onTerrain && !onWindow)
		{
			terrainDeform();
		}

		//Update Spot Light Angle according to the Area value
		
	}

	//Raycast
	//______________________________________________________________________________________________________________________________
	void raycastHit()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		hit = new RaycastHit();
		//Do Raycast hit only against terrain layer
		
		if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << F.I.terrainLayer))
		{
			onTerrain = true;
			if (buildTarget)
			{
				buildTarget.position = Vector3.Lerp(buildTarget.position, hit.point + new Vector3(0, 1, 0), Time.time);
			}
		}
		else
		{
			if (buildTarget)
			{
				Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 200);
				Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint);
				buildTarget.position = curPosition;
				onTerrain = false;
			}
		}
	}

	//TerrainDeformation
	//___________________________________________________________________________________________________________________
	void GetBrushArea(Vector3 position, out int x, out int z, out int startX, out int startZ,
		out int width, out int height)
	{
		x = (int)Mathf.Lerp(0, xRes, Mathf.InverseLerp(0, tData.size.x, position.x));
		z = (int)Mathf.Lerp(0, zRes, Mathf.InverseLerp(0, tData.size.z, position.z));
		x = Mathf.Clamp(x, newTex.width / 2, xRes - newTex.width / 2);
		z = Mathf.Clamp(z, newTex.height / 2, zRes - newTex.height / 2);
		startX = Mathf.Clamp(x - newTex.width / 2, 1, xRes - 2);
		startZ = Mathf.Clamp(z - newTex.height / 2, 1, zRes - 2);
		width = Mathf.Clamp(newTex.width, 1, xRes - startX - 1);
		height = Mathf.Clamp(newTex.height, 1, zRes - startZ - 1);
	}
	void terrainDeform()
	{
		//Terrain deform
		if (Input.GetMouseButton(0) && currentOperation != null &&
			!blockStrokeUntilRelease && !UndoShortcutPressed)
		{
			buildTargPos = buildTarget.position - terrain.GetPosition();

			if (Input.GetKey(KeyCode.LeftShift))
			{
				strengthSave = strengthSliderVal.slider.value;
			}
			else
			{
				strengthSave = -strengthSliderVal.slider.value;

			}

			if (newTex && tData && craterData != null)
			{
				GetBrushArea(buildTargPos, out int x, out int z, out int startX, out int startZ,
					out int width, out int height);

				d_x = x;
				d_z = z;
				d_startX = startX;
				d_startZ = startZ;
				d_width = width;
				d_height = height;

				float[,] areaT = tData.GetHeights(startX, startZ, width, height);
				bool changed = false;

				for (int i = 0; i < height; i++)
				{
					for (int j = 0; j < width; j++)
					{
						float previousHeight = areaT[i, j];
						switch (deformMode)
						{
							case DeformMode.RaiseLower:
								areaT[i, j] = areaT[i, j] - craterData[i * newTex.width + j].a * strengthSave / 15000;
								break;
							case DeformMode.Flatten:
								areaT[i, j] = Mathf.Lerp(areaT[i, j], flattenTarget, craterData[i * newTex.width + j].a * strengthNormalized);
								break;
							case DeformMode.Smooth:
								if (i == 0 || i == height - 1 || j == 0 || j == width - 1)
									continue;

								float heightSum = 0;
								for (int ySub = -1; ySub <= 1; ySub++)
								{
									for (int xSub = -1; xSub <= 1; xSub++)
									{
										heightSum += areaT[i + ySub, j + xSub];
									}
								}
								areaT[i, j] = Mathf.Lerp(areaT[i, j], (heightSum / 9), craterData[i * newTex.width + j].a * strengthNormalized);
								break;
							case DeformMode.Set:
								areaT[i, j] = Mathf.Clamp01((pickedHeight - terrain.GetPosition().y) / tData.size.y);
								break;
							default:
								break;
						}
						changed |= areaT[i, j] != previousHeight;
					}
				}
				if (changed)
				{
					CaptureOriginalHeights(startX, startZ, width, height);
					tData.SetHeights(startX, startZ, areaT);
				}
			}
		}
	}

	void brushScaling()
	{
		//Apply current deform texture resolution 
		float area = areaSliderVal.slider.value;
		newTex = Instantiate(deformTexture) as Texture2D;
		TextureScale.Point(newTex, deformTexture.width * (int)area / 10, deformTexture.height * (int)area / 10);
		newTex.Apply();
		craterData = newTex.GetPixels();
	}

	void wheelValuesControl()
	{
		float mouseWheel = Input.mouseScrollDelta.y;
		if (Mathf.Abs(mouseWheel) > 0.0)
		{
			if (mouseWheel > 0.0)
			{
				//More
				if (!Input.GetKey(KeyCode.LeftShift))
				{
					if (areaSliderVal.slider.value < areaSliderVal.slider.maxValue)
					{
						areaSliderVal.slider.value += 0.5f;
					}
					else
					{
						areaSliderVal.slider.value = areaSliderVal.slider.maxValue;
					}
				}
				else
				{
					if (strengthSliderVal.slider.value < strengthSliderVal.slider.maxValue)
					{
						strengthSliderVal.slider.value += 0.5f;
					}
					else
					{
						strengthSliderVal.slider.value = strengthSliderVal.slider.maxValue;
					}
				}
			}
			else if (mouseWheel < 0.0)
			{
				//Less
				if (!Input.GetKey(KeyCode.LeftShift))
				{
					if (areaSliderVal.slider.value > areaSliderVal.slider.minValue)
					{
						areaSliderVal.slider.value -= 0.5f;
					}
					else
					{
						areaSliderVal.slider.value = areaSliderVal.slider.minValue;
					}
				}
				else
				{
					if (strengthSliderVal.slider.value > strengthSliderVal.slider.minValue)
					{
						strengthSliderVal.slider.value -= 0.5f;
					}
					else
					{
						strengthSliderVal.slider.value = strengthSliderVal.slider.minValue;
					}
				}
			}
			if (areaSliderVal.slider.value > areaSliderVal.slider.minValue)
				brushScaling();
		}
	}
	public void SetDeformMode(int value)
	{
		deformMode = (DeformMode)value;
		editorPanel.DisplayMessageFor(F.I.LocStr(deformMode.ToString()), 3);
	}
	public void ResetTerrain()
	{
		currentOperation = null;
		undoHistory.Clear();
		blockStrokeUntilRelease = Input.GetMouseButton(0);
		tData.SetHeights(0, 0, saved);
	}
}
