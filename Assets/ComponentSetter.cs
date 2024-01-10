using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ComponentSetter : MonoBehaviour
{
	bool initializing;
	PartType partType;
	Action<PartType,string> userSelectedDropdownAction;
	string customString = "Custom";
	public TMP_Dropdown dropdown;
	/// <param name="selectOption">type -1 for custom part</param>
	/// <param name="userSelectedDropdownAction">method called when user selects different part from dropdown</param>
	public void Initialize(PartType type, List<string> options, int selectOption, 
		Action<PartType, string> userSelectedDropdownAction)
	{
		initializing = true;
		partType = type;
		dropdown.ClearOptions();
		options.Add(customString);
		dropdown.AddOptions(options);
		dropdown.value = (selectOption == -1) ? (options.Count-1) : selectOption;
		this.userSelectedDropdownAction = userSelectedDropdownAction;
		initializing = false;
	}
	public string GetComponentName()
	{
		var dropdown = transform.GetChild(1).GetComponent<TMP_Dropdown>();
		return dropdown.options[dropdown.value].text;
	}
	public void UpdateValue(int value)
	{
		if (initializing || userSelectedDropdownAction == null)
			return;
		string partName = dropdown.options[value].text;
		if (partName == customString)
			partName = null;
		userSelectedDropdownAction.Invoke(partType, partName);
	}
}
