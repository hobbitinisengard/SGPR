using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.InputSystem;

public class MainMenuView : Sfxable
{
	[NonSerialized]
	public GameObject prevView;
	public Image dyndak;
	public Button firstButtonToBeSelected;
	public TextMeshProUGUI bottomText;
	public Sprite bgTile;
	public AudioClip music;
	public InputActionReference cancelInput;
	public YouSureDialog youSureDialog;
	public bool prevViewForbidden;
	static ViewSwitcher dimmer;
	protected override void Awake()
	{
		base.Awake();

		if (dimmer == null)
			dimmer = transform.FindParentComponent<ViewSwitcher>();
	}
	private void Start()
	{
		if (!Info.loaded)
		{
			Info.loaded = true;
			PlaySFX("fe-cardssuccess");
		}
	}

	void CancelPressed(InputAction.CallbackContext obj)
	{
		if(youSureDialog == null)
		{
			if (gameObject.activeSelf && prevView && !prevViewForbidden)
			{
				GoToView(prevView);
				PlaySFX("fe-dialogcancel");
			}
		}
		else
		{
			youSureDialog.gameObject.SetActive(true);
		}
	}
	protected void OnDisable()
	{
		cancelInput.action.started -= CancelPressed;
	}
	protected void OnEnable()
	{
		cancelInput.action.started += CancelPressed;
		if (firstButtonToBeSelected)
			firstButtonToBeSelected.Select();
		
		dimmer.SwitchBackgroundTo(bgTile);
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
		if (Info.s_trackName == null || Info.s_trackName.Length < 4)
			PlaySFX("fe-cardserror");
		else
		{
			PlaySFX("fe-gameload");
			if (Info.s_roadType == PavementType.Random)
				Info.s_roadType = (PavementType)Mathf.RoundToInt(Info.pavementTypes * UnityEngine.Random.value);

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
		if (Info.s_trackName == "MEX")
			PlaySFX("fe-cardserror");
		else
		{
			
			if(Info.s_roadType == PavementType.Random)
			{
				PavementType[] allowedTilesets = new[] { PavementType.Highway, PavementType.Asphalt, PavementType.Japanese, PavementType.GreenSand };
				Info.s_roadType = allowedTilesets.GetRandom();
			}
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
