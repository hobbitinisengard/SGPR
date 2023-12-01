using RVP;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class SlideInOut : MonoBehaviour
{
	enum Type { Image, Button, ButtonContainer };
	Type type;
	public enum Dir { In = 1, Out = -1 };
	Dir dir = Dir.In;
	bool canAnimate = false;
	public float initialOwnDelay = 0;
	float ownDelay;
	public bool ForceNextNode = false;
	public bool EasingUnchanged = false;
	public float timer { get; private set; }
	public Vector2 inSlideDirection = Vector2.right;
	float endPos;
	float startPos;
	public float mult = 1;
	bool disableAfterEndOfAnim = false;
	RectTransform rt;
	Image[] imgs;
	SlideInOut nextNode;
	float initEndPos;
	static Camera mainCamera;
	float GetPos()
	{
		if (inSlideDirection.x != 0)
			return rt.anchoredPosition.x;
		else
			return rt.anchoredPosition.y;
	}
	void SetPos(float value)
	{
		if (inSlideDirection.x != 0)
			rt.anchoredPosition = new Vector2(value, rt.anchoredPosition.y);
		else
			rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, value);
	}
	void Awake()
	{
		if(mainCamera == null)
		{
			mainCamera = F.GetTopmostParentComponent<Canvas>(transform).worldCamera;
		}
		rt = GetComponent<RectTransform>();

		if (GetComponent<Button>())
		{
			type = Type.Button;
			if(mult == 1)
				mult = 4;
			imgs = new Image[] { transform.GetChild(1).GetComponent<Image>() };
			nextNode = NextNode();
		}
		else if(GetComponent<Image>())
		{
			type = Type.Image;
			if (mult == 1)
				mult = 2;
			imgs = new Image[] { GetComponent<Image>() };
		}
		else
		{
			type = Type.ButtonContainer;
			if (mult == 1)
				mult = 4;
			imgs = new Image[transform.childCount];
			for (int i = 0; i < transform.childCount; ++i)
				imgs[i] = transform.GetChild(i).GetChild(1).GetComponent<Image>();
		}
		SetContentsTransp(0);
		
		initEndPos = GetPos();
		endPos = initEndPos;

		//var screenPos = mainCamera.WorldToScreenPoint(rt.TransformPoint(rt.anchoredPosition3D));

		if (inSlideDirection.x != 0)
		{
			if (inSlideDirection.x > 0)
				startPos = -2*rt.rect.width;//-(screenPos.x+rt.rect.width);
			else
				startPos = 2*rt.rect.width; //Screen.width-screenPos.x + rt.rect.width;
		}
		else
		{
			if (inSlideDirection.y > 0)
				startPos = -rt.rect.height;
			else
				startPos = rt.rect.height;
		}
	}
	void SetContentsTransp(float a)
	{
		if(type== Type.ButtonContainer || type == Type.Button)
		{
			for (int i = 0; i < imgs.Length; ++i)
			{
				var c = imgs[i].color;
				c.a = a;
				imgs[i].color = c;
			}
		}
		else
		{
			var allImages = transform.GetComponentsInChildren<Image>();
			var allTexts = transform.GetComponentsInChildren<TextMeshProUGUI>();
			foreach(var child in allImages)
			{
				var c = child.color;
				c.a = a;
				child.color = c;
			}
			foreach (var child in allTexts)
			{
				var c = child.color;
				c.a = a;
				child.color = c;
			}
		}
	}
	private SlideInOut NextNode()
	{
		int idx = transform.GetSiblingIndex()+1;
		Transform nnode=null;
		while(idx < transform.parent.childCount-1)
		{
			if (transform.parent.GetChild(idx).gameObject.activeSelf)
			{
				nnode = transform.parent.GetChild(idx);
				break;
			}
			idx++;
		}
		if (!nnode)
			return null;
		return nnode.GetComponent<SlideInOut>();
	}
	public void PlaySlideIn()
	{
		timer = 0;
		endPos = initEndPos;
		PlaySlide(Dir.In);
	}
	public void PlaySlideOut(bool disableAfterEndOfAnim = false)
	{
		endPos = GetPos();
		this.disableAfterEndOfAnim = disableAfterEndOfAnim;
		PlaySlide(Dir.Out);
	}
	void PlaySlide(Dir dir)
	{
		this.dir = dir;

		if (ForceNextNode || type == Type.Button || type == Type.ButtonContainer)
			nextNode = NextNode();
		else if (type == Type.Image)
			canAnimate = true;

		StartCoroutine(Play());
	}
	private void OnDisable()
	{
		canAnimate = false;
		ownDelay = initialOwnDelay;
	}
	void OnEnable()
	{
		rt = GetComponent<RectTransform>();
		PlaySlideIn();
	}
	IEnumerator Play()
	{

		while(true)
		{
			if (canAnimate)
			{
				timer += (int)dir * mult * Time.deltaTime;

				float step;
				if(EasingUnchanged)
					step = Easing.OutCubic(timer);
				else
					step = ((int)dir > 0) ? Easing.OutCubic(timer) : Easing.InCubic(timer);

				SetPos(Mathf.Lerp(startPos, endPos, step));

				//On simple images img var is an image. On buttons img is a white texture
				SetContentsTransp((type == Type.Image) ? step :1 - step);

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
			else
			{
				if (!nextNode)
				{
					canAnimate = true;
				}
				else
				{
					if (((int)dir < 0 && nextNode.timer < timer) || ((int)dir > 0 && nextNode.timer > timer))
					{
						if(ownDelay>0)
						{
							ownDelay -= Time.deltaTime;
						}
						else
						{
							canAnimate = true;
							ownDelay = initialOwnDelay;
						}
					}
				}
			}
			yield return null;
		}
	}
	
}

