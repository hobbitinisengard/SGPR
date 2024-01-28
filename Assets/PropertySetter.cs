using System;
using TMPro;
using UnityEngine;

public class PropertySetter : MonoBehaviour
{
	bool initializing;
	// convention: name(0), inputfield(1)
	public float value { get; private set; }
	Action applyValuesMethod;
	public void Initialize(string propName, float value, Action applyValuesToCarMethod)
	{
		initializing = true;
		transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = propName;
		transform.GetChild(1).GetComponent<TMP_InputField>().text = value.ToString();
		applyValuesMethod = applyValuesToCarMethod;
		this.value = value;
		initializing = false;
	}
	public void UpdateValue(string value)
	{
		if (initializing)
			return;
		try
		{
			this.value = float.Parse(value);
			applyValuesMethod.Invoke();
		}
		catch
		{
			Debug.LogError("Bad string: " + value);
			Debug.Break();
		}
	}
}
