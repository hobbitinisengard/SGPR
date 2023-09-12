using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class ViewSwitcher : MonoBehaviour
{
	public Image blackness;
	public AnimationCurve dimCurve;
	public BackgroundTiles background;
	public AudioMixerGroup audioMusic;
	public AudioMixerGroup audioSFX;
	AudioSource backgroundMusic;

	float timer = 0;
	float duration;
	GameObject viewA;
	GameObject viewB;
	private void Awake()
	{
		backgroundMusic = GetComponent<AudioSource>();
		duration = dimCurve.keys[dimCurve.length - 1].time;
	}
	public void SwitchBackgroundTo(in Sprite sprite) => background.SwitchBackgroundTo(sprite);
	IEnumerator Play()
	{
		while(true)
		{
			if (timer >= duration)
			{
				blackness.gameObject.SetActive(false);
				yield break;
			}
			else if (viewA.activeSelf && timer >= 0.5f * duration)
			{
				//Debug.Log("switch");
				viewA.SetActive(false);
				viewB.SetActive(true);
			}
			var c = blackness.color;
			c.a = dimCurve.Evaluate(timer);
			blackness.color = c;
			timer += Time.deltaTime;
			yield return null;
		}
	}
	public void PlayDimmer(GameObject viewA, GameObject viewB)
	{
		this.viewA = viewA;
		this.viewB = viewB;
		var Bcomp = viewB.GetComponent<MainMenuView>();
		var Acomp = viewA.GetComponent<MainMenuView>();
		if (!Bcomp.prevView)
			Bcomp.prevView = viewA;
		blackness.gameObject.SetActive(true);
		// switch music if
		if (Bcomp.music && (!Acomp.music || Acomp.music != Bcomp.music))
		{
			backgroundMusic.clip = Bcomp.music;
			backgroundMusic.Play();
		}
		timer = 0;
		StartCoroutine(Play());
	}
}
