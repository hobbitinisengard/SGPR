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
	public Transform[] contents = new Transform[2];
	[NonSerialized]
	public TMP_InputField[] inputFields = new TMP_InputField[2];

	public InputActionReference chatButtonInput;

	public bool texting { get; private set; }
	const int rows = 7;
	Coroutine showChatCo;
	// Chat is in-scene placed
	public override void OnNetworkSpawn()
	{
		
		StartCoroutine(Initialize());

		Info.ActivePlayers.Add(new LobbyRelayId { 
			playerLobbyId = AuthenticationService.Instance.PlayerId, 
			playerRelayId =  NetworkManager.LocalClientId
		});
		base.OnNetworkSpawn();
	}
	public override void OnNetworkDespawn()
	{
		SetVisibility(false);
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

		SetVisibility(false);

		Info.mpSelector.server.callbacks.PlayerLeft += Callbacks_PlayerLeft;
		Info.mpSelector.server.callbacks.PlayerJoined += Callbacks_PlayerJoined;
		Info.chat = this;

		foreach (var i in inputFields)
		{
			i.onSelect.AddListener(s => { texting = true;  Info.mpSelector.EnableSelectionOfTracks(false); });
			i.onDeselect.AddListener(s => { texting = false; Info.mpSelector.EnableSelectionOfTracks(server.AmHost && !server.PlayerMe.ReadyGet()); });
			i.onSubmit.AddListener(s =>
			{
				if (showChatCo != null)
					StopCoroutine(showChatCo);
				showChatCo = StartCoroutine(HideRaceChatAfterSeconds(10));

				if (s.Length > 0)
				{
					AddChatRow(server.PlayerMe, s);
					
					i.text = "";
					if(Info.actionHappening == ActionHappening.InRace)
					{
						F.Deselect();
						inputFields[1].gameObject.SetActive(false);
					}
				}
			});
		}
	}
	void SetVisibility(bool enabled)
	{
		inputFields[1].gameObject.SetActive(enabled);
		contents[1].gameObject.SetActive(enabled);
	}
	private void buttonPressed(InputAction.CallbackContext obj)
	{
		StartCoroutine(ButtonPressedSeq());
	}
	IEnumerator ButtonPressedSeq()
	{
		if (showChatCo != null)
			StopCoroutine(showChatCo);

		SetVisibility(true);

		yield return null;

		if (Info.actionHappening == ActionHappening.InRace)
			inputFields[1].Select();
	}

	IEnumerator HideRaceChatAfterSeconds(float timer)
	{
		SetVisibility(true);

		while (timer > 0)
		{
			Debug.Log(timer);
			timer -= Time.deltaTime;
			yield return null;
		}
		SetVisibility(false);
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
