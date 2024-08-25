using System.IO;
using UnityEngine;

public class LoadSelector : TrackSelectorTemplate
{
	public void RemoveCurrentTrack()
	{
		if (selectedTrack == null || selectedTrack.parent.childCount == 1)
		{
			PlaySFX("fe-cardserror");
			return;
		}

		string FolderNamePath = F.I.tracksPath + selectedTrack.name;
		File.Delete(FolderNamePath + ".track");
		File.Delete(FolderNamePath + ".data");
		File.Delete(FolderNamePath + ".jpg");
		if (File.Exists(FolderNamePath + ".rec"))
			File.Delete(FolderNamePath + ".rec");

		F.I.tracks.Remove(selectedTrack.name);

		int i = selectedTrack.GetSiblingIndex();
		var tr = selectedTrack.parent;
		DestroyImmediate(selectedTrack.gameObject);
		i = Mathf.Max(0, i - 1);
		selectedTrack = tr.GetChild(i);

		StartCoroutine(Load(selectedTrack.name));
		//containerCo = StartCoroutine(MoveToTrack());
	}
	//bool[] PopulateContent()
	//{
	//	bool[] existingTrackClasses = new bool[2];

	//	//-----------------
	//	void AddTrackImage(in string key, in TrackHeader value)
	//	{
	//		if (value.unlocked)
	//		{
	//			int trackOrigin = value.TrackOrigin();
	//			var newtrack = Instantiate(trackImageTemplate, trackContent.GetChild(trackOrigin));
	//			newtrack.name = key;
	//			newtrack.GetComponent<Image>().sprite = IMG2Sprite.LoadNewSprite(Path.Combine(F.I.tracksPath, newtrack.name + ".png"));
	//			newtrack.SetActive(true);
	//			existingTrackClasses[trackOrigin] = true;
	//			if (persistentSelectedTrack != null && persistentSelectedTrack == key)
	//			{
	//				selectedTrack = newtrack.transform;
	//				F.I.s_trackName = selectedTrack.name;
	//			}
	//		}
	//	}
	//	//-------------------

	//	for (int i = 0; i < trackContent.childCount; ++i)
	//	{ // remove tracks from previous entry
	//		Transform trackClass = trackContent.GetChild(i);
	//		for (int j = 0; j < trackClass.childCount; ++j)
	//		{
	//			//Debug.Log(trackClass.GetChild(j).name);
	//			Destroy(trackClass.GetChild(j).gameObject);
	//		}
	//	}
	//	string[] sortedTracks;
	//	// populate track grid	
	//	if (curSortingCondition == SortingCond.Name)
	//		sortedTracks = F.I.tracks.OrderBy(t => t.Key).Select(kv => kv.Key).ToArray();
	//	else //if(curSortingCondition == SortingCond.Difficulty)
	//		sortedTracks = F.I.tracks.OrderBy(t => t.Value.difficulty).Select(kv => kv.Key).ToArray();
	//	foreach (var tname in sortedTracks)
	//		AddTrackImage(tname, F.I.tracks[tname]);
	//	return existingTrackClasses;
	//}
	
	
	
	
}