using TMPro;
using UnityEngine;

public class EnterPasswordWnd : MonoBehaviour
{
	public TMP_InputField inputField;
	public GameObject lobbyView;
	public ServerList serverList;
	public string joinCode;
	/// <summary>
	/// Arg1: joinCode, Arg2: password
	/// </summary>
	private void Awake()
	{
		inputField.text = "";
		inputField.onSubmit.AddListener((password) => 
		{
			serverList.OnPasswordEntered(joinCode, password);
			gameObject.SetActive(false);
		});
	}
	public void OpenWindow(string joinCode)
	{
		gameObject.SetActive(true);
		inputField.Select();
		this.joinCode = joinCode;
	}
}
