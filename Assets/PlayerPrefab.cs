using UnityEngine;
using Unity.Netcode;

public class PlayerPrefab : NetworkBehaviour
{
	public Chat lobbyChat;
	public Color nickColor;
	public string playerName;
	public NetworkVariable<string> message = new(null, writePerm:NetworkVariableWritePermission.Owner);
	private void Awake()
	{
		message.OnValueChanged += AddChatRow;
	}
	public void Initialize(Chat lobbyChat, Color nickColor, string playerName)
	{
		this.lobbyChat = lobbyChat;
		this.nickColor = nickColor;
		this.playerName = playerName;
	}

	private void AddChatRow(string _, string newValue)
	{
		lobbyChat.AddChatRowRpc(playerName ,newValue, nickColor, Color.white);
	}
}
