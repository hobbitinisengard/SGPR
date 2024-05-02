using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindsLoad : MonoBehaviour
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
}
