using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComponentSetter : MonoBehaviour
{
	public Dropdown dropdown;

	public void InitializeOriginalPhysics(string groupName)
	{
		dropdown.ClearOptions();
		dropdown.AddOptions(new List<string> { "Original" });
		dropdown.value = 0;
		dropdown.interactable = false;

		foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
		{
			if (dropdown.captionText && label == dropdown.captionText)
				continue;
			if (dropdown.template && label.transform.IsChildOf(dropdown.template))
				continue;

			label.text = groupName + ":";
			break;
		}
	}
}
