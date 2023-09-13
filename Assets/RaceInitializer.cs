using UnityEngine;

public class RaceInitializer : MonoBehaviour
{
	AudioSource audioSource;
	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		Info.PopulateSFXData();
		Info.PopulateCarsData();
		Info.PopulateTrackData();
		//audioSource.clip = Resources.Load<AudioClip>("music/JAP");
	}
	void Start()
	{
		audioSource.Play();
	}
}
