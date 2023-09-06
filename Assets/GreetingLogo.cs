using UnityEngine.UI;
using UnityEngine;
using System.Collections;
using UnityEditor.Experimental.GraphView;

public class GreetingLogo : MonoBehaviour
{
	AnimationCurve curve = AnimationCurve.EaseInOut(0, 1, 1, 0);
	AnimationCurve curve2 = AnimationCurve.EaseInOut(0, 0, 1, 1);

	public Color reddish;
	public AnimationCurve jumpingCurve;
	public Text bottomText;
	public float timer;
	public float timer2;
	public float timer3;
	public int jumps = 0;
	public Button startButton;
	public RectTransform blitz;
	private bool goingUpSeq;

	Vector2 outMoveInitPos;
	Vector2 outMoveTargetPos = new Vector2(0,Screen.height);
	RectTransform rt;
	private bool toDemo;
	private int lastTryCo;

	// Start is called before the first frame update
	void Start()
	{
		rt = GetComponent<RectTransform>();
		transform.GetChild(0).GetComponent<RectTransform>().localPosition = new Vector2(Screen.width, 0);
	}
	private void OnEnable()
	{
		timer = 0;
		timer2 = 0;
		timer3 = 0;
		jumps = 0;
		lastTryCo = 0;
		goingUpSeq = false;
		startButton.Select();
	}
	// Update is called once per frame
	void Update()
	{
		if (goingUpSeq)
		{
			if (timer == 1)
			{
				if (toDemo)
					Debug.Log("Play Demo");
				else
					Debug.Log("To Menu");
			}
			rt.localPosition = Vector2.Lerp(outMoveInitPos, outMoveTargetPos, curve2.Evaluate(timer));
			timer += 4*Time.deltaTime;
			return;
		}
		if (Input.GetKeyDown(KeyCode.Return))
		{
			goingUpSeq = true;
			toDemo = false;
			outMoveInitPos = rt.localPosition;
			timer = 0;
			return;
		}

		Vector2 pos = rt.localPosition;
		if (timer < 1)
		{ // down move
			pos.y = -50 + Screen.height * curve.Evaluate(timer);
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
			if(jumps % 8 == 7 && jumps > lastTryCo)
			{
				lastTryCo = jumps;
				StartCoroutine(BlitzMove());
			}
			else if(jumps % 8 == 0)
			{
				pos.x = -Screen.width * curve2.Evaluate(timer3);
				timer3 += 2 * Time.deltaTime;
			}
			else if(jumps % 8 == 1 && jumps > lastTryCo)
			{
				lastTryCo = jumps;
				pos.x = 0;
				timer3 = 0;
			}
		} 
		
		timer += Time.deltaTime;
		rt.localPosition = pos;
		if (jumps == 80)
		{ // up move
			goingUpSeq = true;
			toDemo = false;
			outMoveInitPos = rt.localPosition;
			timer = 0;
		}
	}
	IEnumerator BlitzMove()
	{
		float bTimer = 0;
		
		while(true)
		{
			var pos = blitz.anchoredPosition;
			pos.y = Mathf.Lerp(rt.rect.height / 1.8f, -rt.rect.height / 1.8f, curve2.Evaluate(bTimer));
			blitz.anchoredPosition = pos;
			bTimer += Time.deltaTime;
			if(bTimer >= 1)
			{
				yield break;
			}
			yield return null;
		}
	}
}
