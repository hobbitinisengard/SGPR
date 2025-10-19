using UnityEngine.EventSystems;
using UnityEngine.UI;

internal class ServerListRowLobbyJoiner : Sfxable, ISelectHandler
{
	ServerList serverlist;
	public void OnSelect(BaseEventData eventData)
	{
		PlaySFX("fe-dialogmove");
	}
	public void Set(ServerList list, string joinCode, bool hasPassword, bool joinable)
	{
		
		serverlist = list;
		name = joinCode;
		if (joinable)
		{
			if (hasPassword)
			{
				GetComponent<Button>().onClick.AddListener(() => serverlist.enterPassWnd.GetComponent<EnterPasswordWnd>().OpenWindow(joinCode));
			}
			else
				GetComponent<Button>().onClick.AddListener(() => { serverlist.buttonFromWhichWeJoinServer = this.gameObject; serverlist.JoinLobby(name); });
		}
		else
		{
			GetComponent<Button>().onClick.AddListener(() => PlaySFX("fe-cardserror"));
		}
	}
}