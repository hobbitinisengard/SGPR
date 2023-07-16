using UnityEngine.UI;
using UnityEngine;
using RVP;

public class StuntInfoOverlay : MonoBehaviour
{
    static float animationTime = .5f;
    GameObject stuntObj;
    Text stuntObjText;
    Text postfixObjText;
    GameObject postfixObj;
    RectTransform rt;
    float postfixObjAnimStartTime;
    float stuntObjAnimStartTime;
    /// <summary>
    /// How many times has this stunt been done without interruptions by other stunts
    /// </summary>
    int localDoneTimes = 0;
    int originalPostfixFontSize;
    private void Initialize()
    {
        postfixObj = transform.GetChild(0).gameObject;
        stuntObj = transform.GetChild(0).GetChild(0).gameObject;

        postfixObjText = postfixObj.GetComponent<Text>();
        stuntObjText = stuntObj.GetComponent<Text>();
        originalPostfixFontSize = postfixObjText.fontSize;

        rt = postfixObj.GetComponent<RectTransform>();
    }

    void Update()
    {
        if(stuntObj)
        {
            float animTime = Time.time - stuntObjAnimStartTime;
            if (animTime < animationTime)
            {
                var color = stuntObjText.color;
                color.a = animTime / animationTime;
                stuntObjText.color = color;
                postfixObjText.color = color;

                Vector2 offsetMin = rt.offsetMin;
                float newBottomValue = -100 * (animationTime - animTime);
                offsetMin.y = newBottomValue;
                rt.offsetMin = offsetMin;
            }
            animTime = Time.time - postfixObjAnimStartTime;
            if (animTime < animationTime)
            {
                postfixObjText.fontSize = (int)((2 - 2 * animTime) * originalPostfixFontSize);
            }
        }
    }
    public void UpdatePostfix(in Stunt stunt)
    {
        postfixObjText.text = (++localDoneTimes * 360).ToString();
        postfixObjAnimStartTime = Time.time;
    }
    public void WriteStuntName(in Stunt stunt)
    {
        Initialize();
        stuntObjAnimStartTime = Time.time;
        stuntObjText.text = stunt.name;
        name = stunt.name;
        postfixObjText.text = (++localDoneTimes * 360).ToString();
    }
    public void DimTexts(float opaqueness)
    {
        var c = postfixObjText.color;
        c.a = opaqueness;
        postfixObjText.color = c;

        c = stuntObjText.color;
        c.a = opaqueness;
        stuntObjText.color = c;
    }
}
