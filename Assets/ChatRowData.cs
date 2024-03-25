using UnityEngine;

public class ChatRowData
{
	public string playerName;
	public string msg;
	public Color playerNameColor;
	public Color msgColor;

	public void Set(string name, string text, Color playerNameColor, Color msgColor)
	{
		this.playerName = name;
		this.msg = text;
		this.playerNameColor = playerNameColor;
		this.msgColor = msgColor;
	}
}
