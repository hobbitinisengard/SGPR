using UnityEngine;

public class Sfxable : MonoBehaviour
{
	protected void PlaySFX(string name)
	{
		var snd = Resources.Load<GameObject>("sfx/SoundInstance");
		var a = Instantiate(snd);
		a.GetComponent<AudioSource>().clip = Info.audioClips[name];
		a.GetComponent<AudioSource>().Play();
		a.name = name;
		Destroy(a, 2);
	}
}