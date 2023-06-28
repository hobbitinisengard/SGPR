using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class PauseMenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    static Color32 redColor = new Color32(255, 0, 0, 255);
    static Color32 blackColor = new Color32(0, 0, 0, 255);
    bool selected = false;
    Text text;
    float timeStart = 0;
    Transform batteryMask;
    public AudioMixer audioMixer;
    public float soundLevel = 0.5f;
    private void Start()
    {
        text = transform.GetChild(0).GetComponent<Text>();
        if(audioMixer)
        {
            // add battery sliders
            batteryMask = transform.GetChild(1).GetChild(0);
            float inVal;
            audioMixer.GetFloat("volume", out inVal);
            soundLevel = Mathf.Pow(10, inVal / 20f);
            soundLevel = Mathf.Floor((int)(soundLevel * 10))/10f; // steps by 0.1
        }
    }
    private void Update()
    {
        
        if(selected)
        {
            if (Time.unscaledTime - timeStart >= 1)
            {
                timeStart = Time.unscaledTime;
            }
            text.color = Color32.Lerp(blackColor, redColor, 0.5f + Mathf.Sin((Time.unscaledTime - timeStart) * 2 * Mathf.PI) / 2f);

            if(batteryMask && audioMixer)
            {
                if(Input.GetButtonDown("Submit"))
                {
                    soundLevel = soundLevel + 0.1f;
                    if (soundLevel > 1.05f)
                        soundLevel = 0;
                    audioMixer.SetFloat("volume", Mathf.Log10(soundLevel) * 20);
                    Vector3 pos = batteryMask.GetComponent<RectTransform>().anchoredPosition;
                    pos.x = Mathf.Lerp(0, 70, soundLevel);
                    batteryMask.GetComponent<RectTransform>().anchoredPosition = pos;
                }
            }
        }
    }
    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        text.color = new Color32(255, 255, 255, 255);
    }
    
}
