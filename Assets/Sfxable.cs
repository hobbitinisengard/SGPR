using UnityEngine;

public class Sfxable : MonoBehaviour
{
	static GameObject mainCameraObj;
	protected virtual void Awake()
	{
		if(!mainCameraObj)
			mainCameraObj = GameObject.Find("MainCamera");
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