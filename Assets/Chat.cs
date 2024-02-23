using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class Chat : MonoBehaviour
{
	public GameObject chatRowPrefab;
	public ServerConnection server;
	public Transform content;
	int rows = 30;
	
	public void WriteMessageOnChat(string text)
	{
		var player = server.PlayerMe();
		var name = player.NameGet();
		var color = player.ReadColor();
		AddChatRow(name, text, color, Color.white);
	}

	public void AddChatRow(Player p, string msg)
	{
		AddChatRow(p.NameGet(), msg, p.ReadColor(), Color.white);
	}
	public void AddChatRow(string strA, string strB, Color colorA, Color colorB)
	{
		if (transform.childCount == rows)
		{
			Destroy(transform.GetChild(0).gameObject);
		}
		var newRow = Instantiate(chatRowPrefab, content);
		var textA = newRow.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		textA.text = strA;
		textA.color = colorA;
		var textB = newRow.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
		textB.text = strB;
		textB.color = colorB;
	}
}
