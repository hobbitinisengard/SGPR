using TMPro;
using UnityEngine.UI;

public class SkateSelector : TrackSelectorTemplate
{
	public Button switchTimeLimitBtn;
	protected override void OnEnable()
	{
		F.I.gameMode = GameMode.Skate;
		F.I.s_raceType = RaceType.Stunt;
		F.I.s_cpuRivals = 0;
		F.I.s_spectator = false;
		SwitchSkateTimeLimit(true);
		base.OnEnable();
	}
	public void SwitchSkateTimeLimit(bool init = false)
	{
		int dir = 0;
		if (init)
		{
			switchTimeLimitBtn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = F.I.LocStr("Time Limit: ") + F.I.s_SkateTimeLimit + "s";
		}
		else
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
		F.I.s_SkateTimeLimit = F.Wraparound(F.I.s_SkateTimeLimit + dir * 60, 30, 300);
		switchTimeLimitBtn.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = F.I.LocStr("Time Limit: ") + F.I.s_SkateTimeLimit + "s";
	}
}
