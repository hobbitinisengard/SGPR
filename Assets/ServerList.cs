using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class ServerList : MonoBehaviour
{
	public Transform content;
	public GameObject rowPrefab;
	public EnterPasswordWnd enterPassWnd;
	public MainMenuView lobbyView;
	public MainMenuView thisView;
	public ServerConnection server;
	public Sprite padlock;
	public Sprite knob;
	float lastRefreshTime = 0;
	public void OnPasswordEntered(string code, string pass)
	{
		server.password = pass;
		JoinLobby(code);
	}

	private void OnEnable()
	{
		server.DisconnectFromLobby();
		Refresh();
	}
	public async void Refresh()
	{
		if (Time.time - lastRefreshTime < 3)
			return;

		lastRefreshTime = Time.time;

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
				newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Data[ServerConnection.k_actionHappening].Value;
				newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = (lobby.MaxPlayers - lobby.AvailableSlots).ToString() + "/" + lobby.MaxPlayers.ToString();
				playersOnlineRN += lobby.MaxPlayers - lobby.AvailableSlots;
			}
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e);
		}

		if(playersOnlineRN > 45)
		{
			thisView.GoToView(thisView.prevView);
		}
	}
	public async void JoinLobby(string joinId)
	{
		if(await server.JoinLobby(joinId))
		{
			thisView.GoToView(lobbyView.gameObject);
		}
	}
}
