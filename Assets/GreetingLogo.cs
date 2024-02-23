using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

public class GreetingLogo : Sfxable
{
	public Color reddish;
	public AnimationCurve jumpingCurve;
	public TextMeshProUGUI bottomText;
	public float timer;
	public float timer2;
	public float timer3;
	public int jumps = 0;
	public Button startButton;
	public RectTransform blitz;
	public GameObject nextMenu;
	public InputActionReference submitRef;
	private bool goingUpSeq;

	Vector2 outMoveInitPos;
	Vector2 outMoveTargetPos = new Vector2(0, Screen.height);
	RectTransform rt;
	private bool toDemo;
	private int lastTryCo;
	MainMenuView view;
	void Start()
	{
		rt = GetComponent<RectTransform>();
		transform.GetChild(0).GetComponent<RectTransform>().localPosition = 
			new Vector2(Screen.width, 0);
		view = transform.parent.GetComponent<MainMenuView>();
		Cursor.visible = false;
	}

	private void SubmitPressed(InputAction.CallbackContext obj)
	{
		PlaySFX("fe-dialogconfirm");
		goingUpSeq = true;
		toDemo = false;
		outMoveInitPos = rt.localPosition;
		timer = 0;
	}
	private void OnDisable()
	{
		submitRef.action.performed -= SubmitPressed;
	}
	private void OnEnable()
	{
		submitRef.action.performed += SubmitPressed;
		timer = 0;
		timer2 = 0;
		timer3 = 0;
		jumps = 0;
		lastTryCo = 0;
		goingUpSeq = false;
		startButton.Select();
	}

	void Update()
	{
		if (goingUpSeq)
		{
			if (timer >= 1)
			{
				if (toDemo)
				{
					int randomIdx = Mathf.RoundToInt((Info.tracks.Count - 1) * UnityEngine.Random.value);
					int i = 0;
					bool cantFind = false;
					foreach (var track in Info.tracks)
					{
						if ((cantFind && track.Key.Length > 3) || randomIdx == i)
						{
							if (track.Key.Length > 3 && track.Value.valid && track.Value.unlocked)
							{
								Info.s_spectator = true;
								Info.s_rivals = 5;
								Info.s_inEditor = false;
								Info.s_cpuLevel = CpuLevel.Elite;
								Info.s_trackName = track.Key;
								Info.s_isNight = UnityEngine.Random.value > 0.5f;
								Info.s_laps = 9;
								break;
							}
							else
							{
								cantFind = true;
							}
						}
						++i;
					}
					if(cantFind)
						view.GoToView(nextMenu);
					else
					{
						view.ToRaceScene();
						toDemo = false;
						goingUpSeq = false;
					}
				}
				//else
				//	view.GoToView(nextMenu);
			}
			rt.localPosition = Vector2.Lerp(outMoveInitPos, outMoveTargetPos, F.curve.Evaluate(timer));
			if (timer < 1)
				timer += 4 * Time.deltaTime;
			return;
		}
		else
		{
			Vector2 pos = rt.localPosition;
			if (timer < 1)
			{ // down move
				pos.y = -50 + Screen.height * F.curve2.Evaluate(timer);
			}
			else if (timer > 8)
			{ // jumping
				pos.y = -50 + 100 * jumpingCurve.Evaluate(timer2);
				timer2 += Time.deltaTime;
				if (timer2 > 0.5f)
				{
					jumps++;
					bottomText.color = (jumps % 2 == 0) ? Color.white : reddish;
					timer2 -= .5f;
				}
			}

			if (jumps > 0)
			{
				if (jumps % 8 == 7 && jumps > lastTryCo)
				{
					lastTryCo = jumps;
					StartCoroutine(BlitzMove());
				}
				else if (jumps % 8 == 0)
				{
					pos.x = -Screen.width * F.curve.Evaluate(timer3);
					timer3 += 2 * Time.deltaTime;
				}
				else if (jumps % 8 == 1 && jumps > lastTryCo)
				{
					lastTryCo = jumps;
					pos.x = 0;
					timer3 = 0;
				}
			}

			timer += Time.deltaTime;
			rt.localPosition = pos;
			if (jumps == 8)//80 or 8
			{ // up move
				goingUpSeq = true;
				toDemo = true;
				outMoveInitPos = rt.localPosition;
				timer = 0;
			}
		}
	}
	IEnumerator BlitzMove()
	{
		float bTimer = 0;

		while (true)
		{
			var pos = blitz.anchoredPosition;
			pos.y = Mathf.Lerp(rt.rect.height / 1.8f, -rt.rect.height / 1.8f, F.curve.Evaluate(bTimer));
			blitz.anchoredPosition = pos;
			bTimer += Time.deltaTime;
			if (bTimer >= 1)
			{
				yield break;
			}
			yield return null;
		}
	}
}
