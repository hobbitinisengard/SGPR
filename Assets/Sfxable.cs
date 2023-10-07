using UnityEngine;
using UnityEngine.SceneManagement;

public class Sfxable : MonoBehaviour
{
	static GameObject mainCameraObj;
	static string curSceneName;
	private void Awake()
	{
		if(!mainCameraObj || curSceneName != SceneManager.GetActiveScene().name)
		{
			mainCameraObj = GameObject.Find("MainCamera");
		}
	}
	protected AudioSource PlaySFX(string name, bool ignorePause = false)
	{
		var snd = Resources.Load<GameObject>("sfx/SoundInstance");
		var go = Instantiate(snd, mainCameraObj.transform);
		var audioSource = go.GetComponent<AudioSource>();
		audioSource.clip = Info.audioClips[name];
		if (ignorePause)
		{
			audioSource.ignoreListenerPause = true;
			//a.GetComponent<AudioSource>().ignoreListenerVolume = true;

		}
		audioSource.Play();
		go.name = name;
		Destroy(go, audioSource.clip.length);
		return audioSource;
	}
}