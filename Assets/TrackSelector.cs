using UnityEngine.UI;
using TMPro;

public class TrackSelector : TrackSelectorTemplate
{
	public TextMeshProUGUI raceTypeButtonText;
	public TextMeshProUGUI lapsButtonText;
	public TextMeshProUGUI nightButtonText;
	public TextMeshProUGUI rivalsButtonText;
	public TextMeshProUGUI catchupButtonText;
	public TextMeshProUGUI sponsorButtonText;
	public TextMeshProUGUI LevelButtonText;
	public TextMeshProUGUI wayButtonText;

	protected int maxCPURivals = 9;
	protected override void OnEnable()
	{
		base.OnEnable();
		ResetButtons();
	}
	public new void ResetButtons()
	{
		SwitchRoadType(true);
		SwitchCatchup(true);
		SwitchCPULevel(true);
		SwitchDayNight(true);
		SwitchLaps(true);
		SwitchRaceType(true);
		SwitchRivals(true);
		SwitchSponsor(true);
		base.ResetButtons();
	}
	public void SwitchRoadType(bool init = false)
	{
		int dir = 0;
		if (init)
		{
			if (F.I.randomPavement)
				F.I.s_roadType = PavementType.Random;
		}
		else
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		F.I.s_roadType = (PavementType)F.Wraparound((int)(F.I.s_roadType + dir), 0, F.I.pavementTypes + 1);
		F.I.randomPavement = F.I.s_roadType == PavementType.Random;
		wayButtonText.text = F.I.LocStr("Tex") + ": " + F.I.LocStr(F.I.s_roadType.ToString());
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
		
		raceTypeButtonText.text = F.I.LocStr(F.I.s_raceType.ToString());
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
		lapsButtonText.text = F.I.LocStr("Laps") + ": " + F.I.s_laps.ToString();
	}
	public void SwitchDayNight(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		F.I.s_timeOfDay = (TimeOfDay)F.Wraparound((int)F.I.s_timeOfDay + dir, (int)TimeOfDay.Day, (int)TimeOfDay.Sunset);
		nightButtonText.text = F.I.LocStr(F.I.s_timeOfDay.ToString());
	}
	public void SwitchCPULevel(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		F.I.s_cpuLevel = (CpuLevel)F.Wraparound((int)F.I.s_cpuLevel+dir, 0, 2);
		LevelButtonText.text = F.I.LocStr("CPU") + ": " + F.I.LocStr(F.I.s_cpuLevel.ToString());
	}
	public void SwitchRivals(bool init = false)
	{
		int dir = 0;
		if (!init)
			dir = F.I.shiftInputRef.action.ReadValue<float>() > 0.5f ? -1 : 1;

		// disable CPU in multiplayer races to save bandwidth
		F.I.s_cpuRivals = F.Wraparound(F.I.s_cpuRivals + dir, 0, (F.I.gameMode == GameMode.Multiplayer) ? 0 : maxCPURivals);

		if (F.I.s_raceType == RaceType.Knockout && ServerC.I.AmHost)
		{
			if (F.I.s_cpuRivals == 0 && F.I.maxCarsInRace - 1 == maxCPURivals)
				F.I.s_cpuRivals = 1;

			F.I.s_laps = F.I.maxCarsInRace - 1 - maxCPURivals + F.I.s_cpuRivals;
			SwitchLaps(true);
		}
		rivalsButtonText.text = F.I.LocStr("Rivals") + ": " + F.I.s_cpuRivals.ToString();
	}
	public void SwitchCatchup(bool init = false)
	{
		if (!init)
			F.I.catchup = !F.I.catchup;
		catchupButtonText.text = F.I.LocStr("Catchup") + ": " + (F.I.catchup ? F.I.LocStr("Yes") : F.I.LocStr("No"));
	}
	public void SwitchSponsor(bool init = false)
	{
		if (!init)
		{
			int dir = F.I.shiftRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
			do
			{
				F.I.s_PlayerCarSponsor = (Livery)F.Wraparound((int)F.I.s_PlayerCarSponsor + dir,
					ServerC.I.AmHost ? 0 : 1, F.I.Liveries);
			} while(F.I.unlockedLiveries[(int)F.I.s_PlayerCarSponsor] == false);
		}
		F.I.teams = F.I.s_PlayerCarSponsor != Livery.Random;
		sponsorButtonText.text = F.I.LocStr("Sponsor") + ": " + F.I.LocStr(F.I.s_PlayerCarSponsor.ToString());
	}
}
