using RVP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class EditorPanel : Sfxable
{
	static readonly int connectorRadius = 5;
	public enum Mode { None, Terrain, Build, Connect};
	Mode mode = Mode.Build;
	public GameObject YouSurePanel;
	public Text trackName;
	public Sprite elementSprite;
	public Sprite selectedElSprite;
	public GameObject TilesMain;
	public SC_TerrainEditor terrainEditor;
	public FlyCamera flyCamera;

	public GameObject savePanel;
	public TextMeshProUGUI trackNameInputField;
	public TextMeshProUGUI trackNameInputFieldPlaceholder;
	public TextMeshProUGUI trackDescInputField;
	public TextMeshProUGUI trackDescInputFieldPlaceholder;

	public Transform invisibleLevel;

	public Vector3? placedConnector;
	public Vector3? floatingConnector;

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
	public Vector3 debug_p;
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
	void DeselectTile()
	{
		if(currentTile)
		{
			Destroy(currentTile);
			if (currentTileButton != null)
				currentTileButton.GetComponent<Image>().sprite = elementSprite;
		}
	}
	void HideCurrentTile()
	{
		if(currentTile)
			currentTile.gameObject.SetActive(false);
	}
	void ShowCurrentTile()
	{
		if(currentTile)
			currentTile.gameObject.SetActive(true);
	}
	
	
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			ShowYouSurePanel();
		}
		if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.S))
			QuickSave();
		if (mode == Mode.Build)
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
			if(uiTest.PointerOverUI())
			{
				HideCurrentTile();

				if (Input.GetMouseButtonDown(1))
					SwitchCurrentPanelTo(TilesMain);

				if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
				{
					DeselectTile();
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
							Destroy(hit.transform.gameObject);
						}
					}
				}
				else
				{ // PLACING 
					ShowCurrentTile();
					
					if (currentTile)
					{
						if(Input.GetKeyDown(KeyCode.Q))
						{
							currentTile.MirrorTile();
						}
						//debug_p = Camera.main.WorldToScreenPoint(currentTile.transform.position);
						if (Input.GetMouseButtonDown(0))
						{
							if (!pointingOnRoad)
							{
								currentTile.GetComponent<Tile>().SetPlaced();
								SetCurrentTileTo(currentTileButton.name);
								placedConnector = null;
								floatingConnector = null;
								anchor = null;
								return;
							}
						}
						if (Input.GetMouseButtonDown(1))
						{
							var euler = currentTile.transform.eulerAngles;
							tileRotation = (tileRotation + 45) % 360;
							euler.y = tileRotation;
							currentTile.transform.rotation = Quaternion.Euler(euler);
						}

						Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
						if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity,
							1 << Info.invisibleLevelLayer | 1 << Info.roadLayer))
						{
							if (Input.GetKey(KeyCode.LeftControl))
							{
								var p = currentTile.transform.position;
								p.y = hit.point.y;
								currentTile.transform.position = p;
							}
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
									var p = invisibleLevel.position;
									p.y = placedConnector.Value.y;
									invisibleLevel.position = p;
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
	}
	void SetCurrentTileTo(string tileName)
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
		currentTile = Instantiate(original).transform.GetComponent<Tile>();
		var r = currentTile.transform.eulerAngles;
		r.y = tileRotation;
		currentTile.transform.rotation = Quaternion.Euler(r);
		currentTile.GetComponent<Tile>().panel = this;
	}
	public void SetCurrentTileTo(GameObject button)
	{
		DeselectTile();
		currentTileButton = button;

		currentTileButton.GetComponent<Image>().sprite = selectedElSprite;
		StartCoroutine(AnimateButton(button.transform.GetChild(0)));
		//PlaySFX("click");
		SetCurrentTileTo(button.name);
	}
	public void SwitchTo(Mode mode)
	{
		if (this.mode == mode && this.mode != Mode.None)
		{
			SwitchTo(Mode.None);
		}
		else
		{
			switch (mode)
			{
				case Mode.Terrain:
					terrainEditor.enabled = true;
					currentTilesPanel.gameObject.SetActive(false);
					break;
				case Mode.Build:
					currentTilesPanel.gameObject.SetActive(true);
					terrainEditor.enabled = false;
					break;
				case Mode.Connect:
					terrainEditor.enabled = false;
					currentTilesPanel.gameObject.SetActive(false);
					break;
				case Mode.None:
					terrainEditor.enabled = false;
					currentTilesPanel.gameObject.SetActive(false);
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
	public void ToggleSavePanel()
	{
		savePanel.SetActive(!savePanel.activeSelf);
		if (savePanel.activeSelf)
			SwitchTo(Mode.None);
	}
	
	IEnumerator AnimateButton(Transform button)
	{
		float timer = 0;
		for(int i=0; i<10000; ++i)
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
	//}

	private bool SaveTrack()
	{
		return false;
	}
	public void ToFreeRoamButton()
	{

	}
	public void ToValidate()
	{

	}
	public void HidePanel()
	{
		F.PlaySlideOutOnChildren(YouSurePanel.transform);
		if (hideCo!=null)
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
