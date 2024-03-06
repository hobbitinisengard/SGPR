using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class Chat : MonoBehaviour
{
	public GameObject chatRowPrefab;
	public ServerConnection server;
	public Transform content;
	public TMP_InputField inputField;
	int rows = 7;

	private void Awake()
	{
		inputField.onSubmit.AddListener(async s =>
		{
			if(s.Length > 0)
			{
				var p = server.PlayerMe;
				p.MessageSet(s);
				AddChatRow(p, s);
				await server.UpdatePlayerData();
				Debug.Log("sent");
				inputField.text = "";
				inputField.Select();
			}
		});
	}
	public void WriteMessageOnChat(string text)
	{
		var player = server.PlayerMe;
		var name = player.NameGet();
		var color = player.ReadColor();
		AddChatRow(name, text, color, Color.white);
	}

	public void AddChatRow(Player p, string msg)
	{
		AddChatRow(p.NameGet(),msg, p.ReadColor(), Color.white);
	}
	public void AddChatRow(string subject, string strB, Color colorA, Color colorB)
	{
		if (content.childCount == rows)
		{
			Destroy(transform.GetChild(0).gameObject);
		}
		var newRow = Instantiate(chatRowPrefab, content);
		var textA = newRow.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		textA.text = subject + ":";
		textA.color = colorA;
		var textB = newRow.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
		textB.text = "  " + strB;
		textB.color = colorB;
	}
}
