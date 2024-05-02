using RVP;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class GreetingView : MonoBehaviour
{
	public Text versionText;
	public EventSystem eventSystem;
	public AudioMixer masterMixer;
	public RaceManager raceManager;
	private void Start()
	{
		RaceManager.I = raceManager;
		F.I.eventSystem = eventSystem;
		F.I.ReadSettingsDataFromJson();
		F.I.SetMixerLevelLog("sfxVol", F.I.playerData.sfxVol, masterMixer);
		F.I.SetMixerLevelLog("musicVol", F.I.playerData.musicVol, masterMixer);
	}
}
