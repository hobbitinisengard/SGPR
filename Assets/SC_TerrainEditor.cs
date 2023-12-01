using System;
using UnityEngine;

public class SC_TerrainEditor : MonoBehaviour
{
	public enum DeformMode { RaiseLower, Flatten, Smooth }
	DeformMode deformMode = DeformMode.RaiseLower;
	string[] deformModeNames = new string[] { "Raise Lower", "Flatten", "Smooth" };
	[NonSerialized]
	Terrain terrain;
	public Texture2D deformTexture;
	public float strength = 1;
	public float area = 1;
	public bool showHelp;

	Transform buildTarget;
	Vector3 buildTargPos;
	Light spotLight;

	//GUI
	Rect windowRect = new Rect(10, 10, 400, 185);
	bool onWindow = false;
	bool onTerrain;
	Texture2D newTex;
	float strengthSave;

	//Raycast
	private RaycastHit hit;

	//Deformation variables
	private int xRes;
	private int yRes;
	private float[,] saved;
	float flattenTarget = 0;
	Color[] craterData;

	TerrainData tData;
	private UITest uiTest;

	float strengthNormalized
	{
		get
		{
			return (strength) / 9.0f;
		}
	}
	public void SetTerrain(Terrain terrain)
	{
		this.terrain = terrain;
		if(terrain != null)
			tData = this.terrain.terrainData;
	}
	void Start()
	{
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

		tData = terrain.terrainData;
		if (tData)
		{
			//Save initial height data
			xRes = tData.heightmapResolution;
			yRes = tData.heightmapResolution;
			saved = new float[xRes, yRes];

			for (int i = 0; i < xRes; i++)
			{
				for (int j = 0; j < xRes; j++)
				{
					saved[i, j] = 0.5f;
				}
			}
		}

		terrain.gameObject.layer = Info.terrainLayer;
		strength = 13;
		area = 13;
		brushScaling();
	}

	void FixedUpdate()
	{
		if (uiTest.PointerOverUI() || terrain == null)
			return;
		raycastHit();
		wheelValuesControl();

		if (onTerrain && !onWindow)
		{
			terrainDeform();
		}

		//Update Spot Light Angle according to the Area value
		spotLight.spotAngle = area * 25f;
	}

	//Raycast
	//______________________________________________________________________________________________________________________________
	void raycastHit()
	{
		
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		hit = new RaycastHit();
		//Do Raycast hit only against terrain layer
		if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << Info.terrainLayer))
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
	void terrainDeform()
	{
		if (Input.GetMouseButtonDown(0))
		{
			buildTargPos = buildTarget.position - terrain.GetPosition();
			float x = Mathf.Clamp01(buildTargPos.x / tData.size.x);
			float y = Mathf.Clamp01(buildTargPos.z / tData.size.z);
			flattenTarget = tData.GetInterpolatedHeight(x, y) / tData.heightmapScale.y;
		}

		//Terrain deform
		if (Input.GetMouseButton(0))
		{

			buildTargPos = buildTarget.position - terrain.GetPosition();

			if (Input.GetKey(KeyCode.LeftShift))
			{
				strengthSave = strength;
			}
			else
			{
				strengthSave = -strength;
			}

			if (newTex && tData && craterData != null)
			{
				int x = (int)Mathf.Lerp(0, xRes, Mathf.InverseLerp(0, tData.size.x, buildTargPos.x));
				int z = (int)Mathf.Lerp(0, yRes, Mathf.InverseLerp(0, tData.size.z, buildTargPos.z));
				x = Mathf.Clamp(x, newTex.width / 2, xRes - newTex.width / 2);
				z = Mathf.Clamp(z, newTex.height / 2, yRes - newTex.height / 2);
				int startX = x - newTex.width / 2;
				int startY = z - newTex.height / 2;
				float[,] areaT = tData.GetHeights(startX, startY, newTex.width, newTex.height);
				for (int i = 0; i < newTex.height; i++)
				{
					for (int j = 0; j < newTex.width; j++)
					{
						if (deformMode == DeformMode.RaiseLower)
						{
							areaT[i, j] = areaT[i, j] - craterData[i * newTex.width + j].a * strengthSave / 15000;
						}
						else if (deformMode == DeformMode.Flatten)
						{
							areaT[i, j] = Mathf.Lerp(areaT[i, j], flattenTarget, craterData[i * newTex.width + j].a * strengthNormalized);
						}
						else if (deformMode == DeformMode.Smooth)
						{
							if (i == 0 || i == newTex.height - 1 || j == 0 || j == newTex.width - 1)
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
						}
					}
				}
				tData.SetHeights(x - newTex.width / 2, z - newTex.height / 2, areaT);
			}
		}
	}

	void brushScaling()
	{
		//Apply current deform texture resolution 
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
					if (area < 13)
					{
						area += 0.5f;
					}
					else
					{
						area = 13;
					}
				}
				else
				{
					if (strength < 13)
					{
						strength += 0.5f;
					}
					else
					{
						strength = 13;
					}
				}
			}
			else if (mouseWheel < 0.0)
			{
				//Less
				if (!Input.GetKey(KeyCode.LeftShift))
				{
					if (area > 1)
					{
						area -= 0.5f;
					}
					else
					{
						area = 1;
					}
				}
				else
				{
					if (strength > 1)
					{
						strength -= 0.5f;
					}
					else
					{
						strength = 1;
					}
				}
			}
			if (area > 1)
				brushScaling();
		}
	}

	//GUI
	//______________________________________________________________________________________________________________________________
	void OnGUI()
	{
		windowRect = GUI.Window(0, windowRect, TerrainEditorWindow, "Terrain Sculptor");

		GUILayout.BeginArea(new Rect(Screen.width - 70, 10, 60, 30));
		showHelp = GUILayout.Toggle(showHelp, "(Help)", new GUILayoutOption[] { GUILayout.Width(60.0f), GUILayout.Height(30.0f) });
		GUILayout.EndArea();

		if (showHelp)
		{
			//Help window properties
			GUI.Window(1, new Rect(Screen.width - 410, 50, 400, 120), HelpWindow, "Help Window");
		}
	}

	//Help window display tips and tricks
	void HelpWindow(int windowId)
	{
		GUILayout.BeginVertical("box");
		{
			GUILayout.Label("- Mouse wheel - area change");
			GUILayout.Label("- Mouse wheel + Shift - strength change");
			GUILayout.Label("- Hold Shift in RaiseLower mode to lower terrain");
		}
		GUILayout.EndVertical();
	}

	void TerrainEditorWindow(int windowId)
	{
		//Detect when mouse cursor inside region (TerrainEditorWindow)
		GUILayout.BeginArea(new Rect(0, 0, 400, 240));
		if (GUILayoutUtility.GetRect(10, 50, 400, 240).Contains(Event.current.mousePosition))
		{
			onWindow = true;
		}
		else
		{
			onWindow = false;
		}
		GUILayout.EndArea();

		GUILayout.BeginVertical();

		//Shared GUI
		GUILayout.Space(10f);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Area:", new GUILayoutOption[] { GUILayout.Width(75f) });
		area = GUILayout.HorizontalSlider(area, 10f, 40f, new GUILayoutOption[] { GUILayout.Width(250f), GUILayout.Height(15f) });
		GUILayout.Label((Mathf.Round(area * 100f) / 100f).ToString(), new GUILayoutOption[] { GUILayout.Width(250f), GUILayout.Height(20f) });
		//Change brush texture size if area value was changed
		if (GUI.changed)
		{
			brushScaling();
		}
		GUILayout.EndHorizontal();

		GUILayout.Space(10f);
		GUILayout.BeginHorizontal();
		GUILayout.Label("Strength:", new GUILayoutOption[] { GUILayout.Width(75f) });
		strength = GUILayout.HorizontalSlider(strength, 2f, 30f, new GUILayoutOption[] { GUILayout.Width(250f), GUILayout.Height(15f) });
		GUILayout.Label((Mathf.Round(strength * 100f) / 100f).ToString(), new GUILayoutOption[] { GUILayout.Width(250f), GUILayout.Height(20f) });
		GUILayout.EndHorizontal();

		//Deform GUI
		GUILayout.Space(10);

		deformMode = (DeformMode)GUILayout.Toolbar((int)deformMode, deformModeNames, GUILayout.Height(25));

		GUILayout.Space(10);

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset Terrain Height", new GUILayoutOption[] { GUILayout.Height(30f) }))
		{
			tData.SetHeights(0, 0, saved);
		}
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();
	}
}