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
		if (!F.I.loaded)
		{
			F.I.loaded = true;
			PlaySFX("fe-cardssuccess");
		}
	}

	void CancelPressed(InputAction.CallbackContext obj)
	{
		GoBack();
	}
	public void GoBack(bool ignoreYouSure = false)
	{
		if (ignoreYouSure || youSureDialog == null)
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
		if (F.I.s_trackName == null || F.I.s_trackName.Length < 4)
			PlaySFX("fe-cardserror");
		else
		{
			PlaySFX("fe-gameload");
			if (F.I.s_roadType == PavementType.Random)
				F.I.s_roadType = (PavementType)Mathf.RoundToInt(F.I.pavementTypes * UnityEngine.Random.value);

			for (int i = 0; i < transform.childCount; ++i)
			{
				if (transform.GetChild(i).gameObject.activeSelf)
					F.PlaySlideOutOnChildren(transform.GetChild(i));
			}
			F.I.s_inEditor = false;
			dimmer.PlayDimmerToWorld();
		}
	}
	public void ToEditorScene()
	{
		if (F.I.s_trackName == "MEX")
			PlaySFX("fe-cardserror");
		else
		{
			
			if(F.I.s_roadType == PavementType.Random)
			{
				PavementType[] allowedTilesets = new[] { PavementType.Highway, PavementType.Asphalt, PavementType.Japanese, PavementType.GreenSand };
				F.I.s_roadType = allowedTilesets.GetRandom();
			}
			F.I.s_inEditor = true;
			F.I.s_spectator = false;
			dimmer.PlayDimmerToWorld();
		}
	}
	public void QuitGame()
	{
		Application.Quit();
	}
}
