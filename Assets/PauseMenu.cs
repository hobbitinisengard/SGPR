using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public RaceManager rm;
    public Button resumeButton;
    public Image veil;
    Color startColor;
    public Color blackColor;
    public float duration = 3;
    float timeElapsed;
    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            gameObject.SetActive(false);
        }
        if(timeElapsed < duration)
        {
            // fade background
			veil.color = Color.Lerp(startColor, blackColor, timeElapsed / duration);
			timeElapsed += Time.unscaledDeltaTime;
		}
    }
    private void OnEnable()
    {
		Time.timeScale = 0;
		startColor = veil.color;
        resumeButton.Select();   
    }
    private void OnDisable()
    {
        veil.color = startColor;
        Time.timeScale = 1;
        timeElapsed = 0;
    }
}