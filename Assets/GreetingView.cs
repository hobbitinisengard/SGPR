using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class GreetingView : Sfxable
{
	public Text versionText;
	public AudioMixer sfxMixer;
	public AudioMixer musicMixer;
	private void Awake()
	{
		versionText.text = Info.version;
		
		// Read PlayerSettings (sfx, music)
		PlayerSettingsData settingsData;
		if (!File.Exists(Info.userdata_path))
		{
			settingsData = new PlayerSettingsData();
			string serializedSettings = JsonConvert.SerializeObject(settingsData);
			File.WriteAllText(Info.userdata_path, serializedSettings);
		}
		else
		{
			string playerSettings = File.ReadAllText(Info.userdata_path);
			settingsData = JsonConvert.DeserializeObject<PlayerSettingsData>(playerSettings);
		}
		sfxMixer.SetFloat("volume", 20 * Mathf.Log10(settingsData.sfxVol));
		musicMixer.SetFloat("volume", 20 * Mathf.Log10(settingsData.musicVol));
		Info.s_playerName = settingsData.lastPlayerName;
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			PlaySFX("fe-dialogconfirm");
		}
	}
}
