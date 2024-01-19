using RVP;
using System;
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
	public GameObject world;
	public GameObject menu;
	AudioSource menuMusic;

	float timer = 0;
	float duration;
	GameObject viewA;
	GameObject viewB;
	private void Awake()
	{
		menuMusic = GetComponent<AudioSource>();
		duration = dimCurve.keys[dimCurve.length - 1].time;
	}
	public void SwitchBackgroundTo(in Sprite sprite) => background.SwitchBackgroundTo(sprite);
	IEnumerator Play(Action method = null)
	{
		timer = 0;
		
		while (timer < duration)
		{
			if (viewA.activeSelf && timer >= 0.5f * duration)
			{
				//Debug.Log("switch");
				viewA.SetActive(false);
				viewB.SetActive(true);
				method?.Invoke();
			}
			timer += Time.deltaTime;
			SetBlacknessColor(dimCurve.Evaluate(timer));
			blackness.gameObject.SetActive(true);
			yield return null;
		}
		blackness.gameObject.SetActive(false);
	}
	void SetBlacknessColor(float a)
	{
		var c = blackness.color;
		c.a = a;
		blackness.color = c;
	}
	public void PlayDimmer(GameObject viewA, GameObject viewB)
	{
		this.viewA = viewA;
		this.viewB = viewB;
		var Bcomp = viewB.GetComponent<MainMenuView>();
		var Acomp = viewA.GetComponent<MainMenuView>();
		if (!Bcomp.prevView)
			Bcomp.prevView = viewA;
		// switch music if
		if (Bcomp.music && (!Acomp.music || Acomp.music != Bcomp.music))
		{
			menuMusic.clip = Bcomp.music;
			menuMusic.Play();
		}
		
		StartCoroutine(Play());
	}
	/// <summary>
	/// switch between world <---> menu
	/// </summary>
	public void PlayDimmerWorldMenu(GameObject viewA, GameObject viewB)
	{
		this.viewA = viewA;
		this.viewB = viewB;
		// switch music if
		StartCoroutine(Play());
	}
	/// <summary>
	/// Dims to targetVisibility. 0 = menu fully visible, 1 = blackness
	/// </summary>
	public void PlayDimmerToWorld()
	{ 
		menuMusic.Stop();
		this.viewA = menu;
		this.viewB = world;
		StartCoroutine(Play());
	}
	public void PlayDimmerToMenu()
	{
		menuMusic.Stop();
		this.viewA = world;
		this.viewB = menu;
			
		StartCoroutine(Play(() => { 
			world.GetComponent<RaceManager>().BackToEditor();
			world.GetComponent<RaceManager>().editorPanel.RemoveTrackLeftovers();
		}));
	}
}
