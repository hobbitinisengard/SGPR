using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BtnTest : MonoBehaviour, IDeselectHandler
{
	Button MyButton;
	AudioSource MyAudioSource;
	void Start()
	{
		MyButton = GetComponent<Button>();
		MyAudioSource = GetComponent<AudioSource>();
		Time.timeScale = 0;
		MyButton.onClick.AddListener(PlayMusic);
	}

	private void PlayMusic()
	{
		Debug.Log(Time.timeScale); // 0
		MyAudioSource.Play();      //I hear the music
		Debug.Log(Time.timeScale); // 0
	}

	public void OnDeselect(BaseEventData eventData)
	{
		PlayMusic();
	}
}
