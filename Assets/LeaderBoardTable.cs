using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardTable : MonoBehaviour
{
	public GameObject LeaderboardRowPrefab;
	public Sprite knob;
	public Sprite crown;
	public void Refresh()
	{
		// TITLE + PLAYERS
		for(int i=1; i<transform.childCount; ++i)
		{
			Destroy(transform.GetChild(i).gameObject);
		}
		foreach (var player in Info.onlinePlayers)
		{
			Add(player);
		}
	}
	void Add(OnlinePlayer player)
	{
		var newRow = Instantiate(LeaderboardRowPrefab, transform).transform;
		newRow.name = player.name;
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().color = player.ready ? Color.green : Color.red;
		newRow.GetChild(0).GetChild(0).GetComponent<Image>().sprite = (player.Id == Info.hostId) ? crown : knob;
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().text = player.name;
		newRow.GetChild(0).GetChild(1).GetComponent<TextMeshProUGUI>().color = player.nickColor;
		newRow.GetChild(1).GetComponent<TextMeshProUGUI>().text = player.carName;
		newRow.GetChild(2).GetComponent<TextMeshProUGUI>().text = player.score.ToString();
		newRow.GetChild(3).GetComponent<TextMeshProUGUI>().text = player.won.ToString();
	}
}

