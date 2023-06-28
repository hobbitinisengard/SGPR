using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Roller : MonoBehaviour
{
    GameObject obj;
    RectTransform rt;
    float height;
    public float target = 0;
    public float current = 0;
    float coeff = 10f;
    bool clockWork = true;
    float pos0;
    float scale;
    // Start is called before the first frame update
    void Start()
    {
        obj = GetComponent<Transform>().gameObject;
        rt = obj.GetComponent<RectTransform>();
        height = rt.sizeDelta.y;
        pos0 = rt.anchoredPosition.y;
        scale = rt.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        if(!clockWork)
        {
            current = target;
        }
        else if(Mathf.Abs(current - target) > Mathf.Epsilon) // current != target
        {
            if(current >= 0.99f && target > 0 && target < 1f)
            {
                current = 0;
            }
            float step = Time.deltaTime * coeff;
            current = Mathf.Lerp(current, target > current ? target : 1, step);
        }
        Vector3 pos = rt.anchoredPosition;
        pos.y = Mathf.Lerp(pos0, scale*height - pos0, current);
        rt.anchoredPosition = pos;
    }
    public void SetActive(bool value)
    {
        obj.SetActive(value);
    }
    /// <summary>
    /// Sets roller to targetValue with centering effect
    /// </summary>
    public void SetValue(float newTarget)
    {
        newTarget = Mathf.Clamp(newTarget, 0, 9);
        this.clockWork = true;

        if (!obj.activeSelf)
            obj.SetActive(true);

        newTarget /= 10f;
        if(Mathf.Abs(newTarget - target) > Mathf.Epsilon)
        {
            target = newTarget;
        }
    }
    /// <summary>
    /// Sets roller to fraction without centering effect
    /// </summary>
    public void SetFrac(float frac)
    {
        this.clockWork = false;

        if (!obj.activeSelf)
            obj.SetActive(true);

        target = frac;
    }
}
