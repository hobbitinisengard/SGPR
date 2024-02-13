using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindSaveLoad : MonoBehaviour
{
	public InputActionAsset actions;
	public Button RebindControllerMenu;
	public void OnEnable()
	{
		var entries = Input.GetJoystickNames();
		RebindControllerMenu.interactable = entries.Any(e => e != "");
		var rebinds = PlayerPrefs.GetString("rebinds");
		if (!string.IsNullOrEmpty(rebinds))
			actions.LoadBindingOverridesFromJson(rebinds);
	}

	public void OnDisable()
	{
		var rebinds = actions.SaveBindingOverridesAsJson();
		PlayerPrefs.SetString("rebinds", rebinds);
	}
}
