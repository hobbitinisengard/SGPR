using UnityEngine;
public class InfoLoader : MonoBehaviour
{
	private void Awake()
	{
		Info.PopulateCarsData();
	}
}
