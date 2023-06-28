using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainMenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    static Color32 deselectedColor = new Color32(230, 223, 253, 255);
    static Color32 selectedColor = new Color32(255, 255, 255, 255);
    public Sprite dyndakSpriteOnSelect;
    public string BottomTextOnSelect;
    Text text;
    MainMenuView mainMenuView;
    [System.NonSerialized]
    public Button buttonComponent;
    AnimationCurve slideAnimation = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public void OnDeselect(BaseEventData eventData)
    {
        text.color = deselectedColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        text.color = selectedColor;
        if(dyndakSpriteOnSelect)
            mainMenuView.dyndak.sprite = dyndakSpriteOnSelect;
        if(!string.IsNullOrEmpty(BottomTextOnSelect))
        {
            mainMenuView.bottomText.text = BottomTextOnSelect;
        }
    }
    public void SlideIn()
    {

    }
    IEnumerator SlideAnimation()
    {

        yield return null;

    }
    void Start()
    {
        text = transform.GetChild(0).GetComponent<Text>();
        buttonComponent = transform.GetComponent<Button>();
        mainMenuView = transform.parent.GetComponent<MainMenuView>();
    }
}
