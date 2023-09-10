using RVP;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TrackSelector : MonoBehaviour
{
	private enum SortingCond { Difficulty, Name };
	public Text trackDescText;
	public Transform buttonsContainer;
	public RectTransform trackContent;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	public RadialOneVisible radial;
	public GameObject trackImageTemplate;
	public MainMenuButton startButton;
	public MainMenuButton sortButton;
	public Text raceTypeButtonText;
	public Text lapsButtonText;
	public Text nightButtonText;
	public Text CPULevelButtonText;
	public Text rivalsButtonText;
	public Text reversedButtonText;
	public Text catchupButtonText;
	public Transform tilesContainer;

	Transform selectedTrack;
	string persistentSelectedTrack;
	SortingCond curSortingCondition = SortingCond.Difficulty;
	Coroutine containerCo;
	bool loadCo;
	public void SortTrackList()
	{
		int sorting = (int)curSortingCondition + 1;
		sorting %= 2;
		curSortingCondition = (SortingCond)sorting;
		if(sorting == 1)
		{
			sortButton.transform.GetChild(0).GetComponent<Text>().text = "Sorted by track's name";
		}
		else
		{
			sortButton.transform.GetChild(0).GetComponent<Text>().text = "Sorted by track's difficulty";
		}

		//sort tracks
		// move to 
		containerCo = StartCoroutine(MoveToTrack());
	}
	public void SwitchRaceType()
	{
		var next = (int)Info.s_raceType+1;
		next %= Info.RaceTypes;
		Info.s_raceType = (Info.RaceType)next;
		raceTypeButtonText.text = Enum.GetName(typeof(Info.RaceType), Info.s_raceType);
	}
	public void SwitchLaps()
	{
		if (Input.GetKey(KeyCode.LeftShift))
			Info.s_laps -= 3;
		else
			Info.s_laps += 3;
		if (Input.GetKey(KeyCode.LeftAlt))
			Info.s_laps -= 1;

		Info.s_laps = Mathf.Clamp(Info.s_laps, 1, 30);
		lapsButtonText.text = "Laps: " + Info.s_laps.ToString();
	}
	public void SwitchDayNight()
	{
		Info.s_isNight = !Info.s_isNight;
		nightButtonText.text = Info.s_isNight ? "Nighttime" : "Daytime";
	}
	public void SwitchCPULevel()
	{
		if (Input.GetKey(KeyCode.LeftShift))
			Info.s_cpuLevel -= 20;
		else
			Info.s_cpuLevel += 20;
		Info.s_cpuLevel = Mathf.Abs(Info.s_cpuLevel % 120);
		string cpuLevelStr;
		if(Info.s_cpuLevel == 0)
			cpuLevelStr = "Beginner";
		else if (Info.s_cpuLevel <= 20)
			cpuLevelStr = "Easy";
		else if (Info.s_cpuLevel <= 40)
			cpuLevelStr = "Medium";
		else if (Info.s_cpuLevel <= 60)
			cpuLevelStr = "Competent";
		else if (Info.s_cpuLevel <= 80)
			cpuLevelStr = "Hard";
		else //if (Info.s_cpuLevel <= 100)
			cpuLevelStr = "Elite";
		CPULevelButtonText.text = "CPU: " + cpuLevelStr;
	}
	public void SwitchRivals()
	{
		if (Input.GetKey(KeyCode.LeftShift))
			Info.s_rivals -= 1;
		else
			Info.s_rivals += 1;
		Info.s_rivals %= 10;
		Info.s_rivals = Mathf.Clamp(Info.s_rivals, 0, 9);
		rivalsButtonText.text = "Rivals: " + Info.s_rivals.ToString();
	}
	public void SwitchReversed()
	{
		Info.s_reversed = !Info.s_reversed;
		reversedButtonText.text = "Reversed: " + (Info.s_reversed ? "Yes" : "No");
	}
	public void SwitchCatchup()
	{
		Info.s_catchup = !Info.s_catchup;
		catchupButtonText.text = "Catchup: " + (Info.s_catchup ? "Yes" : "No");
	}
	private void OnDisable()
	{ // in unity, 
		persistentSelectedTrack = selectedTrack.name;
		Debug.Log("Disable "+persistentSelectedTrack);
	}
	private void OnEnable()
	{
		if (loadCo)
		{
			StopCoroutine(Load());
		}
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
		foreach (var track in Info.tracks)
		{ // populate track grid
			if (track.Value.unlocked)
			{
				int trackOrigin = track.Value.TrackOrigin();
				var newtrack = Instantiate(trackImageTemplate, trackContent.GetChild(trackOrigin));
				newtrack.name = track.Key;
				newtrack.GetComponent<Image>().sprite = Resources.Load<Sprite>(Info.trackImagesPath + track.Key);
				newtrack.SetActive(true);
				existingTrackClasses[trackOrigin] = true;
				if (persistentSelectedTrack != null && persistentSelectedTrack == track.Key)
					selectedTrack = newtrack.transform;
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

		// set buttons
		for (int i = 0; i < existingTrackClasses.Length; ++i)
		{
			if (selectedTrack == null && existingTrackClasses[i])
			{
				selectedTrack = trackContent.GetChild(i).GetChild(0);
			}
			// disable car classes without children (required for sliders to work)
			trackContent.GetChild(i).gameObject.SetActive(existingTrackClasses[i]);
		}
		SetTiles();
		
		radial.gameObject.SetActive(selectedTrack);
		startButton.Select();

		if (selectedTrack == null)
		{
			trackDescText.text = "No tracks available";
		}
		else
		{
			buttonsContainer.GetChild(selectedTrack.parent.GetSiblingIndex()).GetComponent<MainMenuButton>().Select();
			trackDescText.text = Info.tracks[selectedTrack.name].desc;
			radial.SetAnimTo(selectedTrack.parent.GetSiblingIndex());
		}
		
		containerCo = StartCoroutine(MoveToTrack());
		radial.SetChildrenActive(existingTrackClasses);
			
		Debug.Log(selectedTrack);
	}
	void SetTiles()
	{
		void AddTile(in string spriteName)
		{
			var tile = Instantiate(tilesContainer.GetChild(0).gameObject, tilesContainer);
			tile.SetActive(true);
			tile.GetComponent<Image>().sprite = Resources.Load<Sprite>(Info.trackImagesPath + spriteName);
		}

		for (int i = 1; i < tilesContainer.childCount; ++i)
			Destroy(tilesContainer.GetChild(i).gameObject);

		AddTile(Enum.GetName(typeof(Info.CarGroup),Info.tracks[selectedTrack.name].preferredCarClass));
		AddTile(Enum.GetName(typeof(Info.CarGroup), Info.tracks[selectedTrack.name].envir));
		AddTile(Info.tracks[selectedTrack.name].trackDifficulty.ToString());
		foreach (var flag in Info.tracks[selectedTrack.name].flags)
			AddTile(Info.flagNames[flag]);
	}
	
	void Update()
	{
		if (!selectedTrack || loadCo)
			return;
		int x = Input.GetKeyDown(KeyCode.D) ? 1 : Input.GetKeyDown(KeyCode.A) ? -1 : 0;
		int y = Input.GetKeyDown(KeyCode.W) ? -1 : Input.GetKeyDown(KeyCode.S) ? 1 : 0;

		if (x != 0 || y != 0)
		{
			int posx = x + selectedTrack.GetSiblingIndex();
			int posy = y + selectedTrack.parent.GetSiblingIndex();
			if (posy >= 0 && posy <= 3 && posx>=0)
			{
				for (int i = posy; i < trackContent.childCount && i >= 0;)
				{
					Transform selectedClass = trackContent.GetChild(i);
					if (selectedClass.childCount > 0)
					{
						if (posx >= selectedClass.childCount)
							posx = selectedClass.childCount - 1;
						selectedTrack = selectedClass.GetChild(posx);
						Debug.Log(selectedTrack);
						break;
					}
					i = (y > 0) ? (i + 1) : (i - 1);
				}

				// new car has been selected
				// set description
				trackDescText.text = Info.tracks[selectedTrack.name].desc;
				// set bars
				radial.SetAnimTo(selectedTrack.parent.GetSiblingIndex());
				// focus on car
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
		float trackInGroupPos = selectedTrack.GetSiblingIndex() / (selectedTrack.parent.childCount - 1);
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
