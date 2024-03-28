using UnityEngine;
using UnityEngine.UI;

internal class ServerListRowLobbyJoiner : MonoBehaviour
{
	ServerList serverlist;

	public void Set(ServerList list, string joinCode, bool hasPassword)
	{
		serverlist = list;
		name = joinCode;
		if(hasPassword)
		{
			GetComponent<Button>().onClick.AddListener(() => serverlist.enterPassWnd.GetComponent<EnterPasswordWnd>().OpenWindow(joinCode));
		}
		else
			GetComponent<Button>().onClick.AddListener(() => serverlist.JoinLobby(name));
	}

}