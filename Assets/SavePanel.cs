using System;
using TMPro;
using UnityEngine;

public class SavePanelWorks : MonoBehaviour
{
	public TMP_InputField trackDescr;
	public TMP_InputField trackName;
	public TMP_InputField trackAuthor;
	public TMP_Dropdown languageDropdown;
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
		trackName.text = localizedNames[languageDropdown.value];
		trackDescr.text = localizedDescriptions[languageDropdown.value];
	}
	private void OnDisable()
	{
		editorPanel.SetPylonVisibility(true);
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
