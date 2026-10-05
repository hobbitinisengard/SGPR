using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ComponentSetter : MonoBehaviour
{
	public Dropdown dropdown;

	public void InitializeOriginalPhysics(string groupName, List<OriginalVehiclePartDefinition> parts,
		string selectedId, System.Action<OriginalVehiclePartDefinition> select)
	{
		dropdown.onValueChanged.RemoveAllListeners();
		dropdown.ClearOptions();
		var names = new List<string>();
		foreach (var part in parts)
			names.Add(part.IsUserPart ? part.GetName() : F.I.LocStr("Tuning." + part.id + ".Name"));
		if (names.Count == 0) names.Add("Original");
		dropdown.AddOptions(names);
		dropdown.SetValueWithoutNotify(Mathf.Max(0, parts.FindIndex(part => part.id == selectedId)));
		dropdown.interactable = parts.Count > 0;
		dropdown.onValueChanged.AddListener(index => { if (index >= 0 && index < parts.Count) select(parts[index]); });
		foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
		{
			if (dropdown.captionText && label == dropdown.captionText) continue;
			if (dropdown.template && label.transform.IsChildOf(dropdown.template)) continue;
			label.text = groupName + ":";
			break;
		}
	}
}
