using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public RaceManager rm;
    public Button resumeButton;
    public Image veil;
    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            gameObject.SetActive(false);
        }
    }
    private void OnEnable()
    {
        veil.CrossFadeAlpha(0.99f, 3, true);
        Time.timeScale = 0;
        resumeButton.Select();   
    }
    private void OnDisable()
    {
        var clr = veil.color;
        clr.a = 0;
        veil.color = clr;
        Time.timeScale = 1;
    }
}