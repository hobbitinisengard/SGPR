using UnityEngine;

public class GreetingView : Sfxable
{
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			PlaySFX("fe-dialogconfirm");
		}
	}
}
