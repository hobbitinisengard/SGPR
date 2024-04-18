using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class PauseMenuButton : MonoBehaviour, ISelectHandler, IDeselectHandler
{
	static Color32 redColor = new (255, 0, 0, 255);
	static Color32 blackColor = new (0, 0, 0, 255);
	bool selected = false;
	Text text;
	float timeStart = 0;
	Transform batteryMask;
	AudioSource clickSoundEffect;
	public AudioMixer audioMixer;
	public string exposedParameter;
	public float indicatorLevel = 0.5f;
	
	private void Awake()
	{
		GetComponent<Button>().onClick.AddListener(OnClickDo);
		clickSoundEffect = GetComponent<AudioSource>();
		clickSoundEffect.ignoreListenerPause = true;
		text = transform.GetChild(0).GetComponent<Text>();
		if (transform.childCount > 1)
		{
			batteryMask = transform.GetChild(1).GetChild(0);
			if (audioMixer)
				indicatorLevel = F.I.ReadMixerLevelLog(exposedParameter, audioMixer);
			else
				indicatorLevel = Mathf.InverseLerp(0,10,F.I.playerData.steerGamma);
			SetBatteryGUI(indicatorLevel);
		}
	}
	void OnClickDo()
	{
		if (batteryMask)
		{
			indicatorLevel += (F.I.shiftRef.action.ReadValue<float>() > 0 ? -.1f : .1f);
			indicatorLevel = F.Wraparound(indicatorLevel, 0, 1);

			clickSoundEffect.Play();
			SetBatteryGUI(indicatorLevel);

			if(audioMixer)
				F.I.SetMixerLevelLog(exposedParameter, indicatorLevel, audioMixer);
			else
			{
				F.I.playerData.steerGamma = Mathf.Clamp(10*indicatorLevel, 1, 10);
			}
		}
	}
	private void OnEnable()
	{
		selected = gameObject == EventSystem.current.currentSelectedGameObject;
	}
	private void OnDisable()
	{
		text.color = new Color32(255, 255, 255, 255);
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
		OnDisable();
	}
}
