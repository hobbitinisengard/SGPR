using TMPro;
using UnityEngine;

public class NewServerDetailsView : MonoBehaviour
{
   public GameObject Okbutton;
   public TMP_InputField maxPlayersInputField;
   public TMP_InputField serverNameInputField;
   public ServerConnection server;
   const string newServerNameReg = "newServerName";
   const string newServerPlayersReg = "newServerPlayers";
	public void Awake()
	{
      serverNameInputField.onEndEdit.AddListener(SetServerName);
		serverNameInputField.text = PlayerPrefs.GetString(newServerNameReg);
		maxPlayersInputField.onEndEdit.AddListener(SetMaxPlayers);
      maxPlayersInputField.text = PlayerPrefs.GetString(newServerPlayersReg);
	}
	public void SetServerName(string str)
   {
      PlayerPrefs.SetString(newServerNameReg, str);
      server.lobbyName = str;
      CheckOKButton();
   }
   public void SetMaxPlayers(string val)
   {
		server.maxPlayers = Mathf.Clamp(int.Parse(val), 2, 10);
      maxPlayersInputField.text = server.maxPlayers.ToString();
		PlayerPrefs.SetString(newServerPlayersReg, maxPlayersInputField.text);
		CheckOKButton();
	}
   public void CheckOKButton()
   {
      Okbutton.SetActive(maxPlayersInputField.text.Length > 0 && serverNameInputField.text.Length > 0);
	}
}
