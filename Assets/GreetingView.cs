using UnityEngine;
using UnityEngine.UI;

public class GreetingView : Sfxable
{
	public Text versionText;
	private void Awake()
	{
		versionText.text = Info.version;
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			PlaySFX("fe-dialogconfirm");
		}
	}
}
