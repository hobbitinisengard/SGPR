using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class GreetingView : MonoBehaviour
{
	public Text versionText;
	public EventSystem eventSystem;
	public AudioMixer masterMixer;

	private void Start()
	{
		F.I.eventSystem = eventSystem;
		F.I.ReadSettingsDataFromJson();
		F.I.SetMixerLevelLog("sfxVol", F.I.playerData.sfxVol, masterMixer);
		F.I.SetMixerLevelLog("musicVol", F.I.playerData.musicVol, masterMixer);
	}
}
