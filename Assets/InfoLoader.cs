using UnityEngine;
public class InfoLoader : MonoBehaviour
{
	private void Awake()
	{
		Info.PopulateCarsData();
		Info.PopulateTrackData();
		Info.icons = Resources.LoadAll<Sprite>(Info.trackImagesPath + "tiles");
	}
}
