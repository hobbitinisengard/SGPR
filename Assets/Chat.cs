using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
[Serializable]
public class LobbyRelayId
{
	public ulong playerRelayId;
	public string playerLobbyId;
}
public class Chat : NetworkBehaviour
{
	public GameObject chatRowPrefab;

	public ScrollRect[] scrollRects;
	public TMP_InputField[] inputFields;

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
		//Debug.Log("chat INitialize()");
		while (MultiPlayerSelector.I == null)
			yield return null;
	
		F.I.chatButtonInput.action.performed += buttonPressed;
		F.I.quickMessageRef.action.performed += QuickMessagePressed;
		//inputFields[0] = MultiPlayerSelector.I.chatInitializer.lobbyChatInputField;
		//inputFields[1] = MultiPlayerSelector.I.chatInitializer.raceChatInputField;
	
		//scrollRects[0] = MultiPlayerSelector.I.chatInitializer.lobbyChat;
		//scrollRects[1] = MultiPlayerSelector.I.chatInitializer.raceChat;
	
		SetVisibility(false);
	
		ServerC.I.callbacks.PlayerJoined += Callbacks_PlayerJoined;

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
	public void SetVisibility(bool enabled)
	{
		if (F.I.gameMode == MultiMode.Multiplayer)
		{
			inputFields[1].gameObject.SetActive(enabled);
			scrollRects[1].gameObject.SetActive(enabled);
		}
	}
	private void buttonPressed(InputAction.CallbackContext obj)
	{
		StartCoroutine(ButtonPressedSeq());
	}
	private void QuickMessagePressed(InputAction.CallbackContext obj)
	{
		if(F.I.gameMode == MultiMode.Multiplayer && F.I.actionHappening == ActionHappening.InRace && !texting)
		{
			int msgIndex = (int)obj.ReadValue<float>(); // returns 1 - 10
			string msg = F.GetQuickMessage(msgIndex - 1);
			if(msg.Length > 0)
				AddChatRow(ServerC.I.PlayerMe, msg);
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
		yield return new WaitForSecondsRealtime(timer);
		SetVisibility(false);
	}
		
	public void Callbacks_PlayerJoined(List<LobbyPlayerJoined> newPlayers)
	{
		foreach (var p in newPlayers)
		{
			AddChatRowRpc(p.Player.NameGet(), Encoding.UTF8.GetBytes("has joined the server"), Color.white, Color.gray);
		}
	}
	public void PlayerLeft(Player p)
	{
		AddChatRowRpc(p.NameGet(), Encoding.UTF8.GetBytes("has left the server"), Color.white, Color.gray);
	}
	public void AddChatRowAsServer(string msg)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(msg); // for polish chars
		AddChatRowRpc("", bytes, Color.gray, Color.gray);
	}
	public void AddChatRow(Player p, string msg, Color? color = null)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(msg); // for polish chars
		color ??= Color.white;
		
		AddChatRowRpc(p.NameGet(), bytes, p.ReadColor(), color.Value);
	}
	[Rpc(SendTo.Everyone)]
	public void AddChatRowRpc(string name, byte[] msgBytes, Color32 colorA, Color32 colorB)
	{
		string msg = Encoding.UTF8.GetString(msgBytes);
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
			textB.text = "  " + msg;
			textB.color = colorB;
		}
		UpdateCanvases();
	}
	public void UpdateCanvases()
	{
		StartCoroutine(UpdateCanvasesCo());
	}
	public IEnumerator UpdateCanvasesCo()
	{
		yield return null;
		foreach (var c in scrollRects)
		{
			if (c.gameObject.activeInHierarchy)
			{
				c.verticalScrollbar.value = 0;
				Canvas.ForceUpdateCanvases();
				c.verticalNormalizedPosition = 0;
			}
		}
	}
}
