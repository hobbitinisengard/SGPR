using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class GreetingView : MonoBehaviour
{
	public Text versionText;
	public AudioMixer masterMixer;
	private static void CopyFilesRecursively(string sourcePath, string targetPath)
	{
		//Now Create all of the directories
		foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
		{
			Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
		}

		//Copy all the files & Replaces any files with the same name
		foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
		{
			File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
		}
	}
	private void Awake()
	{
		versionText.text = Info.version;

		if (!Directory.Exists(Info.documentsSGPRpath))
		{
			Directory.CreateDirectory(Info.documentsSGPRpath);
			CopyFilesRecursively(Application.streamingAssetsPath, Info.documentsSGPRpath);
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
		Info.steerGamma = settingsData.steerGamma;
		Debug.Log("Gamma: " + Info.steerGamma.ToString());
		Info.SetMixerLevelLog("sfxVol", settingsData.sfxVol, masterMixer);
		Info.SetMixerLevelLog("musicVol", settingsData.musicVol, masterMixer);
	}
}
