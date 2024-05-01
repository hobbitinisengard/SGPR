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
		if(!F.I.gamePaused)
		{
			if (F.I.raceStartDate == DateTime.MinValue)
			{
				var timeNow = DateTime.Now;
				int val = timeNow.Hour;
				digits[0].sprite = sprites[val / 10];
				digits[1].sprite = sprites[val % 10];
				val = timeNow.Minute;
				digits[2].sprite = sprites[val / 10];
				digits[3].sprite = sprites[val % 10];
				val = timeNow.Second;
				digits[4].sprite = sprites[val / 10];
				digits[5].sprite = sprites[val % 10];
				digitColon.sprite = colons[(val % 2 == 0) ? 0 : 1];
			}
			else
			{
			
				TimeSpan timediff = (DateTime.UtcNow - F.I.raceStartDate).Duration();
				// minutes : seconds : ff
				int val = timediff.Minutes;
				digits[0].sprite = sprites[val / 10];
				digits[1].sprite = sprites[val % 10];

				val = timediff.Seconds;
				digits[2].sprite = sprites[val / 10];
				digits[3].sprite = sprites[val % 10];
				digitColon.sprite = colons[(val % 2 == 0) ? 0 : 1];
				val = timediff.Milliseconds / 10;
				digits[4].sprite = sprites[val / 10];
				digits[5].sprite = sprites[val % 10];

			}
		}
	}
}
