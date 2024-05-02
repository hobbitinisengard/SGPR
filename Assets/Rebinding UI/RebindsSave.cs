using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindSave : MonoBehaviour
{
	public InputActionAsset actions;
	public Button RebindControllerMenu;

	public void OnDisable()
	{
		var rebinds = actions.SaveBindingOverridesAsJson();
		PlayerPrefs.SetString("rebinds", rebinds);
	}
}
