using System;
using TMPro;
using UnityEngine;

public class SavePanelWorks : MonoBehaviour
{
	public TMP_InputField trackDescr;
	public TMP_InputField trackName;
	public TMP_InputField trackAuthor;
	public TMP_Dropdown languageDropdown;
	public GameObject beigePlane;
	EditorPanel editorPanel;
	[NonSerialized]
	public string[] localizedDescriptions;
	[NonSerialized]
	public string[] localizedNames;

	private void Awake()
	{
		editorPanel = transform.parent.GetComponent<EditorPanel>();
	}
	private void OnEnable()
	{
		editorPanel.SetPylonVisibility(false);
		beigePlane.SetActive(true);
		Physics.BoxCast(Vector3.zero + 2000 * Vector3.down, new Vector3(3000, 1, 3000), Vector3.up, out var hit, Quaternion.identity, Mathf.Infinity, 1 << F.I.roadLayer);
		beigePlane.transform.position = hit.point;
		editorPanel.SetPylonVisibility(true);
		trackName.text = localizedNames[languageDropdown.value];
		trackDescr.text = localizedDescriptions[languageDropdown.value];
		editorPanel.SwitchDayNight(TimeOfDay.Day);
	}
	private void OnDisable()
	{
		beigePlane.SetActive(false);
		//editorPanel.SetPylonVisibility(true);
		SetFlyCamera(true);
	}
	public void RegisterName(string name)
	{
		localizedNames[languageDropdown.value] = name;
	}
	public void RegisterDescription(string desc)
	{
		localizedDescriptions[languageDropdown.value] = desc;
	}
	public void LanguageChanged()
	{
		trackName.text = localizedNames[languageDropdown.value];
		trackDescr.text = localizedDescriptions[languageDropdown.value];
	}
	public void SetFlyCamera(bool enabled)
	{
		editorPanel.flyCamera.enabled = enabled;
	}
	public void Deselect()
	{
		F.Deselect();
	}
}
