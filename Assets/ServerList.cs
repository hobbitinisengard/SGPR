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
	public MainMenuView lobbyView;
	public MainMenuView thisView;
	public ServerConnection server;
	private void OnEnable()
	{
		server.DisconnectFromLobby();
		Refresh();
	}
	public async void Refresh()
	{
		try
		{
			QueryLobbiesOptions options = new QueryLobbiesOptions();
			options.Count = 25;

			// Filter for open lobbies only
			options.Filters = new List<QueryFilter>()
			{
				new QueryFilter(
					field: QueryFilter.FieldOptions.AvailableSlots,
					op: QueryFilter.OpOptions.GT,
					value: "0")
			};

			// Order by newest lobbies first
			options.Order = new List<QueryOrder>()
			{
				new QueryOrder(
					asc: false,
					field: QueryOrder.FieldOptions.Created)
			};

			QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

			content.DestroyAllChildren();
			foreach(var lobby in lobbies.Results)
			{
				var newRow = Instantiate(rowPrefab, content).transform;
				newRow.name = lobby.Data[ServerConnection.k_keyJoinCode].Value;
				newRow.GetComponent<Button>().onClick.AddListener(()=>JoinLobby(newRow.name));
				newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = (lobby.AvailableSlots == 0) ? Color.red : Color.green;
				newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Name;
				newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = lobby.Data[ServerConnection.k_description].Value;
				newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = lobby.AvailableSlots.ToString() + "/" + lobby.MaxPlayers.ToString();
			}
		}
		catch (LobbyServiceException e)
		{
			Debug.Log(e);
		}
	}
	async void JoinLobby(string joinCode)
	{
		if(await server.JoinLobby(joinCode))
		{
			thisView.GoToView(lobbyView.gameObject);
		}
	}
	
}
