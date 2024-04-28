using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using static UnityEngine.GraphicsBuffer;
[Serializable]
public class LobbyRelayId
{
	public ulong playerRelayId;
	public string playerLobbyId;
}
public class Chat : NetworkBehaviour
{
	public GameObject chatRowPrefab;

	[NonSerialized]
	public ScrollRect[] scrollRects = new ScrollRect[2];
	[NonSerialized]
	public TMP_InputField[] inputFields = new TMP_InputField[2];

	public bool texting { get; private set; }
	Coroutine showChatCo;
	// Chat is in-scene placed
	private void Awake()
	{
		F.I.chat = this;
	}
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		StartCoroutine(Initialize());
	}
	public override void OnNetworkDespawn()
	{
		SetVisibility(false);
		//ServerC.I.activePlayers.Clear();

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
		Debug.Log("chat INitialize()");
		while (MultiPlayerSelector.I == null)
			yield return null;

		F.I.chatButtonInput.action.performed += buttonPressed;
		F.I.quickMessageRef.action.performed += QuickMessagePressed;
		inputFields[0] = MultiPlayerSelector.I.chatInitializer.lobbyChatInputField;
		inputFields[1] = MultiPlayerSelector.I.chatInitializer.raceChatInputField;

		scrollRects[0] = MultiPlayerSelector.I.chatInitializer.lobbyChat;
		scrollRects[1] = MultiPlayerSelector.I.chatInitializer.raceChat;

		SetVisibility(false);

		ServerC.I.callbacks.PlayerJoined += Callbacks_PlayerJoined;

		F.I.chat = this;

		foreach(var c in scrollRects)
			F.DestroyAllChildren(c.content);

		foreach (var i in inputFields)
		{
			i.onSelect.AddListener(s => { texting = true;  MultiPlayerSelector.I.EnableSelectionOfTracks(false); });
			i.onDeselect.AddListener(s => { texting = false; MultiPlayerSelector.I.EnableSelectionOfTracks(ServerC.I.AmHost && !ServerC.I.PlayerMe.ReadyGet()); });
			i.onSubmit.AddListener(s =>
			{
				if (showChatCo != null)
					StopCoroutine(showChatCo);
				showChatCo = StartCoroutine(HideRaceChatAfterSeconds(10));

				if (s.Length > 0)
				{
					AddChatRow(ServerC.I.PlayerMe, s);
					
					i.text = "";
					if(F.I.actionHappening == ActionHappening.InRace)
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
		scrollRects[1].gameObject.SetActive(enabled);
	}
	private void buttonPressed(InputAction.CallbackContext obj)
	{
		StartCoroutine(ButtonPressedSeq());
	}
	private void QuickMessagePressed(InputAction.CallbackContext obj)
	{
		if(F.I.gameMode == MultiMode.Multiplayer && F.I.actionHappening == ActionHappening.InRace)
		{
			float val = obj.ReadValue<float>();
			AddChatRow(ServerC.I.PlayerMe, F.GetQuickMessage(val));
		}
	}
	IEnumerator ButtonPressedSeq()
	{
		if (showChatCo != null)
			StopCoroutine(showChatCo);

		SetVisibility(true);

		yield return null;

		if (F.I.actionHappening == ActionHappening.InRace)
			inputFields[1].Select();
	}

	IEnumerator HideRaceChatAfterSeconds(float timer)
	{
		SetVisibility(true);

		while (timer > 0)
		{
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
	public void PlayerLeft(Player p)
	{
		AddChatRowRpc(p.NameGet(), "has left the server", Color.white, Color.gray, RpcTarget.Everyone);
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
		showChatCo = StartCoroutine(HideRaceChatAfterSeconds(10));

		foreach(var c in scrollRects)
		{
			var newRow = Instantiate(chatRowPrefab, c.content).GetComponent<RectTransform>();
			var textA = newRow.GetChild(0).GetComponent<TextMeshProUGUI>();
			textA.text = name + ":";
			textA.color = colorA;
			var textB = newRow.GetChild(1).GetComponent<TextMeshProUGUI>();
			textB.text = "  " + message;
			textB.color = colorB;
			c.verticalScrollbar.value = 0;
			
			Canvas.ForceUpdateCanvases();
			c.verticalNormalizedPosition = 0;
		}
	}
}
