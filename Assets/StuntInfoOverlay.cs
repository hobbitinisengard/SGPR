using UnityEngine.UI;
using UnityEngine;

public class StuntInfoOverlay : MonoBehaviour
{
    GameObject stuntObj;
    Text stuntObjText;
    Text prefixObjText;
    GameObject prefixObj;
    float prefixObjAnimStartTime;
    float stuntObjAnimStartTime;
    void Start()
    {
        stuntObj = transform.GetChild(0).gameObject;
        stuntObjText = stuntObj.GetComponent<Text>();
        prefixObjText = prefixObj.GetComponent<Text>();
        prefixObj = transform.GetChild(0).GetChild(0).gameObject;
        stuntObjAnimStartTime = Time.time;
    }

    void Update()
    {
        float animTime = Time.time - stuntObjAnimStartTime;
        if (animTime < 1)
        {
            var color = stuntObjText.color;
            color.a = 255 * animTime;
            stuntObjText.color = color;
        }
        animTime = Time.time - prefixObjAnimStartTime;
        if(animTime < .5f)
        {
            prefixObj.transform.localScale = (2-2*animTime)*Vector3.one;
        }
    }
    public void NewPrefix(string str, bool animate)
    {
        prefixObj.SetActive(true);
        prefixObjText.text = str;
        if(animate)
            prefixObjAnimStartTime = Time.time;
    }
    public void NewStunt(string str)
    {
        stuntObjText.text = str;
    }
}
