using TMPro;
using UnityEngine;

public class GraphicsSettingsView : MonoBehaviour
{
	public TMP_Text vSyncText;
	public TMP_Text trailText;
	public TMP_InputField limiterInput;
	private void OnEnable()
	{
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
		trailText.text = "Aerodynamic trail: " + (F.I.playerData.trail ? "Yes" : "No");
	}
	public void SwitchVSync(bool init)
	{
		if (!init)
		{
			F.I.playerData.vSync = !F.I.playerData.vSync;
			QualitySettings.vSyncCount = F.I.playerData.vSync ? 1 : 0;
			
		}
		vSyncText.text = "VSync: " + (F.I.playerData.vSync ? "Yes" : "No");
	}
}
