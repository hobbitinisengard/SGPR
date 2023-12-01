using RVP;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using static TrackHeader;
using TMPro;
using Newtonsoft.Json.Linq;
using System.IO;

public class TrackSelector : Sfxable
{
	private enum SortingCond { Difficulty, Name };
	public TextMeshProUGUI trackDescText;
	public RectTransform trackContent;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	public RadialOneVisible radial;
	public GameObject trackImageTemplate;
	public MainMenuButton startButton;
	public MainMenuButton sortButton;
	public TextMeshProUGUI raceTypeButtonText;
	public TextMeshProUGUI lapsButtonText;
	public TextMeshProUGUI nightButtonText;
	public TextMeshProUGUI CPULevelButtonText;
	public TextMeshProUGUI rivalsButtonText;
	public TextMeshProUGUI wayButtonText;
	public TextMeshProUGUI catchupButtonText;
	public Transform tilesContainer;
	public Transform recordsContainer;
	Transform selectedTrack;
	string persistentSelectedTrack;
	SortingCond curSortingCondition = SortingCond.Difficulty;
	Coroutine containerCo;
	bool loadCo;

	public void SortTrackList()
	{
		curSortingCondition = (SortingCond)(((int)curSortingCondition + 1) % 2);
		if(curSortingCondition == SortingCond.Name)
			sortButton.transform.GetChild(0).GetComponent<Text>().text = "Sorted by track's name";
		else
			sortButton.transform.GetChild(0).GetComponent<Text>().text = "Sorted by track's difficulty";
		// reload 
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load());
	}
	public void SwitchRaceType(bool init=false)
	{
		var next = init ? (int)Info.s_raceType : (int)Info.s_raceType+1;
		next %= Info.RaceTypes;
		Info.s_raceType = (Info.RaceType)next;
		raceTypeButtonText.text = Enum.GetName(typeof(Info.RaceType), Info.s_raceType);
	}
	public void SwitchLaps(bool init = false)
	{
		if(!init)
		{
			if (Input.GetKey(KeyCode.LeftShift))
				Info.s_laps -= 3;
			else
				Info.s_laps += 3;
			if (Input.GetKey(KeyCode.LeftAlt))
				Info.s_laps -= 1;
		}
		Info.s_laps = Mathf.Clamp(Info.s_laps, 1, 30);
		lapsButtonText.text = "Laps: " + Info.s_laps.ToString();
	}
	public void SwitchDayNight(bool init = false)
	{
		if(!init)
			Info.s_isNight = !Info.s_isNight;
		nightButtonText.text = Info.s_isNight ? "Night" : "Daytime";
	}
	public void SwitchCPULevel(bool init = false)
	{
		if(!init)
		{
			if (Input.GetKey(KeyCode.LeftShift))
				Info.s_cpuLevel -= 25;
			else
				Info.s_cpuLevel += 25;
		}
		
		Info.s_cpuLevel = Mathf.Abs(Info.s_cpuLevel % 125);
		string cpuLevelStr;
		if(Info.s_cpuLevel == 0)
			cpuLevelStr = "Beginner";
		else if (Info.s_cpuLevel <= 25)
			cpuLevelStr = "Easy";
		else if (Info.s_cpuLevel <= 50)
			cpuLevelStr = "Medium";
		else if (Info.s_cpuLevel <= 75)
			cpuLevelStr = "Hard";
		else
			cpuLevelStr = "Elite";
		CPULevelButtonText.text = "CPU: " + cpuLevelStr;
	}
	public void SwitchRivals(bool init = false)
	{
		if(!init)
		{
			if (Input.GetKey(KeyCode.LeftShift))
				Info.s_rivals -= 1;
			else
				Info.s_rivals += 1;
		}
		Info.s_rivals %= 10;
		Info.s_rivals = Mathf.Clamp(Info.s_rivals, 0, 9);
		rivalsButtonText.text = "Opponents: " + Info.s_rivals.ToString();
	}
	public void SwitchRoadType(bool init = false)
	{
		if (!init)
		{
			int dir = Input.GetKey(KeyCode.LeftShift) ? -1 : 1;
			Info.s_roadType = (Info.PavementType)Mathf.Clamp((int)(Info.s_roadType + dir) % (Info.pavementTypes + 1), 0, Info.pavementTypes + 1);
		}
		wayButtonText.text = "Way: " + Enum.GetName(typeof(Info.PavementType), Info.s_roadType);
	}
	public void SwitchCatchup(bool init = false)
	{
		if(!init)
			Info.s_catchup = !Info.s_catchup;
		catchupButtonText.text = "Catchup: " + (Info.s_catchup ? "Yes" : "No");
	}
	private void OnDisable()
	{ // in unity, 
		persistentSelectedTrack = selectedTrack.name;
		//Debug.Log("Disable "+persistentSelectedTrack);
	}
	private void OnEnable()
	{
		Info.s_spectator = false;
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load());
	}
	bool[] PopulateContent()
	{
		bool[] existingTrackClasses = new bool[2];

		for (int i = 0; i < trackContent.childCount; ++i)
		{ // remove tracks from previous entry
			Transform trackClass = trackContent.GetChild(i);
			for (int j = 0; j < trackClass.childCount; ++j)
			{
				//Debug.Log(trackClass.GetChild(j).name);
				Destroy(trackClass.GetChild(j).gameObject);
			}
		}
		string[] sortedTracks;
		// populate track grid	
		if (curSortingCondition == SortingCond.Name)
			sortedTracks = Info.tracks.OrderBy(t => t.Key).Select(kv => kv.Key).ToArray();
		else //if(curSortingCondition == SortingCond.Difficulty)
			sortedTracks = Info.tracks.OrderBy(t => t.Value.difficulty).Select(kv => kv.Key).ToArray();
		foreach (var trackName in sortedTracks)
		{
			TrackHeader track = Info.tracks[trackName];
			if (track.unlocked && track.valid)
			{
				int trackOrigin = track.TrackOrigin();
				var newtrack = Instantiate(trackImageTemplate, trackContent.GetChild(trackOrigin));
				newtrack.name = trackName;
				newtrack.GetComponent<Image>().sprite = IMG2Sprite.LoadNewSprite(Path.Combine(Info.documents_sgpr_path, trackName+".png"));
				newtrack.SetActive(true);
				existingTrackClasses[trackOrigin] = true;
				if (persistentSelectedTrack != null && persistentSelectedTrack == trackName)
				{
					selectedTrack = newtrack.transform;
					Info.s_trackName = selectedTrack.name;
				}
			}
		}
		return existingTrackClasses;
	}
	IEnumerator Load()
	{
		loadCo = true;
		bool[] existingTrackClasses = PopulateContent();
		//Debug.Log(menuButtons[0] + " " + menuButtons[1] + " " + menuButtons[2] + " " + menuButtons[3]);

		yield return null; // wait for one frame for active objects to refresh

		for (int i = 0; i < existingTrackClasses.Length; ++i)
		{
			if (selectedTrack == null && existingTrackClasses[i])
			{
				selectedTrack = trackContent.GetChild(i).GetChild(0);
				Info.s_trackName = selectedTrack.name;
			}
			// disable track classes without children (required for sliders to work)
			trackContent.GetChild(i).gameObject.SetActive(existingTrackClasses[i]);
		}
		SetTiles();
		SetRecords();
		radial.gameObject.SetActive(selectedTrack);
		startButton.Select();

		if (selectedTrack == null)
		{
			trackDescText.text = "No tracks available";
			Info.s_trackName = null;
		}
		else
		{
			trackDescText.text = selectedTrack.name + "\n\n" + Info.tracks[selectedTrack.name].desc;
			
			radial.SetAnimTo(selectedTrack.parent.GetSiblingIndex());
		}
		
		containerCo = StartCoroutine(MoveToTrack());
		radial.SetChildrenActive(existingTrackClasses);
			
		Debug.Log(selectedTrack);
		loadCo = false;

		SwitchCatchup(true);
		SwitchCPULevel(true);
		SwitchDayNight(true);
		SwitchLaps(true);
		SwitchRaceType(true);
		SwitchRivals(true);
		SwitchRoadType(true);
	}
	void SetRecords()
	{
		for (int i = 0; i < recordsContainer.childCount; ++i)
		{
			Transform record = recordsContainer.GetChild(i);

			Record recordData = selectedTrack ? Info.tracks[selectedTrack.name].records[i] : new Record(null,0,0);

			string valueStr = (recordData == null || recordData.playerName == null) ? "" : recordData.playerName;
			record.GetChild(1).GetComponent<Text>().text = valueStr;
			if (recordData == null || recordData.secondsOrPts == 0 || recordData.secondsOrPts == 35999)
				valueStr = "";
			else
			{
				if (i <= 1) // lap, race, stunt, grip
					valueStr = Info.ToLaptimeStr(TimeSpan.FromSeconds(recordData.secondsOrPts));
				else
					valueStr = Mathf.RoundToInt(recordData.secondsOrPts).ToString();
			}
			record.GetChild(2).GetComponent<Text>().text = valueStr;

			if (recordData != null && (i > 0 && recordData.secondsOrPts > recordData.requiredSecondsOrPts)
				|| (i == 0 && recordData.secondsOrPts < recordData.requiredSecondsOrPts))
				record.GetChild(2).GetComponent<Text>().color = Color.yellow;
			else
				record.GetChild(2).GetComponent<Text>().color = Color.white;
		}
	}
	void SetTiles()
	{
		void AddTile(string spriteName)
		{
			var tile = Instantiate(tilesContainer.GetChild(0).gameObject, tilesContainer);
			tile.SetActive(true);
			tile.name = spriteName;
			try
			{
				tile.GetComponent<Image>().sprite = Info.icons.First(i => i.name == spriteName);
			}
			catch
			{
				Debug.LogError(spriteName);
			}
		}

		for (int i = 1; i < tilesContainer.childCount; ++i)
			Destroy(tilesContainer.GetChild(i).gameObject);
		if(selectedTrack)
		{
			AddTile(Enum.GetName(typeof(Info.CarGroup), Info.tracks[selectedTrack.name].preferredCarClass));
			AddTile(Enum.GetName(typeof(Info.Envir), Info.tracks[selectedTrack.name].envir));
			AddTile((Info.tracks[selectedTrack.name].difficulty + 4).ToString());
			foreach (var flag in Info.tracks[selectedTrack.name].icons)
				AddTile(Info.IconNames[flag]);
		}
	}
	
	void Update()
	{
		if (!selectedTrack || loadCo)
			return;
		int mult = Input.GetKey(KeyCode.LeftShift) ? 5 : 1;
		int x = Input.GetKeyDown(KeyCode.D) ? mult : Input.GetKeyDown(KeyCode.A) ? -mult : 0;
		int y = Input.GetKeyDown(KeyCode.W) ? -mult : Input.GetKeyDown(KeyCode.S) ? mult : 0;
		bool gotoHome = Input.GetKeyDown(KeyCode.Home);
		bool gotoEnd = Input.GetKeyDown(KeyCode.End);

		if (x != 0 || y != 0 || gotoHome || gotoEnd)
		{
			int posx = gotoHome ? 0 : gotoEnd ? selectedTrack.parent.childCount-1 : x + selectedTrack.GetSiblingIndex();
			if (posx < 0)
				posx = 0;
			int posy = y + selectedTrack.parent.GetSiblingIndex();
			if (posy >= 0 && posy <= 3 && posx>=0)
			{
				Transform tempSelectedTrack = null;
				for (int i = posy; i < trackContent.childCount && i >= 0;)
				{
					Transform selectedClass = trackContent.GetChild(i);
					if (selectedClass.childCount > 0)
					{
						if (posx >= selectedClass.childCount)
							posx = selectedClass.childCount - 1;
						tempSelectedTrack = selectedClass.GetChild(posx);
						//Debug.Log(tempSelectedTrack);
						break;
					}
					i = (y > 0) ? (i + 1) : (i - 1);
				}
				if(tempSelectedTrack != null && tempSelectedTrack != selectedTrack)
				{
					selectedTrack = tempSelectedTrack;
					Info.s_trackName = selectedTrack.name;
					PlaySFX("fe-bitmapscroll");
				}
				// new track has been selected
				// set description
				trackDescText.text = selectedTrack.name + "\n\n" + Info.tracks[selectedTrack.name].desc;
				radial.SetAnimTo(selectedTrack.parent.GetSiblingIndex());
				SetTiles();
				SetRecords();
				// focus on track
				if (containerCo != null)
					StopCoroutine(containerCo);
				containerCo = StartCoroutine(MoveToTrack());
			}
		}
	}
	IEnumerator MoveToTrack()
	{
		yield return null;
		if (!selectedTrack)
			yield break;
		float timer = 0;
		Vector2 initPos = trackContent.anchoredPosition;
		Vector2 targetPos = new Vector2(-((RectTransform)selectedTrack).anchoredPosition.x,
			-selectedTrack.parent.GetComponent<RectTransform>().anchoredPosition.y);
		Vector2 scrollInitPos = new Vector2(scrollx.value, scrolly.value);
		Vector2 scrollInitSize = new Vector2(scrollx.size, scrolly.size);
		float trackInGroupPos = Info.InGroupPos(selectedTrack);
		float groupPos = trackContent.PosAmongstActive(selectedTrack.parent, false);
		Vector2 scrollTargetPos = new Vector2(trackInGroupPos, groupPos);
		Vector2 scrollTargetSize = new Vector2(1f / selectedTrack.parent.ActiveChildren(), 1f / trackContent.ActiveChildren());

		while (timer < 1)
		{
			float step = F.EasingOutQuint(timer);
			trackContent.anchoredPosition = Vector2.Lerp(initPos, targetPos, step);
			scrollx.value = Mathf.Lerp(scrollInitPos.x, scrollTargetPos.x, step);
			scrolly.value = Mathf.Lerp(scrollInitPos.y, scrollTargetPos.y, step);
			scrollx.size = Mathf.Lerp(scrollInitSize.x, scrollTargetSize.x, step);
			scrolly.size = Mathf.Lerp(scrollInitSize.y, scrollTargetSize.y, step);
			timer += Time.deltaTime;

			yield return null;
		}
	}
}
