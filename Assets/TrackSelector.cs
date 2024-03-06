using System;
using UnityEngine.UI;
using TMPro;

public class TrackSelector : TrackSelectorTemplate
{
	public TextMeshProUGUI raceTypeButtonText;
	public TextMeshProUGUI lapsButtonText;
	public TextMeshProUGUI nightButtonText;
	public TextMeshProUGUI CPULevelButtonText;
	public TextMeshProUGUI rivalsButtonText;
	public TextMeshProUGUI wayButtonText;
	public TextMeshProUGUI catchupButtonText;
	protected int maxRivals = 9;
	protected override void OnEnable()
	{
		base.OnEnable();
		ResetButtons();
	}
	public void ResetButtons()
	{
		SwitchCatchup(true);
		SwitchCPULevel(true);
		SwitchDayNight(true);
		SwitchLaps(true);
		SwitchRaceType(true);
		SwitchRivals(true);
		SwitchRoadType(true);
	}
	public void SwitchRaceType(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		Info.s_raceType = (RaceType)Wraparound((int)Info.s_raceType+dir,0,Info.RaceTypes-1);

		if (Info.s_raceType == RaceType.Knockout)
		{
			lapsButtonText.transform.parent.GetComponent<Button>().interactable = false;
			SwitchRivals(true);
		}
		else
		{
			lapsButtonText.transform.parent.GetComponent<Button>().interactable = true;
		}
		raceTypeButtonText.text = Enum.GetName(typeof(RaceType), Info.s_raceType);
	}
	public void SwitchLaps(bool init = false)
	{
		if (!init)
		{
			if (shiftInputRef.action.ReadValue<float>() > 0.5f)
				Info.s_laps -= 3;
			else
			{
				if (ctrlInputRef.action.ReadValue<float>() > 0.5f)
					Info.s_laps -= 1;
				else if (altInputRef.action.ReadValue<float>() > 0.5f)
					Info.s_laps += 1;
				else
					Info.s_laps += 3;
			}
		}
		Info.s_laps = Wraparound(Info.s_laps, 1, 99);
		lapsButtonText.text = "Laps: " + Info.s_laps.ToString();
	}
	public void SwitchDayNight(bool init = false)
	{
		if (!init)
			Info.s_isNight = !Info.s_isNight;
		nightButtonText.text = Info.s_isNight ? "Night" : "Daytime";
	}
	public void SwitchCPULevel(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		Info.s_cpuLevel = (CpuLevel)Wraparound((int)Info.s_cpuLevel+dir, 0, 3);
		string cpuLevelStr = Info.s_cpuLevel switch
		{
			CpuLevel.Easy => "Easy",
			CpuLevel.Medium => "Medium",
			CpuLevel.Hard => "Hard",
			CpuLevel.Elite => "Elite",
			_ => "Elite",
		};
		CPULevelButtonText.text = "CPU: " + cpuLevelStr;
	}
	public void SwitchRivals(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		Info.s_rivals = Wraparound(Info.s_rivals + dir, 0, maxRivals);

		if (Info.s_raceType == RaceType.Knockout)
		{
			if (Info.s_rivals == 0)
				Info.s_rivals = 1;
			Info.s_laps = Info.s_rivals;
			SwitchLaps(true);
		}
		rivalsButtonText.text = "Opponents: " + Info.s_rivals.ToString();
	}
	protected int Wraparound(int value, int min, int max)
	{
		if (value < min)
			value = max;
		else if (value > max)
			value = min;
		return value;
	}
	public void SwitchRoadType(bool init = false)
	{
		if (!init)
		{
			int dir = shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
			Info.s_roadType = (PavementType)Wraparound((int)(Info.s_roadType + dir), 0, Info.pavementTypes+1);
		}
		wayButtonText.text = "Tex: " + Enum.GetName(typeof(PavementType), Info.s_roadType);
	}
	public void SwitchCatchup(bool init = false)
	{
		if (!init)
			Info.s_catchup = !Info.s_catchup;
		catchupButtonText.text = "Catchup: " + (Info.s_catchup ? "Yes" : "No");
	}
	
}
