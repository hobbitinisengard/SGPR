using TMPro;
using UnityEngine;

public class Uppercase : MonoBehaviour
{
	TMP_InputField inputField;
	private void Awake()
	{
		inputField = GetComponent<TMP_InputField>();
	}
	void Start()
	{
		inputField.onSubmit.AddListener(delegate { ValueChangeCheck(); });
	}
	public void ValueChangeCheck()
	{
		inputField.text = inputField.text.ToUpper();
	}
}




