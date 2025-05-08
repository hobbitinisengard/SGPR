using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class GraphicsSettingsView : MonoBehaviour
{
	public TMP_Text vSyncText;
	public TMP_Text trailText;
	public TMP_Text langText;
	public TMP_InputField limiterInput;
	private void OnEnable()
	{
		SwitchLang(true);
		SwitchTrail(true);
		SwitchVSync(true);
		limiterInput.text = F.I.playerData.fpsLimit.ToString();
	}
	private void OnDisable()
	{
		F.I.SaveSettingsDataToJson();
	}
	public void UpdateFPSLimiter(string newLimit)
	{
		Application.targetFrameRate = Mathf.Clamp(int.Parse(newLimit), 60, 500);
		F.I.playerData.fpsLimit = Application.targetFrameRate;
		limiterInput.text = F.I.playerData.fpsLimit.ToString();
	}
	public void SwitchTrail(bool init)
	{
		if(!init)
			F.I.playerData.trail = !F.I.playerData.trail;
		trailText.text = F.I.LocStr("Aerodynamic trail") + ": " + (F.I.playerData.trail ? F.I.LocStr("Yes") : F.I.LocStr("No"));
	}
	public void SwitchLang(bool init)
	{
		if (!init)
		{
			int dir = F.I.shiftRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
			F.I.playerData.language = (Language)F.Wraparound((int)F.I.playerData.language + dir, 0, LocalizationSettings.AvailableLocales.Locales.Count-1);
			F.I.UpdateLanguage();
			OnEnable();
		}
		langText.text = F.I.LocStr("Language") + ": " + F.I.LocStr(F.I.playerData.language.ToString());
	}
	public void SwitchVSync(bool init)
	{
		if (!init)
		{
			F.I.playerData.vSync = !F.I.playerData.vSync;
			QualitySettings.vSyncCount = F.I.playerData.vSync ? 1 : 0;
		}
		vSyncText.text = F.I.LocStr("VSync") + ": " + (F.I.playerData.vSync ? F.I.LocStr("Yes") : F.I.LocStr("No"));
	}
}
