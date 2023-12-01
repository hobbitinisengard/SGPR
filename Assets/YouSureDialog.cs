using UnityEngine;
using UnityEngine.UI;

public class YouSureDialog : MonoBehaviour
{
   public GameObject notInteractableExternalButtonsContainer;
	void SetInteractibilityOfButtons(bool toValue)
	{
		for (int i = 0; i < notInteractableExternalButtonsContainer.transform.childCount; ++i)
		{
			notInteractableExternalButtonsContainer.transform.GetChild(i).GetComponent<Button>().interactable = toValue;
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
