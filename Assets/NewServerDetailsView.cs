using System.Linq;
using System;
using TMPro;
using UnityEngine;
using System.Security.Cryptography;
using System.IO;
public class NewServerDetailsView : MainMenuView
{
   public GameObject Okbutton;
   public TextMeshProUGUI OkbuttonText;
	public MainMenuView lobbyView;

   public TMP_InputField maxPlayersInputField;
   public TMP_InputField serverNameInputField;
   public ServerConnection server;
   const string newServerNameReg = "newServerName";
   const string newServerPlayersReg = "newServerPlayers";

	public void Awake()
	{
		serverNameInputField.text = PlayerPrefs.GetString(newServerNameReg);
      maxPlayersInputField.text = PlayerPrefs.GetString(newServerPlayersReg);
		serverNameInputField.onEndEdit.AddListener(SetServerName);
		maxPlayersInputField.onEndEdit.AddListener(SetMaxPlayers);
      SetServerName(serverNameInputField.text);
      SetMaxPlayers(maxPlayersInputField.text);
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
   public void CheckOKButton()
   {
      Okbutton.SetActive(maxPlayersInputField.text.Length > 0 && serverNameInputField.text.Length > 0);
	}
	public async void CreateLobby()
	{
		OkbuttonText.text = "WAIT";
		string trackName = Info.tracks.Keys.First();
		string sha = SHA(Info.tracksPath + trackName + ".data");
		Debug.Log("CreateLobby start" + trackName + " " + sha);
		if (await server.CreateLobby(trackName, sha))
		{
			GoToView(lobbyView.gameObject);
		}
	}
	string SHA(string filePath)
	{
		string hash;
		using (var cryptoProvider = new SHA1CryptoServiceProvider())
		{
			byte[] buffer = File.ReadAllBytes(filePath);
			hash = BitConverter.ToString(cryptoProvider.ComputeHash(buffer));
		}
		return hash;
	}
}
