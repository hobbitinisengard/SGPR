using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class ServerList : MonoBehaviour
{
	public GameObject tooManyPlayersText;
	public Button startAServerButton;
	public Transform content;
	public GameObject rowPrefab;
	public EnterPasswordWnd enterPassWnd;
	public MainMenuView lobbyView;
	public MainMenuView thisView;
	public ServerC server;
	public Sprite padlock;
	public Sprite knob;
	[NonSerialized]
	public GameObject buttonFromWhichWeJoinServer;
	float lastRefreshTime = 0;
	public void OnPasswordEntered(string code, string pass)
	{
		server.password = pass;
		JoinLobby(code);
	}

	private void OnEnable()
	{
		server.DisconnectFromLobby();
		startAServerButton.interactable = false;
		tooManyPlayersText.SetActive(false);
		content.DestroyAllChildren();
		Refresh();
	}
	public async void Refresh()
	{
		if (Time.time - lastRefreshTime < 3)
			return;

		lastRefreshTime = Time.time;
		await Task.Delay(2000);
		int playersOnlineRN = 0;
		try
		{
			QueryLobbiesOptions options = new()
			{
				Count = 25,

				// Filter for open lobbies only
				//options.Filters = new List<QueryFilter>()
				//{
				//	new QueryFilter(
				//		field: QueryFilter.FieldOptions.AvailableSlots,
				//		op: QueryFilter.OpOptions.GT,
				//		value: "0")
				//};

				// Order by newest lobbies first
				Order = new List<QueryOrder>()
				{
					new (asc: true, field: QueryOrder.FieldOptions.AvailableSlots)
				}
			};

			QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

			content.DestroyAllChildren();
			foreach(var lobby in lobbies.Results)
			{
				var newRow = Instantiate(rowPrefab, content).transform;
				newRow.name = lobby.Id;
				newRow.GetComponent<ServerListRowLobbyJoiner>().Set(this, lobby.Id, lobby.HasPassword);
				newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = lobby.HasPassword ? padlock : knob;
				newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = (lobby.AvailableSlots == 0) ? Color.red : Color.green;
				newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Name;
				newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Data[ServerC.k_actionHappening].Value;
				newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = (lobby.MaxPlayers - lobby.AvailableSlots).ToString() + "/" + lobby.MaxPlayers.ToString();
				playersOnlineRN += lobby.MaxPlayers - lobby.AvailableSlots;
			}
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e);
		}

		tooManyPlayersText.SetActive(playersOnlineRN > 45);
		startAServerButton.interactable = (!tooManyPlayersText.activeSelf);
		if (tooManyPlayersText.activeSelf)
			content.DestroyAllChildren();
	}
	public async void JoinLobby(string joinId)
	{
		try
		{
			if (await server.JoinLobby(joinId))
			{
				thisView.GoToView(lobbyView.gameObject);
			}
		}
		catch
		{
			if(buttonFromWhichWeJoinServer)
			{
				Destroy(buttonFromWhichWeJoinServer);
			}
		}
	}
}
