using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertySetter : MonoBehaviour
{
	bool initializing;
	InputField input;
	public float value { get; private set; }
	Action<float> applyValuesMethod;

	public void Initialize(string propName, float value, Action<float> applyValuesToCarMethod)
	{
		initializing = true;
		transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = propName;
		input = transform.GetChild(1).GetComponent<InputField>();
		input.contentType = InputField.ContentType.Standard;
		input.characterValidation = InputField.CharacterValidation.None;
		applyValuesMethod = applyValuesToCarMethod;
		this.value = value;
		SetText(value);
		initializing = false;
	}

	void SetText(float number) => input.SetTextWithoutNotify(number.ToString("G9", CultureInfo.CurrentCulture));

	public void UpdateValue(string text)
	{
		if (initializing) return;
		text = text.Trim();
		float number;
		if (!float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number) &&
			!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
		{
			SetText(value);
			return;
		}
		if (float.IsNaN(number) || float.IsInfinity(number)) { SetText(value); return; }
		value = number;
		SetText(value);
		applyValuesMethod?.Invoke(value);
	}

	public void CommitInput() => UpdateValue(input.text);
}
