using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class EnterIPPort : MonoBehaviour
{
	public NetworkManager networkManager;
	public TMP_InputField inputField;
	public GameObject lobbyView;
	TextMeshProUGUI placeholder;
	MainMenuView thisView;
	
	private void Awake()
	{
		placeholder = (TextMeshProUGUI)inputField.placeholder;
		thisView = transform.parent.GetComponent<MainMenuView>();
	}
	public void EnterPortAndStartAServer()
	{
		gameObject.SetActive(true);
		inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
		placeholder.text = "Enter port number in range 1024-49151";
		Info.serverSide = ServerSide.Host;
	}
	public void EnterIPAndJoinAServer()
	{
		gameObject.SetActive(true);
		inputField.contentType = TMP_InputField.ContentType.Standard;
		placeholder.text = "Enter IP address and port. Example: 123.45.67.89:8080";
		Info.serverSide = ServerSide.Client;
	}
	public void CreateJoinLobby(string text)
	{
		if (Info.serverSide == ServerSide.Host)
		{
			if (text.Length >= 4 && text.Length <= 5)
			{
				Info.playerData.portNumber = int.Parse(text);
				thisView.GoToView(lobbyView);
			}
		}
		else
		{
			text = text.Trim();
			string pattern = @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}:\d{4,5}";
			Regex rg = new Regex(pattern);
			if (rg.IsMatch(text))
			{
				thisView.GoToView(lobbyView);
			}
		}
	}
}
