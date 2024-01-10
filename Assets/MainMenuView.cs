using UnityEngine;
using UnityEngine.UI;
using RVP;
using System;
using TMPro;

public class MainMenuView : Sfxable
{
	[NonSerialized]
	public GameObject prevView;
	public Image dyndak;
	public Button firstButtonToBeSelected;
	public TextMeshProUGUI bottomText;
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
			if(transform.GetChild(i).gameObject.activeSelf)
				F.PlaySlideOutOnChildren(transform.GetChild(i));
		}
		dimmer.PlayDimmer(gameObject, view);
	}
	public void ToRaceScene()
	{
		if (Info.s_trackName == null)
			PlaySFX("fe-cardserror");
		else
		{
			PlaySFX("fe-gameload");
			if (Info.s_roadType == Info.PavementType.Random)
				Info.s_roadType = (Info.PavementType)Mathf.RoundToInt(Info.pavementTypes * UnityEngine.Random.value);

			for (int i = 0; i < transform.childCount; ++i)
			{
				if (transform.GetChild(i).gameObject.activeSelf)
					F.PlaySlideOutOnChildren(transform.GetChild(i));
			}
			Info.s_inEditor = false;
			dimmer.PlayDimmerToWorld();
		}
		
	}
	public void ToEditorScene()
	{
		if (Info.s_trackName == null)
			PlaySFX("fe-cardserror");
		else
		{
			Info.s_roadType = Info.PavementType.Highway;
			Info.s_inEditor = true;
			Info.s_spectator = false;
			dimmer.PlayDimmerToWorld();
		}
	}
	public void QuitGame()
	{
		Application.Quit();
	}
}
