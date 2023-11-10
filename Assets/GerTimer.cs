using System;
using UnityEngine;

public class GerTimer : MonoBehaviour
{
	public Sprite[] sprites;
	public SpriteRenderer[] digits;
	public Sprite[] colons;
	public SpriteRenderer digitColon;
	void Update()
	{
		if (Info.raceStartDate == DateTime.MinValue)
			return;
		TimeSpan timediff = DateTime.Now - Info.raceStartDate;
		// minutes : seconds : ff
		int minutes = timediff.Minutes;
		digits[0].sprite = sprites[minutes / 10];
		digits[1].sprite = sprites[minutes % 10];

		int seconds = timediff.Seconds;
		digits[2].sprite = sprites[seconds / 10];
		digits[3].sprite = sprites[seconds % 10];

		int tens = timediff.Milliseconds / 10;
		digits[4].sprite = sprites[tens / 10];
		digits[5].sprite = sprites[tens % 10];
		digitColon.sprite = colons[(tens < 50) ? 0 : 1];
	}
}
