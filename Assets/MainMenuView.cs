using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    public Image dyndak;
    public Button firstButtonToBeSelected;
    public Text bottomText;
    void Start()
    {
        firstButtonToBeSelected.Select();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GoToView(GameObject view)
    {

    }
}
