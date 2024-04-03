using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
public class LobbyRelayId
{
	public ulong playerRelayId;
	public string playerLobbyId;
}
public class Chat : NetworkBehaviour
{
	public GameObject chatRowPrefab;

	ServerConnection server;
	[NonSerialized]
	public Transform[] contents;
	[NonSerialized]
	public TMP_InputField[] inputFields;

	public InputActionReference chatButtonInput;
	int rows = 7;
	Coroutine showChatCo;
	// Chat is in-scene placed
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		StartCoroutine(Initialize());

		Info.ActivePlayers.Add(new LobbyRelayId { 
			playerLobbyId = AuthenticationService.Instance.PlayerId, 
			playerRelayId =  NetworkManager.LocalClientId
		});
	}
	public override void OnNetworkDespawn()
	{
		Info.ActivePlayers.Remove(Info.ActivePlayers.First(ap => ap.playerLobbyId == AuthenticationService.Instance.PlayerId));

		base.OnNetworkDespawn();
		foreach(var i in inputFields)
		{
			i.onSelect.RemoveAllListeners();
			i.onDeselect.RemoveAllListeners();
			i.onSubmit.RemoveAllListeners();
		}
	}
	IEnumerator Initialize()
	{
		while (Info.mpSelector == null)
			yield return null;

		chatButtonInput.action.performed += buttonPressed;
		server = Info.mpSelector.server;
		inputFields[0] = Info.mpSelector.chatInitializer.lobbyChatInputField;
		inputFields[1] = Info.mpSelector.chatInitializer.raceChatInputField;

		contents[0] = Info.mpSelector.chatInitializer.lobbyChatContent;
		contents[1] = Info.mpSelector.chatInitializer.raceChatContent;

		Info.mpSelector.server.callbacks.PlayerLeft += Callbacks_PlayerLeft;
		Info.mpSelector.server.callbacks.PlayerJoined += Callbacks_PlayerJoined;
		Info.chat = this;

		foreach (var i in inputFields)
		{
			i.onSelect.AddListener(s => { Info.mpSelector.EnableSelectionOfTracks(false); });
			i.onDeselect.AddListener(s => { Info.mpSelector.EnableSelectionOfTracks(server.AmHost && !server.PlayerMe.ReadyGet()); });
			i.onSubmit.AddListener(s =>
			{
				if (s.Length > 0)
				{
					AddChatRow(server.PlayerMe, s);
					
					i.text = "";
					i.Select();
				}
			});
		}
		Player p = server.lobby.Players.First(p => p.Id == server.lobby.HostId);
		AddChatRowRpc(p.NameGet(), "is hosting now", p.ReadColor(), Color.gray, RpcTarget.Me);
	}

	private void buttonPressed(InputAction.CallbackContext obj)
	{
		StopCoroutine(showChatCo);
		contents[1].gameObject.SetActive(contents[1].gameObject.activeSelf);
	}

	IEnumerator HideRaceChatAfter5Seconds()
	{
		float timer = 5;
		contents[1].gameObject.SetActive(true);

		while(timer > 0)
		{
			timer -= Time.fixedDeltaTime;
			yield return null;
		}
		contents[1].gameObject.SetActive(false);
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
		if (showChatCo != null)
			StopCoroutine(showChatCo);
		showChatCo = StartCoroutine(HideRaceChatAfter5Seconds());

		if (contents[0].childCount == rows)
		{
			Destroy(contents[0].GetChild(0).gameObject);
			Destroy(contents[1].GetChild(0).gameObject);
		}
		foreach(var c in contents)
		{
			var newRow = Instantiate(chatRowPrefab, c);
			var textA = newRow.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
			textA.text = name + ":";
			textA.color = colorA;
			var textB = newRow.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
			textB.text = "  " + message;
			textB.color = colorB;
		}
	}
}
