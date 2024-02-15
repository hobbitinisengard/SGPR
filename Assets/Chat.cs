using System.Linq;
using TMPro;
using UnityEngine;

public class Chat : MonoBehaviour
{
	public GameObject chatRowPrefab;
	int rows = 10;
	public void WriteMessageOnChat(string text)
	{
		var player = Info.onlinePlayers.First(p => p.Id == 0);
		AddChatRow(player.name, text, player.nickColor, Color.white);
	}

	public void AddChatRow(string strA, string strB)
	{
		AddChatRow(strA, strB, Color.yellow, Color.white);
	}
	public void AddChatRow(string strA, string strB, Color colorA, Color colorB)
	{
		if (transform.childCount == rows)
		{
			Destroy(transform.GetChild(0).gameObject);
		}
		var newRow = Instantiate(chatRowPrefab, transform);
		var textA = newRow.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		textA.text = strA;
		textA.color = colorA;
		var textB = newRow.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
		textB.text = strB;
		textB.color = colorB;
	}
}
