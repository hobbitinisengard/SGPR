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
	AudioSource clickSoundEffect;
	public AudioMixer audioMixer;
	public string exposedParameter;
	public float soundLevel = 0.5f;
	private void Start()
	{
		clickSoundEffect = GetComponent<AudioSource>();
		clickSoundEffect.ignoreListenerPause = true;
		text = transform.GetChild(0).GetComponent<Text>();
		if (audioMixer)
		{
			// add battery sliders
			batteryMask = transform.GetChild(1).GetChild(0);
			soundLevel = Info.ReadMixerLevelLog(exposedParameter, audioMixer);
			SetBatteryGUI(soundLevel);
		}
	}
	private void OnEnable()
	{
		selected = gameObject == EventSystem.current.currentSelectedGameObject;
	}
	private void Update()
	{
		if (selected)
		{
			if (Time.unscaledTime - timeStart >= 1)
			{
				timeStart = Time.unscaledTime;
			}
			text.color = Color32.Lerp(blackColor, redColor, 0.5f + Mathf.Sin((Time.unscaledTime - timeStart) * 2 * Mathf.PI) / 2f);

			if (batteryMask && audioMixer)
			{
				if (Input.GetButtonDown("Submit"))
				{
					//soundLevel = (soundLevel + 0.1f) % 1.1f;
					soundLevel += 0.1f;
					if (soundLevel > 1.05f)
						soundLevel = 0;
					Info.SetMixerLevelLog(exposedParameter, soundLevel, audioMixer);
					clickSoundEffect.Play();
					SetBatteryGUI(soundLevel);
				}
			}
		}
	}
	void SetBatteryGUI(float val01)
	{
		Vector3 pos = batteryMask.GetComponent<RectTransform>().anchoredPosition;
		pos.x = Mathf.Lerp(0, 70, val01);
		batteryMask.GetComponent<RectTransform>().anchoredPosition = pos;
	}
	public void OnSelect(BaseEventData eventData)
	{
		selected = true;
	}
	public void OnDeselect(BaseEventData eventData)
	{
		clickSoundEffect.Play();
		selected = false;
		text.color = new Color32(255, 255, 255, 255);
	}
}
