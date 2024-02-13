using UnityEngine;
using System.IO;

public class LoadSelector : TrackSelectorTemplate
{
	public void RemoveCurrentTrack()
	{
		if (selectedTrack.parent.GetSiblingIndex() == 0 || selectedTrack == null)
		{
			PlaySFX("fe-cardserror");
			return;
		}
		
		Info.tracks.Remove(selectedTrack.name);
		File.Delete(Info.tracksPath + selectedTrack.name + ".track");
		File.Delete(Info.tracksPath + selectedTrack.name + ".data");
		File.Delete(Info.tracksPath + selectedTrack.name + ".png");

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
	//			newtrack.GetComponent<Image>().sprite = IMG2Sprite.LoadNewSprite(Path.Combine(Info.tracksPath, newtrack.name + ".png"));
	//			newtrack.SetActive(true);
	//			existingTrackClasses[trackOrigin] = true;
	//			if (persistentSelectedTrack != null && persistentSelectedTrack == key)
	//			{
	//				selectedTrack = newtrack.transform;
	//				Info.s_trackName = selectedTrack.name;
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
	//		sortedTracks = Info.tracks.OrderBy(t => t.Key).Select(kv => kv.Key).ToArray();
	//	else //if(curSortingCondition == SortingCond.Difficulty)
	//		sortedTracks = Info.tracks.OrderBy(t => t.Value.difficulty).Select(kv => kv.Key).ToArray();
	//	foreach (var tname in sortedTracks)
	//		AddTrackImage(tname, Info.tracks[tname]);
	//	return existingTrackClasses;
	//}
	
	
	
	
}