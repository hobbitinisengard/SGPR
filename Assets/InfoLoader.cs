using UnityEngine;
using System.Collections.Generic;
public class InfoLoader : MonoBehaviour
{
	private void Awake()
	{
		Info.PopulateCarsData();
		
	}
}
