using UnityEngine;
using UnityEngine.UI;

internal class ServerListRowLobbyJoiner : MonoBehaviour
{
	ServerList serverlist;
	string code;

	public void Set(ServerList list, string joinCode)
	{
		serverlist = list;
		code = joinCode;
		GetComponent<Button>().onClick.AddListener(()=>serverlist.JoinLobby(code));
	}
}