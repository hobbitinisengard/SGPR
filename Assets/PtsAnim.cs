using UnityEngine;
using UnityEngine.UI;
public class PtsAnim : Sfxable
{
	public enum PtsAnimType { Jump, Evo };
	public class PtsAnimInfo
	{
		public int score;
		public PtsAnimType type;
		public int level;
		public PtsAnimInfo(int score, PtsAnimType type, int level)
		{
			this.score = score;
			this.type = type;
			this.level = level;
		}
		public PtsAnimInfo(PtsAnimInfo pai)
		{
			this.score = pai.score;
			this.type = pai.type;
			this.level = pai.level;
		}
	}
	public AnimationCurve scaleCurve;
	public AnimationCurve visibilityCurve;
	public Sprite jumpImage;
	public Sprite EvoImage;
	public Sprite[] digitsSprites;
	public Image[] visibleDigits;
	public Color[] colorLevels;
	//public Color bronze;//FF9F00
	//public Color silver;//FFFFFF
	//public Color gold;//F1D200
	//public Color violet; //EA3863
	
	Image image;
	RectTransform rt;
	float timer;
	float duration = 1;
	private void Start()
	{
		image = GetComponent<Image>();
		rt = GetComponent<RectTransform>();
		timer = duration;
	}
	// Update is called once per frame
	void Update()
	{

		if (timer < duration)
		{
			rt.localScale = Vector3.one * scaleCurve.Evaluate(timer);
			var clr = image.color;
			clr.a = visibilityCurve.Evaluate(timer);
			image.color = clr;

			clr = visibleDigits[0].color;
			clr.a = visibilityCurve.Evaluate(timer);
			foreach (var digit in visibleDigits)
			{
				digit.color = clr;
			}
			var pos = rt.localPosition;
			pos.y = Screen.currentResolution.height / 2f * timer / duration;
			rt.localPosition = pos;
		}
		else
		{
			gameObject.SetActive(false);
		}
		timer += Time.deltaTime;
	}
	public void Play(in PtsAnimInfo pai)
	{
		if (pai == null)
			return;
		gameObject.SetActive(true);
		timer = 0;

		if (pai.type == PtsAnimType.Jump)
		{
			image.sprite = jumpImage;
			PlaySFX("swoosh2");
		}
		else
		{
			image.sprite = EvoImage;
			PlaySFX("swoosh1");
		}
		pai.level = Mathf.Clamp(pai.level, 0, colorLevels.Length - 1);
		image.color = colorLevels[pai.level];
		rt.localScale = Vector3.zero;

		int score = pai.score;
		int digits = (int)CountDigit(score) - 1;
		foreach (var vd in visibleDigits)
			vd.gameObject.SetActive(false);

		for (int i = digits; i >= 0; --i)
		{
			try
			{
				int letter = score % 10;
				visibleDigits[i].sprite = digitsSprites[letter];
				visibleDigits[i].gameObject.SetActive(true);
				score /= 10;
			}
			catch
			{
				Debug.Log(pai.score);
				break;
			}
		}
	}
	float CountDigit(int number)
	{
		return Mathf.Floor(Mathf.Log10(number) + 1);
	}
}
