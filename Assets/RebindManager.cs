using UnityEngine;
using UnityEngine.InputSystem;

public class RebindManager : MonoBehaviour
{
   public InputActionMap globalMap;
	private void OnEnable()
	{
		foreach(var action in globalMap.actions)
			action.Disable();
	}
	private void OnDisable()
	{
		foreach (var action in globalMap.actions)
			action.Enable();
	}
}
