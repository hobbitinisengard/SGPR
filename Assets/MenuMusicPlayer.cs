using UnityEngine;

public class MenuMusicPlayer : MonoBehaviour
{
	AudioSource audioPlayer;
	private void Awake()
	{
		audioPlayer = transform.parent.GetComponent<AudioSource>();
	}
	private void OnEnable()
	{
		audioPlayer.Play();
	}
}
