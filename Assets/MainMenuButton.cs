using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using RVP;

public class MainMenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
	static Color32 deselectedColor = new Color32(200, 198, 250, 255);
	static Color32 selectedColor = new Color32(255, 255, 255, 255);
	public Sprite dyndakSpriteOnSelect;
	public string BottomTextOnSelect;
	Text text;
	MainMenuView mainMenuView;
	[System.NonSerialized]
	public Button buttonComponent;
	public void OnDeselect(BaseEventData eventData)
	{
		text.color = deselectedColor;
	}

	public void OnSelect(BaseEventData eventData)
	{
		text.color = selectedColor;
		if (dyndakSpriteOnSelect)
			mainMenuView.dyndak.sprite = dyndakSpriteOnSelect;
		if (!string.IsNullOrEmpty(BottomTextOnSelect))
		{
			mainMenuView.bottomText.text = BottomTextOnSelect;
		}
		mainMenuView.dyndak.GetComponent<SlideInOut>().PlaySlideIn();
	}
	void Start()
	{
		text = transform.GetChild(0).GetComponent<Text>();
		text.color = deselectedColor;
		buttonComponent = transform.GetComponent<Button>();
		mainMenuView = transform.FindParentComponent<MainMenuView>();//transform.parent.GetComponent<MainMenuView>();
	}
}
