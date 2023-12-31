using RVP;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class YouSureDialog : MonoBehaviour
{
   public GameObject notInteractableExternalButtonsContainer;
	public Coroutine hideCo;
	void SetInteractibilityOfButtons(bool toValue)
	{
		for (int i = 0; i < notInteractableExternalButtonsContainer.transform.childCount; ++i)
		{
			var button = notInteractableExternalButtonsContainer.transform.GetChild(i).GetComponent<Button>();
			if(button)
				button.interactable = toValue;
		}
	}
	public void HidePanel()
	{
		F.PlaySlideOutOnChildren(transform);
		if (hideCo != null)
			StopCoroutine(hideCo);
		hideCo = StartCoroutine(HidePanelIn(0.6f));
	}
	IEnumerator HidePanelIn(float timer)
	{
		for (int i = 0; i < 10000; ++i)
		{
			if (timer < 0)
			{
				gameObject.SetActive(false);
				yield break;
			}
			timer -= Time.deltaTime;
			yield return null;
		}
	}
	private void OnEnable()
	{
		SetInteractibilityOfButtons(false);
		transform.GetChild(1).GetComponent<Button>().Select();
	}
	private void OnDisable()
	{
		SetInteractibilityOfButtons(true);
		notInteractableExternalButtonsContainer.transform.GetChild(2).GetComponent<Button>().Select();
		gameObject.SetActive(false);
	}
}
