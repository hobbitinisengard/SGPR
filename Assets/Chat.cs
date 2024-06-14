using System.Collections;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class Chat : NetworkBehaviour
{
	public GameObject chatRowPrefab;

	public ScrollRect[] scrollRects;
	public TMP_InputField[] inputFields;

	public Button readyButton;

	public bool texting { get; private set; }
	Coroutine showChatCo;
	// Chat is in-scene placed
	private void Awake()
	{
		F.I.chatButtonInput.action.performed += buttonPressed;
		F.I.quickMessageRef.action.performed += QuickMessagePressed;
		F.I.viewSwitcher.OnWorldMenuSwitch += F.Deselect;
	}
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		Initialize();
	}
	public override void OnNetworkDespawn()
	{
		SetVisibility(false);

		foreach (var i in inputFields)
		{
			i.onSelect.RemoveAllListeners();
			i.onDeselect.RemoveAllListeners();
			i.onSubmit.RemoveAllListeners();
		}

		base.OnNetworkDespawn();
	}
	void Initialize()
	{
		SetVisibility(false);

		foreach(var c in scrollRects)
			F.DestroyAllChildren(c.content);

		foreach (var i in inputFields)
		{
			i.onSelect.AddListener(s => { texting = true;  MultiPlayerSelector.I.EnableSelectionOfTracks(false); });
			i.onDeselect.AddListener(s => { texting = false; MultiPlayerSelector.I.EnableSelectionOfTracks(ServerC.I.AmHost && !ServerC.I.PlayerMe.ReadyGet()); });
			i.onSubmit.AddListener(s =>
			{
				readyButton.Select();

				if (s.Length > 0)
				{
					AddChatRow(ServerC.I.PlayerMe, s);
					
					i.text = "";
					if(F.I.actionHappening == ActionHappening.InRace)
					{
						//F.Deselect();
						inputFields[1].gameObject.SetActive(false);
					}
				}
				else
				{
					if (showChatCo != null)
						StopCoroutine(showChatCo);
					showChatCo = StartCoroutine(HideRaceChatAfterSeconds(5));
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
		if(F.I.gameMode == MultiMode.Multiplayer && !texting)
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
		else
			inputFields[0].Select();
	}

	IEnumerator HideRaceChatAfterSeconds(float timer)
	{
		SetVisibility(true);
		yield return new WaitForSecondsRealtime(timer);
		SetVisibility(false);
	}
	
	public void PlayerLeft(Player p)
	{
		AddChatRowLocally(p.NameGet(), "has left the server", Color.white, Color.grey);
	}
	public void AddChatRowAsServer(string msg)
	{
		AddChatRowLocally("", msg, Color.gray, Color.gray);
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
		AddChatRowLocally(name, msg, colorA, colorB);
	}
	public void AddChatRowLocally(string name, string msg, Color32 colorA, Color32 colorB)
	{
		if (showChatCo != null)
			StopCoroutine(showChatCo);
		showChatCo = StartCoroutine(HideRaceChatAfterSeconds(5));

		foreach (var c in scrollRects)
		{
			var newRow = Instantiate(chatRowPrefab, c.content).GetComponent<RectTransform>();
			var textA = newRow.GetChild(0).GetComponent<TextMeshProUGUI>();
			textA.text = name;
			textA.color = colorA;
			var textB = newRow.GetChild(1).GetComponent<TextMeshProUGUI>();
			textB.text = ((textA.text.Length > 0) ? ":  " : "") + msg;
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
