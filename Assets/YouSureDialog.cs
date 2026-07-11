using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class YouSureDialog : MonoBehaviour
{
  public GameObject notInteractableExternalButtonsContainer;
	public Coroutine hideCo;
	[NonSerialized]
	public Button selectMeAfterDisable;
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
		selectMeAfterDisable = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
		SetInteractibilityOfButtons(false);
		transform.GetChild(1).GetComponent<Button>().Select();
	}
	private void OnDisable()
	{
		SetInteractibilityOfButtons(true);
		selectMeAfterDisable.Select();
		gameObject.SetActive(false);
	}
}
