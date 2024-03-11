using UnityEngine;

public class SavePanelWorks : MonoBehaviour
{
	EditorPanel editorPanel;
	private void Awake()
	{
		editorPanel = transform.parent.GetComponent<EditorPanel>();
	}
	private void OnEnable()
	{
		editorPanel.SetPylonVisibility(false);
	}
	private void OnDisable()
	{
		editorPanel.SetPylonVisibility(true);
	}

}
