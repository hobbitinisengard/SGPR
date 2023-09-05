using UnityEngine;

public class Dyndak : MonoBehaviour
{
    float posy;
    RectTransform rt;
    float begintime = 0;
    // Start is called before the first frame update
    void Start()
    {
        rt = GetComponent<RectTransform>();
        posy = rt.anchoredPosition.y;
        begintime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = rt.anchoredPosition;
        pos.y = posy + 2*40 * Mathf.Abs(Mathf.Sin((Time.time - begintime) * 0.8f * Mathf.PI));
        rt.anchoredPosition = pos;
    }
}
