using System;
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
   public ServerC server;
	public new void Awake()
	{
		base.Awake();
		serverNameInputField.text = F.I.playerData.serverName;
		maxPlayersInputField.text = F.I.playerData.serverMaxPlayers;
      passInputField.text = F.I.playerData.serverPassword;
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
		OkbuttonText.text = F.I.LocStr("OK");
	}
	public void SetServerName(string str)
   {
		F.I.playerData.serverName = str;
      server.lobbyName = str;
      CheckOKButton();
   }

   public void SetMaxPlayers(string val)
   {
		server.maxPlayers = Mathf.Clamp(int.Parse(val), 2, 10);
      maxPlayersInputField.text = server.maxPlayers.ToString();
		F.I.playerData.serverMaxPlayers = maxPlayersInputField.text;
		CheckOKButton();
	}
	public void SetPassword(string str)
	{
		F.I.playerData.serverPassword = str;
		server.password = str;
		CheckOKButton();
	}
	public void CheckOKButton()
   {
      Okbutton.SetActive(!server.isCreatingLobby && maxPlayersInputField.text.Length > 0 && serverNameInputField.text.Length > 0
			 && (passInputField.text.Length > 7 || passInputField.text.Length == 0));
	}
	public async void CreateLobby()
	{
		prevViewForbidden = true;
		OkbuttonText.text = F.I.LocStr("WAIT");
		F.I.SaveSettingsDataToJson();
		// Preserve the multiplayer selector's choice instead of always advertising
		// the first track in the catalog when a new lobby is created.
		string trackName = MultiPlayerSelector.I ? MultiPlayerSelector.I.SelectedLobbyTrack : null;
		if (string.IsNullOrEmpty(trackName) || !F.I.tracks.TryGetValue(trackName, out var track) || !track.valid || !track.unlocked)
			trackName = F.I.s_trackName;
		if (string.IsNullOrEmpty(trackName) || !F.I.tracks.TryGetValue(trackName, out track) || !track.valid || !track.unlocked)
			trackName = F.I.tracks.First(kv => kv.Value.valid && kv.Value.unlocked).Key;
		F.I.s_trackName = trackName;
		string sha = F.I.SHA(F.I.tracksPath + trackName + ".data");
		Debug.Log("CreateLobby start" + F.I.s_trackName + " " + sha);
		try
		{
			if(await server.CreateLobby(trackName, sha))
			{
				prevViewForbidden = false;
				GoToView(lobbyView);
			}
		}
		catch(Exception e)
		{
			Debug.Log(e.Message);
		}
		prevViewForbidden = false;
	}	
}
