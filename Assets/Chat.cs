using System;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
public class Chat : NetworkBehaviour
{
	public ServerConnection server;
	public GameObject chatRowPrefab;
	[NonSerialized]
	public PlayerPrefab playerPrefab;
	public Transform content;
	public TMP_InputField inputField;
	public GameObject chatRowDataPrefab;
	public GameObject newMessagesContainerPrefab;
	int rows = 7;

	private void Awake()
	{
		inputField.onSubmit.AddListener(s =>
		{
			if(s.Length > 0)
			{
				AddChatRow(server.PlayerMe, s);
				//playerPrefab.message.Value = s;
				Debug.Log("sent");
				inputField.text = "";
				inputField.Select();
			}
		});
	}
	

	//public void AddChatRow(
	//{
	//	var rowInstance = Instantiate(chatRowDataPrefab, transform);
	//	rowInstance.GetComponent<ChatRowData>().Set(name, message, colorA, colorB);
	//	rowInstance.GetComponent<NetworkObject>().Spawn(true);
	//}
	public void AddChatRow(Player p, string msg)
	{
		AddChatRowRpc(p.NameGet(), msg, p.ReadColor(), Color.white);
	}
	[Rpc(SendTo.ClientsAndHost)]
	public void AddChatRowRpc(string name, string message, in Color colorA, in Color colorB)
	{
		if (content.childCount == rows)
		{
			Destroy(content.GetChild(0).gameObject);
		}
		var newRow = Instantiate(chatRowPrefab, content);
		var textA = newRow.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		textA.text = name + ":";
		textA.color = colorA;
		var textB = newRow.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
		textB.text = "  " + message;
		textB.color = colorB;
	}
}
