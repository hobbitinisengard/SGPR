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
	public TextMeshProUGUI sponsorButtonText;

	protected int maxCPURivals = 9;
	protected override void OnEnable()
	{
		base.OnEnable();
		ResetButtons();
	}
	public void ResetButtons()
	{
		//SwitchCatchup(true);
		SwitchCPULevel(true);
		SwitchDayNight(true);
		SwitchLaps(true);
		SwitchRaceType(true);
		SwitchRivals(true);
		SwitchSponsor(true);
		SwitchRoadType(true);
	}
	public void SwitchRaceType(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		F.I.s_raceType = (RaceType)F.Wraparound((int)F.I.s_raceType+dir,0,F.I.RaceTypes-1);

		if (F.I.s_raceType == RaceType.Knockout)
		{
			lapsButtonText.transform.parent.GetComponent<Button>().interactable = false;
			SwitchRivals(true);
		}
		else
		{
			lapsButtonText.transform.parent.GetComponent<Button>().interactable = true;
			rivalsButtonText.transform.parent.GetComponent<Button>().interactable = true;
		}
		if (F.I.s_raceType == RaceType.Drift)
		{
			F.I.s_cpuRivals = 0;
			SwitchRivals(true);
			rivalsButtonText.transform.parent.GetComponent<Button>().interactable = false;
		}
		
		raceTypeButtonText.text = Enum.GetName(typeof(RaceType), F.I.s_raceType);
	}
	public void SwitchLaps(bool init = false)
	{
		if (!init)
		{
			if (F.I.shiftInputRef.action.ReadValue<float>() > 0.5f)
				F.I.s_laps -= 3;
			else
			{
				if (F.I.ctrlInputRef.action.ReadValue<float>() > 0.5f)
					F.I.s_laps -= 1;
				else if (F.I.altInputRef.action.ReadValue<float>() > 0.5f)
					F.I.s_laps += 1;
				else
					F.I.s_laps += 3;
			}
		}
		F.I.s_laps = F.Wraparound(F.I.s_laps, 1, 99);
		lapsButtonText.text = "Laps: " + F.I.s_laps.ToString();
	}
	public void SwitchDayNight(bool init = false)
	{
		if (!init)
			F.I.s_isNight = !F.I.s_isNight;
		nightButtonText.text = F.I.s_isNight ? "Night" : "Daytime";
	}
	public void SwitchCPULevel(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		F.I.s_cpuLevel = CpuLevel.Normal;//(CpuLevel)F.Wraparound((int)F.I.s_cpuLevel+dir, 0, 3);
		string cpuLevelStr = F.I.s_cpuLevel switch
		{
			_ => "Normal",
		};
		CPULevelButtonText.text = "CPU: " + cpuLevelStr;
	}
	public void SwitchRivals(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		// disable CPU in multiplayer races to save bandwidth
		F.I.s_cpuRivals = F.Wraparound(F.I.s_cpuRivals + dir, 0, (F.I.gameMode == MultiMode.Multiplayer) ? 0 : maxCPURivals);

		if (F.I.s_raceType == RaceType.Knockout && ServerC.I.AmHost)
		{
			if (F.I.s_cpuRivals == 0 && F.I.maxCarsInRace - 1 == maxCPURivals)
				F.I.s_cpuRivals = 1;

			F.I.s_laps = F.I.maxCarsInRace - 1 - maxCPURivals + F.I.s_cpuRivals;
			SwitchLaps(true);
		}
		rivalsButtonText.text = "Opponents: " + F.I.s_cpuRivals.ToString();
	}
	
	public void SwitchRoadType(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
		F.I.s_roadType = (PavementType)F.Wraparound((int)(F.I.s_roadType + dir), 0, F.I.pavementTypes+1);
		F.I.randomPavement = F.I.s_roadType == PavementType.Random;
		wayButtonText.text = "Tex: " + F.I.s_roadType.ToString();
	}
	public void SwitchCatchup(bool init = false)
	{
		if (!init)
			F.I.s_catchup = !F.I.s_catchup;
		sponsorButtonText.text = "Catchup: " + (F.I.s_catchup ? "Yes" : "No");
	}
	public void SwitchSponsor(bool init = false)
	{
		if (!init)
		{
			int dir = F.I.shiftRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
			F.I.s_PlayerCarSponsor = (Livery)F.Wraparound((int)F.I.s_PlayerCarSponsor + dir, 0, F.I.Liveries);
		}
		sponsorButtonText.text = "Sponsor:" + F.I.s_PlayerCarSponsor.ToString();
	}
}
