using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class TrackSelectorTemplate : Selector
{
	public GameObject trackImageTemplate;
	public MainMenuButton startButton;
	public RectTransform trackContent;
	public Scrollbar scrollx;
	public Scrollbar scrolly;
	public Transform tilesContainer;
	public Transform recordsContainer;
	public MainMenuButton sortButton;
	public TextMeshProUGUI trackDescText;
	public RadialOneVisible radial;
	/// <summary>
	/// If true, populate menu only with valid tracks
	/// </summary>
	public bool checkForValid;
	protected Transform selectedTrack;
	protected enum SortingCond { Difficulty, Name };
	protected SortingCond curSortingCondition = SortingCond.Difficulty;
	protected bool loadCo;
	protected Coroutine containerCo;
	protected string persistentSelectedTrack;

	protected virtual void OnDisable()
	{
		move2Ref.action.performed -= CalculateTargetToSelect;
		persistentSelectedTrack = selectedTrack.name;
	}
	protected virtual void OnEnable()
	{
		F.I.s_spectator = false;
		move2Ref.action.performed += CalculateTargetToSelect;
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load());
	}
	bool ValidCheck(bool trackValid)
	{
		if (checkForValid)
			return trackValid;
		else
			return true;
	}
	bool[] PopulateContent()
	{
		
		bool[] existingTrackClasses = new bool[2];

		for (int i = 0; i < trackContent.childCount; ++i)
		{  // remove tracks from previous entry
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
			sortedTracks = F.I.tracks.OrderBy(t => t.Key).Select(kv => kv.Key).ToArray();
		else //if(curSortingCondition == SortingCond.Difficulty)
			sortedTracks = F.I.tracks.OrderBy(t => t.Value.difficulty).Select(kv => kv.Key).ToArray();

		foreach (var trackName in sortedTracks)
		{
			TrackHeader track = F.I.tracks[trackName];
			if (track.unlocked && ValidCheck(track.valid))
			{
				int trackOrigin = track.TrackOrigin();
				var newtrack = Instantiate(trackImageTemplate, trackContent.GetChild(trackOrigin));
				newtrack.name = trackName;
				newtrack.GetComponent<Image>().sprite = IMG2Sprite.LoadNewSprite(Path.Combine(F.I.tracksPath, trackName + ".png"));
				newtrack.SetActive(true);
				existingTrackClasses[trackOrigin] = true;
				if (persistentSelectedTrack != null && persistentSelectedTrack == trackName)
				{
					selectedTrack = newtrack.transform;
				}
			}
		}
		return existingTrackClasses;
	}
	protected IEnumerator Load(string specificTrackName = null, bool forceReload = false)
	{
		loadCo = true;
		if (specificTrackName!= null)
			F.I.s_trackName = specificTrackName;

		int visibleTracks = trackContent.GetChild(0).childCount + (trackContent.GetChild(1) != null ? trackContent.GetChild(1).childCount : 0);
		int validTracks = F.I.tracks.Count(t => ValidCheck(t.Value.valid));

		bool reloadContent = (visibleTracks != validTracks) || forceReload;

		bool[] existingTrackClasses = reloadContent ? PopulateContent() 
			: new bool[] { trackContent.GetChild(0).gameObject.activeSelf, trackContent.GetChild(1).gameObject.activeSelf };

		yield return null; // wait for one frame for active objects to refresh

		selectedTrack = null;

		for (int i = 0; i < existingTrackClasses.Length; ++i)
		{
			if (selectedTrack == null && existingTrackClasses[i])
			{
				if (trackContent.GetChild(i).childCount > 0)
				{
					Transform Trackclass = trackContent.GetChild(i);
					for(int j=0; j<Trackclass.childCount; ++j)
					{
						if (Trackclass.GetChild(j).name == F.I.s_trackName)
						{
							selectedTrack = Trackclass.GetChild(j);
						}
					}
				}
			}
			// disable track classes without children (required for sliders to work)
			trackContent.GetChild(i).gameObject.SetActive(existingTrackClasses[i]);
		}
		if (F.I.randomTracks)
		{
			var randomClass =  trackContent.GetChild(UnityEngine.Random.Range(0, 2));
			F.I.s_trackName = randomClass.GetChild(UnityEngine.Random.Range(0, randomClass.childCount)).name;
		}
		else
		{
			if (selectedTrack == null)
				selectedTrack = trackContent.GetChild(0).GetChild(0);
			F.I.s_trackName = selectedTrack.name;
		}
		Debug.Log(F.I.s_trackName);
		SetTiles();
		SetRecords();
		radial.gameObject.SetActive(selectedTrack);

		if (selectedTrack == null)
			trackDescText.text = "No tracks available";
		else
		{
			trackDescText.text = selectedTrack.name + "\n\n" + F.I.tracks[selectedTrack.name].desc;
			if (radial.gameObject.activeSelf)
				radial.SetAnimTo(selectedTrack.parent.GetSiblingIndex());
		}
		startButton.Select();
		containerCo = StartCoroutine(MoveToTrack());
		radial.SetChildrenActive(trackContent);

		loadCo = false;
	}

	protected void CalculateTargetToSelect(InputAction.CallbackContext ctx)
	{
		if (!selectedTrack || loadCo)
			return;
		Vector2 move2 = move2Ref.action.ReadValue<Vector2>();
		int mult = (shiftInputRef.action.ReadValue<float>() > 0) ? 5 : 1;
		int x = Mathf.RoundToInt(move2.x) * mult;
		int y = Mathf.RoundToInt(-move2.y) * mult;
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
					F.I.s_trackName = selectedTrack.name;
					PlaySFX("fe-bitmapscroll");
				}
				// new track has been selected
				// set description
				trackDescText.text = selectedTrack.name + "\n\n" + F.I.tracks[selectedTrack.name].desc;

				if(radial.gameObject.activeSelf)
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
	public void SortTrackList()
	{
		curSortingCondition = (SortingCond)(((int)curSortingCondition + 1) % 2);
		if (curSortingCondition == SortingCond.Name)
			sortButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Sorted by track's name";
		else
			sortButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Sorted by track's difficulty";
		// reload 
		if (loadCo)
			StopCoroutine(Load());
		StartCoroutine(Load(F.I.s_trackName, forceReload: true));
	}
	protected void SetRecords()
	{
		for (int i = 0; i < recordsContainer.childCount; ++i)
		{
			Transform record = recordsContainer.GetChild(i);

			Record recordData = selectedTrack ? F.I.tracks[selectedTrack.name].records[i] : new Record(null, 0, 0);

			string valueStr = (recordData == null || recordData.playerName == null || recordData.secondsOrPts == 0 
				|| recordData.secondsOrPts > 35000) ? "" : recordData.playerName;

			record.GetChild(1).GetComponent<Text>().text = valueStr;
			if (recordData == null || recordData.secondsOrPts == 0 || recordData.secondsOrPts == 35999)
				valueStr = "";
			else
			{
				if (i <= 1) // lap, race, stunt, grip
					valueStr = TimeSpan.FromSeconds(recordData.secondsOrPts).ToLaptimeStr();
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
	protected void SetTiles()
	{
		void AddTile(string spriteName)
		{
			var tile = Instantiate(tilesContainer.GetChild(0).gameObject, tilesContainer);
			tile.SetActive(true);
			tile.name = spriteName;
			try
			{
				tile.GetComponent<Image>().sprite = F.I.icons.First(i => i.name == spriteName);
			}
			catch
			{
				Debug.LogError(spriteName);
			}
		}

		for (int i = 1; i < tilesContainer.childCount; ++i)
			Destroy(tilesContainer.GetChild(i).gameObject);

		if (selectedTrack)
		{
			AddTile(Enum.GetName(typeof(CarGroup), F.I.tracks[selectedTrack.name].preferredCarClass));
			AddTile(Enum.GetName(typeof(Envir), F.I.tracks[selectedTrack.name].envir));
			AddTile((F.I.tracks[selectedTrack.name].difficulty + 4).ToString());
			foreach (var flag in F.I.tracks[selectedTrack.name].icons)
				AddTile(F.I.IconNames[flag]);
		}
	}
	protected IEnumerator MoveToTrack()
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
		float trackInGroupPos = F.I.InGroupPos(selectedTrack);
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

public class Selector : Sfxable
{
	public InputActionReference move2Ref;
	public InputActionReference shiftInputRef;
	public InputActionReference ctrlInputRef;
	public InputActionReference altInputRef;
}
