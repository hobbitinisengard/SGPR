using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class GreetingView : Sfxable
{
	public Text versionText;
	public AudioMixer masterMixer;
	private void Awake()
	{
		versionText.text = Info.version;

		if (!Directory.Exists(Info.documentsSGPRpath))
		{
			Directory.CreateDirectory(Info.documentsSGPRpath);
		}
		if (!Directory.Exists(Info.partsPath))
		{
			Directory.CreateDirectory(Info.partsPath);
		}
		Info.ReloadCarPartsData();
		Debug.Log("Loaded parts: " + Info.carParts.Count);
		Info.ReloadCarsData();
		Info.PopulateTrackData();
		Info.icons = Resources.LoadAll<Sprite>(Info.trackImagesPath + "tiles");
		Info.PopulateSFXData();
	}
	private void Start()
	{
		PlayerSettingsData settingsData = Info.ReadSettingsDataFromJson();
		Info.s_playerName = settingsData.lastPlayerName;
		Info.SetMixerLevelLog("sfxVol", settingsData.sfxVol, masterMixer);
		Info.SetMixerLevelLog("musicVol", settingsData.musicVol, masterMixer);
	}
	void Update()
	{
		if (Input.GetButtonDown("Submit"))
		{
			PlaySFX("fe-dialogconfirm");
		}
	}
}
