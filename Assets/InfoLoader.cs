using System.IO;
using UnityEngine;
public class InfoLoader : MonoBehaviour
{
	private void Awake()
	{
		if (!Directory.Exists(Info.documents_sgpr_path))
		{
			Directory.CreateDirectory(Info.documents_sgpr_path);
		}
		Info.PopulateCarsData();
		Info.PopulateTrackData();
		Info.icons = Resources.LoadAll<Sprite>(Info.trackImagesPath + "tiles");
		Info.PopulateSFXData();
	}
}
