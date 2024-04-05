using System.Linq;
using TMPro;
using UnityEngine;

public class NewServerDetailsView : MainMenuView
{
   public GameObject Okbutton;
   public TextMeshProUGUI OkbuttonText;
	public MainMenuView lobbyView;

   public TMP_InputField maxPlayersInputField;
   public TMP_InputField serverNameInputField;
   public TMP_InputField passInputField;
   public ServerConnection server;
   const string newServerNameReg = "newServerName";
   const string newServerPlayersReg = "newServerPlayers";
   const string passwordReg = "password";

	public new void Awake()
	{
		base.Awake();
		serverNameInputField.text = PlayerPrefs.GetString(newServerNameReg);
      maxPlayersInputField.text = PlayerPrefs.GetString(newServerPlayersReg);
      passInputField.text = PlayerPrefs.GetString(passwordReg);
		serverNameInputField.onEndEdit.AddListener(SetServerName);
		maxPlayersInputField.onEndEdit.AddListener(SetMaxPlayers);
		passInputField.onEndEdit.AddListener(SetPassword);
      SetServerName(serverNameInputField.text);
      SetMaxPlayers(maxPlayersInputField.text);
      SetPassword(passInputField.text);
	}
	new private void OnEnable()
	{
		base.OnEnable();
		OkbuttonText.text = "OK";
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
	public void SetPassword(string str)
	{
		PlayerPrefs.SetString(passwordReg, str);
		server.password = str;
		CheckOKButton();
	}
	public void CheckOKButton()
   {
      Okbutton.SetActive(maxPlayersInputField.text.Length > 0 && serverNameInputField.text.Length > 0);
	}
	public async void CreateLobby()
	{
		OkbuttonText.text = "WAIT";
		string trackName = F.I.tracks.First(kv => kv.Value.valid).Key;
		string sha = await F.I.SHA(F.I.tracksPath + trackName + ".data");
		Debug.Log("CreateLobby start" + trackName + " " + sha);
		if (await server.CreateLobby(trackName, sha))
		{
			GoToView(lobbyView.gameObject);
		}
	}
	/// <summary>
	/// SHA is performed on .data track file
	/// </summary>
	
}
