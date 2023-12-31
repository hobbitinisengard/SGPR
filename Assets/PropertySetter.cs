using System;
using TMPro;
using UnityEngine;

public class PropertySetter : MonoBehaviour
{
	// convention: name(0), inputfield(1)
	public float value { get; private set; }
	Action applyValuesMethod;
	public void Initialize(string propName, float value, Action applyValuesToCarMethod)
	{
		transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = propName;
		transform.GetChild(1).GetComponent<TMP_InputField>().text = value.ToString();
		applyValuesMethod = applyValuesToCarMethod;
		this.value = value;
	}
	public void UpdateValue(string value)
	{
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
