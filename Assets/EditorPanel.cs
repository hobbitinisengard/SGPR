using RVP;
using SFB;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class EditorPanel : MonoBehaviour
{
	public GameObject YouSurePanel;
	public Text trackName;
	Coroutine hideCo;
	void Start()
	{

	}

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			ShowYouSurePanel();
		}
		if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.S))
			QuickSave();
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
	public void ShowSavePanel()
	{
		string[] originalpath = StandaloneFileBrowser.OpenFolderPanel(
			"Select folder to save this track in ..", Info.LoadLastFolderPath(), false);
		string path = originalpath[0];
		if (SaveTrack(path))
		{
			Info.SaveLastFolderPath(path);
		}
	}

	private bool SaveTrack(string path)
	{
		throw new NotImplementedException();
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
		YouSurePanel.transform.GetChild(1).GetComponent<Button>().Select();
	}
}
