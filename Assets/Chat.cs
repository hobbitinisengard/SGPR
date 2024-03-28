using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
public class Chat : NetworkBehaviour
{
	public GameObject chatRowPrefab;

	ServerConnection server;
	Transform content;
	[NonSerialized]
	public TMP_InputField inputField;
	int rows = 7;
	// Chat is in-scene placed
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		StartCoroutine(Initialize());
	}
	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		inputField.onSelect.RemoveAllListeners();
		inputField.onDeselect.RemoveAllListeners();
		inputField.onSubmit.RemoveAllListeners();
	}
	IEnumerator Initialize()
	{
		while (Info.mpSelector == null)
			yield return null;

		server = Info.mpSelector.server;
		content = Info.mpSelector.chatContent;
		inputField = Info.mpSelector.chatInputField;

		Info.mpSelector.server.callbacks.PlayerLeft += Callbacks_PlayerLeft;
		Info.mpSelector.server.callbacks.PlayerJoined += Callbacks_PlayerJoined;
		Info.mpSelector.OnHostChanged += MpSelector_OnHostChanged;

		inputField.onSelect.AddListener(s => { Info.mpSelector.EnableSelectionOfTracks(false); });
		inputField.onDeselect.AddListener(s => { Info.mpSelector.EnableSelectionOfTracks(server.AmHost && !server.PlayerMe.ReadyGet()); });
		inputField.onSubmit.AddListener(s =>
		{
			if (s.Length > 0)
			{
				AddChatRow(server.PlayerMe, s);
				Debug.Log("sent");
				inputField.text = "";
				inputField.Select();
			}
		});
	}

	private void MpSelector_OnHostChanged()
	{
		Player p = server.lobby.Players.First(p => p.Id == server.lobby.HostId);
		AddChatRowRpc(p.NameGet(), "is hosting now", p.ReadColor(), Color.gray, RpcTarget.Everyone);
	}

	public void Callbacks_PlayerJoined(List<LobbyPlayerJoined> newPlayers)
	{
		foreach (var p in newPlayers)
		{
			AddChatRowRpc(p.Player.NameGet(), "has joined the server", Color.white, Color.gray, RpcTarget.Everyone);
		}
	}
	public void Callbacks_PlayerLeft(List<int> players)
	{
		foreach (var p in players)
		{
			AddChatRowRpc(server.lobby.Players[p].NameGet(), "has left the server", Color.white, Color.gray, RpcTarget.Everyone);
		}
	}
	public void AddChatRow(Player p, string msg)
	{
		AddChatRowRpc(p.NameGet(), msg, p.ReadColor(), Color.white, RpcTarget.Everyone);
	}
	[Rpc(SendTo.Everyone, AllowTargetOverride = true)]
	public void AddChatRowRpc(string name, string message, Color colorA, Color colorB, RpcParams rpcParams)
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
