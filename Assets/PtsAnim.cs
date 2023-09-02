using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
public class PtsAnim : MonoBehaviour
{
    public AnimationCurve scaleCurve;
    public AnimationCurve visibilityCurve;
    public Sprite jumpImage;
    public Sprite EvoImage;
    public Sprite[] digitsSprites;
    public Image[] visibleDigits;
    public Color bronze;
    public Color silver;
    public Color gold;
    public Color violet;
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
        if(timer < duration)
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
	public void Play(int score, in Sprite sprite, in Color color)
    {
		gameObject.SetActive(true);
		timer = 0;

        image.sprite = sprite;
        image.color = color;
        rt.localScale = Vector3.zero;

		int digits = (int)CountDigit(score) - 1;
        foreach (var vd in visibleDigits)
            vd.gameObject.SetActive(false);

		for (int i = digits; i >= 0; --i)
		{
			int letter = score % 10;
			visibleDigits[i].sprite = digitsSprites[letter];
            visibleDigits[i].gameObject.SetActive(true);
			score /= 10;
		}
	}
	float CountDigit(int number)
	{
		return Mathf.Floor(Mathf.Log10(number) + 1);
	}
}
