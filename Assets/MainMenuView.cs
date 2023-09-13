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
		//if (Input.GetKeyDown(KeyCode.Alpha1))
		//	Info.AddCar();
	}
	public void GoToView(GameObject view)
	{
		for(int i=0; i< transform.childCount; ++i)
		{
			Check(transform.GetChild(i));
		}
		dimmer.PlayDimmer(gameObject, view);
	}
	void Check(Transform node)
	{
		var comp = node.GetComponent<SlideInOut>();
		if (comp)
		{
			if(comp.gameObject.activeSelf)
				comp.PlaySlideOut();
		}
		else
		{
			for (int i = node.transform.childCount - 1; i >= 0; --i)
			{
				Check(node.transform.GetChild(i));
			}
		}
	}
	public void ToRaceScene()
	{
		PlaySFX("fe-gameload");
	}
}
