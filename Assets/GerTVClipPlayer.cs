using UnityEngine;
using UnityEngine.Video;

public class GerTVClipPlayer : MonoBehaviour
{
	public VideoPlayer videoPlayer;
	public VideoClip[] videos;
	void Start()
	{
		int i = Random.Range(0, videos.Length);
		videoPlayer.clip = videos[i];	
	}
}
