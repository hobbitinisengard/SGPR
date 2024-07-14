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
	public TextMeshProUGUI dialogTextObj;
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
	float lastRefreshTime = -MinimumRefreshInterval;
	const int MinimumRefreshInterval = 3;
	public void OnPasswordEntered(string code, string pass)
	{
		server.password = pass;
		JoinLobby(code);
	}
	private void OnEnable()
	{
		startAServerButton.interactable = false;
		dialogTextObj.gameObject.SetActive(false);
		content.DestroyAllChildren();
		Refresh();
	}
	public void ShowErrorMessage(string msg)
	{
		dialogTextObj.gameObject.SetActive(true);
		startAServerButton.interactable = false;
		content.DestroyAllChildren();
		dialogTextObj.text = msg;
	}
	public async void Refresh()
	{
		if (Time.time - lastRefreshTime < MinimumRefreshInterval)
			return;

		lastRefreshTime = Time.time;
		content.DestroyAllChildren();

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


			foreach (var lobby in lobbies.Results)
			{
				var newRow = Instantiate(rowPrefab, content).transform;
				newRow.name = lobby.Id;
				newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = lobby.HasPassword ? padlock : knob;
				newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = (lobby.AvailableSlots == 0) ? Color.red : Color.green;
				newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Name;
				newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Data[ServerC.k_actionHappening].Value;

				if (lobby.Data.ContainsKey(ServerC.k_gameVer))
					newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = lobby.Data[ServerC.k_gameVer].Value;
				newRow.GetChild(3).GetComponent<TextMeshProUGUI>().text = (lobby.MaxPlayers - lobby.AvailableSlots).ToString() + "/" + lobby.MaxPlayers.ToString();

				bool joinable = lobby.Data.ContainsKey(ServerC.k_gameVer) && lobby.Data[ServerC.k_gameVer].Value == Info.VERSION;
				newRow.GetComponent<ServerListRowLobbyJoiner>().Set(this, lobby.Id, lobby.HasPassword, joinable);
				
				playersOnlineRN += lobby.MaxPlayers - lobby.AvailableSlots;
			}
		}
		catch (LobbyServiceException e)
		{
			ShowErrorMessage(e.Message);
		}

		if (playersOnlineRN < F.I.maxConcurrentUsers)
			startAServerButton.interactable = true;
		else
			ShowErrorMessage("Servers are overloaded. Try again later");
	}
	public async void JoinLobby(string joinId)
	{
		thisView.prevViewForbidden = true;
		try
		{
			if (await server.JoinLobby(joinId))
			{
				thisView.GoToView(lobbyView);
			}
		}
		catch (Exception e)
		{
			ShowErrorMessage(e.Message);
			if (buttonFromWhichWeJoinServer)
			{
				Destroy(buttonFromWhichWeJoinServer);
			}
		}
		thisView.prevViewForbidden = false;
	}
}
