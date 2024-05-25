using RVP;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class ResultsSeq : MonoBehaviour
{
	public TextMeshProUGUI pressEnterText;
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
	Color yellowDark = new (0.3607f, 0.3607f, 0);
	string[] rightBoxLabels = new string[] { "BEST LAP", "RACE-TIME", "AEROMILES", "DRIFT"};
	int rightBoxLabelInt = 0;
	Coroutine seq, dimCo,showResultCo, showTableCo;
	AudioSource audioSource;
	public float d_invLerp;
	public float d_minMax;
	float cosArg;
	bool submitFlag;
	Coroutine setTableBoxesValuesCo;
	Coroutine bottomTextCo;
	private float lastRoundEndedTime;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}
	private void OnDisable()
	{
		F.I.enterRef.action.performed -= OnEnterClicked;
		if(bottomTextCo != null)
			StopCoroutine(bottomTextCo);
	}
	private void OnEnable()
	{
		lastRoundEndedTime = 0;
		submitFlag = false;
		F.I.enterRef.action.performed += OnEnterClicked;

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
		var playerResultPosition = RaceManager.I.Position(RaceManager.I.playerCar);
		imgResult.sprite = ResultPositionSprites[playerResultPosition];
		imgResult.transform.GetChild(0).gameObject.SetActive(playerResultPosition > 2);
		minImgResultPos = -Screen.height / 2f - imgResult.transform.GetComponent<RectTransform>().sizeDelta.y / 2f;
		imgResult = transform.GetChild(0).GetComponent<Image>();
		rightBoxLabel.text = rightBoxLabels[rightBoxLabelInt];
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
		audioSource.clip = F.I.audioClips[(playerResultPosition <= 2) ? "RacePodium" : "RaceNotPodium"];
		audioSource.loop = false;
		audioSource.Play();
	}
	private void OnEnterClicked(UnityEngine.InputSystem.InputAction.CallbackContext obj)
	{
		submitFlag = true;
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
			{
				audioSource.clip = F.I.audioClips["RaceResults"];
				audioSource.loop = true;
				audioSource.Play();
			}
			if (timer >= 3.5f)
				ImgResultSetAlpha(new Color(1, 1, 1, 1 - Mathf.InverseLerp(3.5f, 4, timer)));
			else if ( submitFlag)
			{
				timer = 3.5f;
				submitFlag = false;
			}

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
			if (timer < 8 && submitFlag)
			{
				submitFlag = false;
				timer = 8;
				if (audioSource != null)
					audioSource.Stop();
			}

			if (timer > 8 && timer < 12)
			{
				if (submitFlag)
				{
					submitFlag = false;
					timer = 12;
				}
				showResultCo ??= StartCoroutine(ShowResult());
			}

			if (timer > 12 && timer <= 13)
			{ // boxes slide In
				foreach (var b in boxes)
				{
					b.gameObject.SetActive(true);
				}
				if(showTableCo == null)
					showTableCo = StartCoroutine(ShowTableCo());
				yield break;
			}
			yield return null;
		}
	}
	IEnumerator BottomText()
	{
		while(true)
		{
			if (F.I.gameMode == MultiMode.Multiplayer && ResultsView.Count < ServerC.I.lobby.Players.Count)
			{
				pressEnterText.text = "WAITING";
			}
			else
			{
				if (F.I.gameMode == MultiMode.Multiplayer)
				{
					if (F.I.CurRound == F.I.Rounds)
						lastRoundEndedTime = Time.time;
				}
				pressEnterText.text = "PRESS ENTER";
				yield break;
			}
			yield return new WaitForSeconds(1);
		}
	}
	IEnumerator ShowTableCo()
	{
		float TimeRequiredForUpdate = 5;
		float timer = TimeRequiredForUpdate+1;

		if (bottomTextCo != null)
			StopCoroutine(bottomTextCo);
		bottomTextCo = StartCoroutine(BottomText());

		while(true)
		{
			if (submitFlag && dimCo == null) // CLOSING SEQUENCE
			{
				if (F.I.gameMode == MultiMode.Singleplayer 
					|| (F.I.gameMode == MultiMode.Multiplayer && ResultsView.Count >= ServerC.I.lobby.Players.Count))
				{
					
					foreach (var b in boxes)
						b.GetComponent<SlideInOut>().PlaySlideOut(true);

					if (setTableBoxesValuesCo != null)
						StopCoroutine(setTableBoxesValuesCo);

					int activeOnes = leftRow.ActiveChildren();
					
					for (int i = 0; i < activeOnes; ++i)
					{
						leftRow.GetChild(i).gameObject.GetComponent<SlideInOut>().PlaySlideOut(true);
						rightRow.GetChild(i).gameObject.GetComponent<SlideInOut>().PlaySlideOut(true);
						yield return new WaitForSecondsRealtime(.2f);
					}

					// Wait for scores of online players to be updated before proceeding to ResultsView and WinnerView
					if (F.I.CurRound == F.I.Rounds && lastRoundEndedTime != 0)
						yield return new WaitForSecondsRealtime(3 - (Time.time - lastRoundEndedTime));

					dimCo = StartCoroutine(DimmerWorks());
					yield break;
				}
			}

			// update row colors
			Color playerColor = (timer % 1f < 0.5f) ? yellowDark : Color.yellow;
			bottomBoxLabel.color = playerColor;
			for (int i = 0; i < leftRow.childCount; ++i)
			{
				var leftChildText = leftRow.GetChild(i).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
				var rightChildText = rightRow.GetChild(i).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
				Color rowColor =  (leftChildText.text == F.I.playerData.playerName) ? playerColor : Color.yellow;
				// Acknowledge already set transparency of boxes. Set only color.
				var a = rightChildText.color.a;
				rowColor.a = a;
				rightChildText.color = rowColor;
				leftChildText.color = rowColor;
			}
			
			if (timer > TimeRequiredForUpdate)
			{
				timer = 0;
				++rightBoxLabelInt;
				rightBoxLabelInt %= rightBoxLabels.Length;

				if (setTableBoxesValuesCo != null)
					StopCoroutine(setTableBoxesValuesCo);
				setTableBoxesValuesCo = StartCoroutine(SetTableBoxesValues());
			}
			if (ResultsView.Count != leftRow.ActiveChildren())
			{
				if (setTableBoxesValuesCo != null)
					StopCoroutine(setTableBoxesValuesCo);
				setTableBoxesValuesCo = StartCoroutine(SetTableBoxesValues());
			}
			timer += Time.deltaTime;
			yield return null;
		}
	}
	IEnumerator DimmerWorks()
	{
		
		float timer = .5f;
		while (timer > 0)
		{
			//dimmer.color = new Color(0,0,0, 1-timer/2f);
			audioSource.volume = timer / 2f;
			timer -= Time.deltaTime;
			yield return null;
		}
		audioSource.Stop();
		gameObject.SetActive(false);
	}
	IEnumerator SetTableBoxesValues()
	{
		rightBoxLabel.text = rightBoxLabels[rightBoxLabelInt];
		var list = ResultsView.SortedResultsByFinishPos;
		for (int i = 0; i < list.Count; ++i)
		{
			leftRow.GetChild(i).GetChild(0).GetChild(0)
						.GetComponent<TextMeshProUGUI>().text = list[i].name;

			rightRow.GetChild(i).GetChild(0).GetChild(0)
						.GetComponent<TextMeshProUGUI>().text = list[i].ToString((RecordType)rightBoxLabelInt);
		}

		for (int i = 0; i < ResultsView.Count; ++i)
		{
			if(!leftRow.GetChild(i).gameObject.activeSelf)
			{
				leftRow.GetChild(i).gameObject.SetActive(i + 1 <= ResultsView.Count);
				rightRow.GetChild(i).gameObject.SetActive(i + 1 <= ResultsView.Count);
				yield return new WaitForSecondsRealtime(0.25f);
			}
		}
	}
}
