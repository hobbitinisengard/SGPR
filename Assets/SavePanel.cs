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
	public GameObject renderImageGameObject;
	public TextMeshProUGUI overwriteTrackPicButtonText;
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
		overwriteTrackPicButtonText.text = F.I.LocStr("Overwrite track picture:") + (editorPanel.overwriteTrackPicture ? F.I.LocStr("Yes") : F.I.LocStr("No"));
		F.I.renderTextureCam.SetActive(true);
		editorPanel.SetVisibleInPictureMode(false);
		beigePlane.SetActive(true);
		renderImageGameObject.SetActive(editorPanel.overwriteTrackPicture);
		Physics.BoxCast(Vector3.zero + 2000 * Vector3.down, new Vector3(3000, 1, 3000), Vector3.up, out var hit, Quaternion.identity, Mathf.Infinity, 1 << F.I.roadLayer);
		beigePlane.transform.position = hit.point;
		editorPanel.SetVisibleInPictureMode(true);
		trackName.text = localizedNames[languageDropdown.value];
		trackDescr.text = localizedDescriptions[languageDropdown.value];
		editorPanel.SwitchDayNight(TimeOfDay.Day);
	}
	private void OnDisable()
	{
		F.I.renderTextureCam.SetActive(false);
		beigePlane.SetActive(false);
		//editorPanel.SetPylonVisibility(true);
		SetFlyCamera(true);
	}
	public void ToggleOverwriteTrackPicture()
	{
		editorPanel.overwriteTrackPicture = !editorPanel.overwriteTrackPicture;
		overwriteTrackPicButtonText.text = F.I.LocStr("Overwrite track picture:") + (editorPanel.overwriteTrackPicture ? F.I.LocStr("Yes") : F.I.LocStr("No"));
		renderImageGameObject.SetActive(editorPanel.overwriteTrackPicture);
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
