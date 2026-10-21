using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Message
{
	public string text = "";
	public BottomInfoType type;
	public Message(string content, BottomInfoType type)
	{
		text = content;
		this.type = type;
	}
	public Message()
	{
	}
}
public class InfoText : MonoBehaviour
{
	RectTransform infoText_rt;
	Text infoText;

	Queue<Message> liveMessages = new();
	Message curMsgInQueue = new();
	
	const float msgHiddenPos = 0;
	const float msgVisiblePos = 80;
	public float newPosY = 0;
	// 0 = hidden text, 1 = visible text, x - time of animation
	AnimationCurve bottomTextAnim;
	float timer = 0;
	bool showing = false;
	Coroutine showingCo;
    void EnsureInitialized()
    {
        // HUD.Connect may reset this component while its GameObject is still
        // inactive and Awake has not run (for example, editor free driving).
        if (!infoText_rt) infoText_rt = GetComponent<RectTransform>();
        if (!infoText) infoText = GetComponent<Text>();
        if (bottomTextAnim == null)
            bottomTextAnim = new AnimationCurve(new Keyframe[] {
                new (0,0),
                new (.5f,1),
                new (2.5f,1),
                new (3,0),
            });
    }
    private void Awake()
    {
        EnsureInitialized();
        SetBottomTextPos(msgHiddenPos);
        gameObject.SetActive(true);
    }
	private void OnEnable()
	{
		Reset();
	}
	public void Reset()
	{
        EnsureInitialized();
		showing = false;
		if(showingCo != null)
			StopCoroutine(showingCo);
        showingCo = null;
		liveMessages.Clear();
		curMsgInQueue = null;
		newPosY = msgHiddenPos;
		SetBottomTextPos(newPosY);
	}
	IEnumerator BottomTextShowing()
	{
		showing = true;
		while (liveMessages.Count > 0)
		{
			curMsgInQueue = liveMessages.Dequeue();
			timer = 0;

			while(timer <= bottomTextAnim.Duration())
			{
				infoText.text = curMsgInQueue.text;
				newPosY = Mathf.Lerp(msgHiddenPos, msgVisiblePos, bottomTextAnim.Evaluate(timer));
				SetBottomTextPos(newPosY);
				infoText.color = timer % 1f > 0.5f ? F.I.yellow : F.I.red;

				timer += Time.deltaTime;
				yield return null;
			}
			curMsgInQueue = null;
		}
		SetBottomTextPos(msgHiddenPos);
		showing = false;
	}
	
	public void AddMessage(Message message)
	{
        EnsureInitialized();
		if (curMsgInQueue != null && message.type == curMsgInQueue.type)
		{ // if already displaying message of the same type -> immediately switch to this message
			curMsgInQueue = message;
			infoText.text = curMsgInQueue.text;
			timer = 0;
		}
		else
		{
			bool found = false;
			foreach (var livemsg in liveMessages)
			{
				if (message.type == livemsg.type)
				{ // found message of the same type in queue -> just update text
					livemsg.text = message.text;
					found = true;
					break;
				}
			}
			if (!found) // haven't found this type -> add new msg to queue
				liveMessages.Enqueue(message);
		}
		if (!showing && gameObject.activeInHierarchy)
		{
			showingCo = StartCoroutine(BottomTextShowing());
		}
	}
	public void SetBottomTextPos(float posy)
	{
        EnsureInitialized();
		Vector2 position = infoText_rt.anchoredPosition;
		position.y = posy;
		infoText_rt.anchoredPosition = position;
	}
}
