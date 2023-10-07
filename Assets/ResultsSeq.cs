using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class ResultsSeq : Sfxable
{
	public SGP_HUD playerHUD;
	public Sprite[] ResultPositionSprites;
	public Image imgResult;
	float minImgResultPos;
	public Transform[] boxes;
	public RectTransform leftRow;
	public RectTransform rightRow;
	public TextMeshProUGUI rightBoxLabel;
	public TextMeshProUGUI bottomBoxLabel;
	public Image dimmer;
	//AnimationCurve pulseCurve;
	Color yellowDark = new Color(0.3607f, 0.3607f, 0);
	string[] rightBoxLabels = new string[] { "DRIFT POINTS", "RACE TIME", "AEROMILES", "BEST LAP"};
	int rightBoxLabelInt = 0;
	Coroutine seq, dimCo,showResultCo, showTableCo;
	int playerResultPosition = 0;
	AudioSource audioSource;
	public float d_invLerp;
	public float d_minMax;
	float cosArg;
	AudioSource podiumMusic;

	private void Awake()
	{
		//pulseCurve.AddKey(0, 1);
		//pulseCurve.AddKey(0.5f, 0);
		//pulseCurve.AddKey(1, 1);
		audioSource = GetComponent<AudioSource>();
	}
	private void OnEnable()
	{
		rightBoxLabelInt = 0;
		foreach (var b in boxes)
		{
			b.gameObject.SetActive(false);
		}
		for (int i = 0; i < leftRow.childCount; ++i)
		{
			leftRow.GetChild(i).gameObject.SetActive(false);
			rightRow.GetChild(i).gameObject.SetActive(false);
		}

		cosArg = 0;
		playerResultPosition = playerHUD.raceManager.Position(playerHUD.vp);
		Debug.Log(playerResultPosition);
		imgResult.sprite = ResultPositionSprites[playerResultPosition - 1];
		imgResult.transform.GetChild(0).gameObject.SetActive(playerResultPosition > 3);
		minImgResultPos = -Screen.height / 2f - imgResult.transform.GetComponent<RectTransform>().sizeDelta.y / 2f;

		imgResult = transform.GetChild(0).GetComponent<Image>();
		rightBoxLabel.text = rightBoxLabels[rightBoxLabelInt];
		// populate names
		for (int i = 0; i < Info.s_rivals; ++i)
		{
			leftRow.GetChild(i).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = "NAME" + i.ToString();
		}

		if (showResultCo != null)
			StopCoroutine(showResultCo);
		if (seq != null)
			StopCoroutine(seq);
		if (dimCo != null)
			StopCoroutine(dimCo);
		if (showTableCo != null)
			StopCoroutine(showTableCo);
		dimmer.color = new Color(0, 0, 0, 0);
		dimCo = showResultCo = showTableCo = null;
		seq = StartCoroutine(Seq());
		audioSource.volume = 1;
		podiumMusic = PlaySFX(playerResultPosition <= 3 ? "RacePodium" : "RaceNotPodium");
	}
	void ImgResultSetAlpha(in Color c)
	{
		imgResult.color = c;
		imgResult.transform.GetChild(0).GetComponent<Image>().color = c;
	}
	IEnumerator ShowResult()
	{
		imgResult.gameObject.SetActive(true);
		float timer = 0;
		while(timer < 4)
		{
			if (timer <= 1f)
			{
				ImgResultSetAlpha(new Color(1, 1, 1, timer));
			}
			if (!audioSource.isPlaying)
				audioSource.Play();
			if (Input.GetKeyDown(KeyCode.Return))
				timer = 3.5f;
			if (timer >= 3.5f)
				ImgResultSetAlpha(new Color(1, 1, 1, 1 - Mathf.InverseLerp(3.5f, 4, timer)));

			cosArg += 2 * Mathf.PI * Time.deltaTime;
			var p = imgResult.transform.localPosition;
			p.y = Mathf.InverseLerp(2, 0, 2 * Easing.OutCubic(timer / 2f)) * minImgResultPos * Mathf.Cos(cosArg);
			imgResult.transform.localPosition = p;
			timer += Time.deltaTime;
			yield return null;
		}
		imgResult.gameObject.SetActive(false);
	}
	IEnumerator Seq()
	{
		float timer = 0;
		while (true)
		{
			timer += Time.deltaTime;
			if (timer < 8 && Input.GetKey(KeyCode.Return))
			{
				timer = 8;
				if (podiumMusic != null)
					podiumMusic.Stop();
				yield return null;
			}

			if (timer > 8 && timer < 12)
			{
				if (Input.GetKeyDown(KeyCode.Return))
					timer = 11.5f;
				if(showResultCo == null)
					showResultCo = StartCoroutine(ShowResult());
			}

			if (timer > 12 && timer <= 13)
			{ // boxes slide In
				foreach (var b in boxes)
				{
					b.gameObject.SetActive(true);
				}
			}

			if (timer >= 13 && showTableCo==null)
			{
				showTableCo = StartCoroutine(ShowTableCo());
				yield break;
			}
			yield return null;
		}
	}
	IEnumerator ShowTableCo()
	{
		leftRow.GetComponent<VerticalLayoutGroup>().enabled = true;
		rightRow.GetComponent<VerticalLayoutGroup>().enabled = true;

		float TimeRequiredForUpdate = 8;
		float timer = TimeRequiredForUpdate+1;
		for (int i = leftRow.childCount-1; i >=0 ; --i)
		{
			leftRow.GetChild(i).gameObject.SetActive(i + 1 <= Info.s_rivals);
			rightRow.GetChild(i).gameObject.SetActive(i + 1 <= Info.s_rivals);
		}
		yield return null;

		leftRow.GetComponent<VerticalLayoutGroup>().enabled = false;
		rightRow.GetComponent<VerticalLayoutGroup>().enabled = false;

		yield return null;

		while(true)
		{
			if (Input.GetKeyDown(KeyCode.Return) && dimCo == null)
			{
				for (int i = 0; i < Info.s_rivals; ++i)
				{ // closing seq
					leftRow.GetChild(i).gameObject.GetComponent<SlideInOut>().PlaySlideOut(true);
					rightRow.GetChild(i).gameObject.GetComponent<SlideInOut>().PlaySlideOut(true);
					foreach (var b in boxes)
						b.GetComponent<SlideInOut>().PlaySlideOut(true);
				}
				dimCo = StartCoroutine(DimmerWorks());
				yield break;
			}

			RowsBlinkColor((timer % 1f < 0.5f) ? yellowDark : Color.yellow);

			if (timer > TimeRequiredForUpdate)
			{
				timer = 0;
				SetRightRowBoxesValues(++rightBoxLabelInt % rightBoxLabels.Length);
			}
			timer += Time.deltaTime;
			yield return null;
		}
	}
	IEnumerator DimmerWorks()
	{
		float timer = 2;
		while (timer > 0)
		{
			dimmer.color = new Color(0,0,0, 1-timer/2f);
			audioSource.volume = timer / 2f;
			timer -= Time.deltaTime;
			yield return null;
		}
		audioSource.Stop();
		gameObject.SetActive(false);
	}
	void RowsBlinkColor(Color color)
	{
		bottomBoxLabel.color = color;
		rightRow.GetChild(playerResultPosition - 1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().color = color;
		leftRow.GetChild(playerResultPosition - 1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().color = color;
	}
	void SetRightRowBoxesValues(int labelInt)
	{
		rightBoxLabel.text = rightBoxLabels[labelInt];
		for (int i = 0; i < Info.s_rivals; ++i)
		{
			rightRow.GetChild(i).GetChild(0).GetChild(0)
					.GetComponent<TextMeshProUGUI>().text = "VAL" + Random.value.ToString("f3");
		}
	}

}
