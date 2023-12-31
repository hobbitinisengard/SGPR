using RVP;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using static TrackHeader;
using TMPro;
using System.IO;

public class LoadSelector : Sfxable
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
		if (curSortingCondition == SortingCond.Name)
			sortButton.transform.GetChild(0).GetComponent<Text>().text = "Sorted by track's name";
		else
			sortButton.transform.GetChild(0).GetComponent<Text>().text = "Sorted by track's difficulty";
		// reload 
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load());
	}

	private void OnDisable()
	{ // in unity, 
		persistentSelectedTrack = selectedTrack.name;
		//Debug.Log("Disable "+persistentSelectedTrack);
	}
	private void OnEnable()
	{
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load());
		
	}
	public void RemoveCurrentTrack()
	{
		if (selectedTrack.parent.GetSiblingIndex() == 0 || selectedTrack == null)
		{
			PlaySFX("fe-cardserror");
			return;
		}
		
		Info.tracks.Remove(selectedTrack.name);
		File.Delete(Info.documentsSGPRpath + selectedTrack.name + ".track");
		File.Delete(Info.documentsSGPRpath + selectedTrack.name + ".data");
		File.Delete(Info.documentsSGPRpath + selectedTrack.name + ".png");

		int children = selectedTrack.parent.childCount;
		int index = Mathf.Clamp(selectedTrack.GetSiblingIndex(),0,children-2);
		Destroy(selectedTrack.gameObject);
		if (selectedTrack.parent.childCount == 0)
			selectedTrack = null;
		else
			selectedTrack = trackContent.GetChild(1).GetChild(index);

		StartCoroutine(Load());
		//containerCo = StartCoroutine(MoveToTrack());
	}
	bool[] PopulateContent()
	{
		bool[] existingTrackClasses = new bool[2];

		//-----------------
		void AddTrackImage(in string key, in TrackHeader value)
		{
			if (value.unlocked)
			{
				int trackOrigin = value.TrackOrigin();
				var newtrack = Instantiate(trackImageTemplate, trackContent.GetChild(trackOrigin));
				newtrack.name = key;
				newtrack.GetComponent<Image>().sprite = IMG2Sprite.LoadNewSprite(Path.Combine(Info.documentsSGPRpath, newtrack.name + ".png"));
				newtrack.SetActive(true);
				existingTrackClasses[trackOrigin] = true;
				if (persistentSelectedTrack != null && persistentSelectedTrack == key)
				{
					selectedTrack = newtrack.transform;
					Info.s_trackName = selectedTrack.name;
				}
			}
		}
		//-------------------

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
		foreach (var tname in sortedTracks)
			AddTrackImage(tname, Info.tracks[tname]);
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
				if(trackContent.GetChild(i).childCount > 0)
				{
					selectedTrack = trackContent.GetChild(i).GetChild(0);
					Info.s_trackName = selectedTrack.name;
				}
			}
			// disable track classes without children (required for sliders to work)
			trackContent.GetChild(i).gameObject.SetActive(existingTrackClasses[i]);
		}
		SetTiles();
		SetRecords();
		radial.gameObject.SetActive(selectedTrack);
		startButton.Select();

		if (selectedTrack == null)
			trackDescText.text = "No tracks available";
		else
		{
			trackDescText.text = selectedTrack.name + "\n\n" + Info.tracks[selectedTrack.name].desc;
			radial.SetAnimTo(selectedTrack.parent.GetSiblingIndex());
		}
		
		containerCo = StartCoroutine(MoveToTrack());
		radial.SetChildrenActive(existingTrackClasses);

		Debug.Log(selectedTrack);
		loadCo = false;
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

			if (recordData != null && (i>0 && recordData.secondsOrPts > recordData.requiredSecondsOrPts) 
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
			int posx = gotoHome ? 0 : gotoEnd ? selectedTrack.parent.childCount - 1 : x + selectedTrack.GetSiblingIndex();
			if (posx < 0)
				posx = 0;
			int posy = y + selectedTrack.parent.GetSiblingIndex();
			if (posy >= 0 && posy <= 3 && posx >= 0)
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
				if (tempSelectedTrack != null && tempSelectedTrack != selectedTrack)
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
