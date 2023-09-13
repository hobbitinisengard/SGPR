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
	protected void PlaySFX(string name, bool ignorePause = false)
	{
		var snd = Resources.Load<GameObject>("sfx/SoundInstance");
		var a = Instantiate(snd, mainCameraObj.transform);
		a.GetComponent<AudioSource>().clip = Info.audioClips[name];
		if (ignorePause)
		{
			a.GetComponent<AudioSource>().ignoreListenerPause = true;
			//a.GetComponent<AudioSource>().ignoreListenerVolume = true;

		}
		a.GetComponent<AudioSource>().Play();
		a.name = name;
		Destroy(a, 2);
	}
}