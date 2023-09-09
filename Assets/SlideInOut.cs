using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class SlideInOut : MonoBehaviour
{
	enum Type { Image, Button };
	Type type;
	public enum Dir { In = 1, Out = -1 };
	Dir dir = Dir.In;
	bool canAnimate = false;

	public float timer { get; private set; }
	public Vector2 inSlideDirection = Vector2.right;
	float targetPos;
	float startPos;
	float mult = 1;
	bool disableAfterEndOfAnim = false;
	RectTransform rt;
	Image img;
	SlideInOut nextNode;

	void Awake()
	{
		timer = 0;
		if (GetComponent<Button>())
		{
			type = Type.Button;
			mult = 4;
			img = transform.GetChild(1).GetComponent<Image>();
			nextNode = NextNode();
		}
		else
		{
			type = Type.Image;
			mult = 2;
			img = GetComponent<Image>();
		}
		var c = img.color;
		c.a = 0;
		img.color = c;

		rt = GetComponent<RectTransform>();
		targetPos = Get();

		if (inSlideDirection.x != 0)
			startPos = -inSlideDirection.x * (Screen.width / 2f + rt.rect.width/2f);
		else
			startPos = -inSlideDirection.y * (Screen.height / 2f + rt.rect.height/2f);
		PlaySlideIn();
	}
	private SlideInOut NextNode()
	{
		int idx = transform.GetSiblingIndex()+1;
		Transform child=null;
		while(idx < transform.parent.childCount-1)
		{
			child = transform.parent.GetChild(idx);
			if (child.gameObject.activeSelf)
				break;
			idx++;
		}
		if (!child)
			return null;
		return child.GetComponent<SlideInOut>();
	}
	public void PlaySlideIn() => PlaySlide(Dir.In);
	public void PlaySlideOut(bool disableAfterEndOfAnim = false)
	{
		this.disableAfterEndOfAnim = disableAfterEndOfAnim;
		PlaySlide(Dir.Out);
	}
	void PlaySlide(Dir dir)
	{
		this.dir = dir;
		if (type == Type.Image)
		{
			canAnimate = true;
			//timer = ((int)dir > 0) ? 0 : 1;
		}
		else
		{
			if(!nextNode)
			{
				//timer = ((int)dir > 0) ? 0 : 1;
			}
		}
		StartCoroutine(Play());
		
	}
	private void OnDisable()
	{
		canAnimate = false;
		timer = 0;
	}
	void OnEnable()
	{
		if(type == Type.Button)
			nextNode = NextNode();
		PlaySlideIn();
	}
	IEnumerator Play()
	{
		for (int i=0; i<100000; ++i)
		{
			if (canAnimate)
			{
				timer += (int)dir * mult * Time.deltaTime;

				float step = ((int)dir > 0) ? Easing.OutCubic(timer) : Easing.InCubic(timer);
				Set(Mathf.Lerp(startPos, targetPos, step));

				var c = img.color;
				//On simple images img var is an image. On buttons img is a white texture
				c.a = (type == Type.Image) ? step :1 - step;
				img.color = c;

				if ((dir < 0 && timer < 0) || (dir > 0 && timer > 1))
				{
					canAnimate = false;
					if (dir == Dir.Out && disableAfterEndOfAnim)
					{
						gameObject.SetActive(false);
						disableAfterEndOfAnim = false;
					}
					yield break;
				}
			}
			else if (type == Type.Button)
			{
				if (!nextNode)
				{
					canAnimate = true;
					//timer = ((int)dir > 0) ? 0 : 1;
				}
				else
				{
					if (((int)dir < 0 && nextNode.timer < timer) || ((int)dir > 0 && nextNode.timer > timer))
					{
						//timer = ((int)dir > 0) ? 0 : 1;
						canAnimate = true;
					}
				}
			}
			yield return null;
		}
		Debug.LogError("nope");
		yield break;
	}
	float Get()
	{
		if (inSlideDirection.x != 0)
			return rt.anchoredPosition.x;
		else
			return rt.anchoredPosition.y;
	}
	void Set(float value)
	{
		if (inSlideDirection.x != 0)
			rt.anchoredPosition = new Vector2(value, rt.anchoredPosition.y);
		else
			rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, value);
	}
}

