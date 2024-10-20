using TMPro;
using UnityEngine;

public class QuickMessagesSetup : MonoBehaviour
{
	private void OnEnable()
	{
		for (int i = 0; i < transform.childCount; ++i)
		{
			transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text = F.GetQuickMessage(i);
		}
	}
	private void OnDisable()
	{
		for (int i = 0; i < transform.childCount; ++i)
		{
			F.SetQuickMessage(i, transform.GetChild(i).GetChild(0).GetComponent<TMP_InputField>().text);
		}
	}
}
