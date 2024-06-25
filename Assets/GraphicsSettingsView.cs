using TMPro;
using UnityEngine;

public class GraphicsSettingsView : MonoBehaviour
{
	public TMP_Text vSyncText;
	public TMP_InputField limiterInput;
	private void OnEnable()
	{
		SwitchVSync(true);
		limiterInput.text = F.I.playerData.fpsLimit.ToString();
	}
	public void UpdateFPSLimiter(string newLimit)
	{
		Application.targetFrameRate = Mathf.Clamp(int.Parse(newLimit), 60, 500);
		F.I.playerData.fpsLimit = Application.targetFrameRate;
		limiterInput.text = F.I.playerData.fpsLimit.ToString();
		F.I.SaveSettingsDataToJson();
	}
	public void SwitchVSync(bool init)
	{
		if (!init)
		{
			F.I.playerData.vSync = !F.I.playerData.vSync;
			QualitySettings.vSyncCount = F.I.playerData.vSync ? 1 : 0;
			F.I.SaveSettingsDataToJson();
		}
		vSyncText.text = "VSync: " + (F.I.playerData.vSync ? "Yes" : "No");
	}
}
