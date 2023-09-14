using UnityEngine;
using UnityEngine.UI;
using RVP;
using System;

public class MainMenuView : Sfxable
{
	[NonSerialized]
	public GameObject prevView;
	public Image dyndak;
	public Button firstButtonToBeSelected;
	public Text bottomText;
	public Sprite bgTile;
	public AudioClip music;
	ViewSwitcher dimmer;

	private void Start()
	{
		if (!Info.loaded)
		{
			Info.loaded = true;
			PlaySFX("fe-cardssuccess");
		}
	}
	private void OnEnable()
	{
		if(firstButtonToBeSelected)
			firstButtonToBeSelected.Select();
		dimmer = transform.FindParentComponent<ViewSwitcher>();
		dimmer.SwitchBackgroundTo(bgTile);

	}
	void Update()
	{
		if (prevView && Input.GetKeyDown(KeyCode.Escape))
		{
			GoToView(prevView);
			PlaySFX("fe-dialogcancel");
		}
	}
	public void GoToView(GameObject view)
	{
		for(int i=0; i< transform.childCount; ++i)
		{
			F.PlaySlideOutOnChildren(transform.GetChild(i));
		}
		dimmer.PlayDimmer(gameObject, view);
	}
	
	public void ToRaceScene()
	{
		PlaySFX("fe-gameload");
	}
	public void ToEditorScene()
	{
		
	}
}
