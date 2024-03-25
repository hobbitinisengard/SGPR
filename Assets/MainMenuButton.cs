using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MainMenuButton : Sfxable, ISelectHandler, IDeselectHandler, ISubmitHandler
{
	static Color32 deselectedColor = new Color32(200, 198, 250, 255);
	static Color32 selectedColor = new Color32(255, 255, 255, 255);
	public Sprite dyndakSpriteOnSelect;
	public string BottomTextOnSelect;
	TextMeshProUGUI text;
	MainMenuView mainMenuView;
	[System.NonSerialized]
	public Button buttonComponent;
	new void Awake()
	{
		base.Awake();
		text = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		text.color = deselectedColor;
		buttonComponent = GetComponent<Button>();
		mainMenuView = transform.FindParentComponent<MainMenuView>();
	}
	private void OnDisable()
	{
		text.color = deselectedColor;
	}
	public void OnSubmit(BaseEventData eventData)
	{
		PlaySFX("fe-dialogconfirm");
	}
	public void OnDeselect(BaseEventData eventData)
	{
		text.color = deselectedColor;
		PlaySFX("fe-dialogmove");
	}
	public void OnSelect(BaseEventData eventData)
	{
		text.color = selectedColor;
		if (dyndakSpriteOnSelect)
		{
			mainMenuView.dyndak.sprite = dyndakSpriteOnSelect;
			mainMenuView.dyndak.gameObject.SetActive(false);
			mainMenuView.dyndak.gameObject.SetActive(true);
		}
		if (!string.IsNullOrEmpty(BottomTextOnSelect))
		{
			mainMenuView.bottomText.text = BottomTextOnSelect;
		}
	}
	public void Select()
	{
		if (!gameObject.activeSelf)
			StartCoroutine(SelectBtn());
		else
			buttonComponent.Select();
	}
	IEnumerator SelectBtn()
	{ 
		float timer = 1;
		while (timer > 0)
		{
			if (gameObject.activeSelf)
			{
				buttonComponent.Select();
				yield break;
			}
			timer -= Time.deltaTime;
			yield return null;
		}
	}

	
}
