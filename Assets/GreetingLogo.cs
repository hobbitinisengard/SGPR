using UnityEngine;

public class GreetingLogo : MonoBehaviour
{
    // 1 -> 0
    AnimationCurve curve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    float animStartTime;
    RectTransform rt;
    // Start is called before the first frame update
    void Start()
    {
        animStartTime = Time.time;
        rt = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 pos = rt.anchoredPosition;
        if(Time.time - animStartTime < 1)
        {
            pos.y = Screen.height * curve.Evaluate(Time.time - animStartTime);
        }
        rt.anchoredPosition = pos;
    }
}
