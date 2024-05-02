using System.Collections.Generic;
using UnityEngine;

public class Debug2Mono : MonoBehaviour
{
    public class Debug2String
    {
        public Vector3 pos;
        public string text;
        public Color? color;
        public float eraseTime;
    }

    public List<Debug2String> Strings = new List<Debug2String>();

    public void OnDrawGizmos()
    {
        foreach (var stringpair in Strings)
        {
            GUIStyle style = new GUIStyle();
            Color color = stringpair.color.HasValue ? stringpair.color.Value : Color.green;
            style.normal.textColor = color;

#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(stringpair.pos, stringpair.text, style);
#endif
        }
    }

    private static Debug2Mono m_instance;
    public static Debug2Mono instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = Object.FindFirstObjectByType<Debug2Mono>();
                if (m_instance == null)
                {
                    var go = new GameObject("DeleteMeLater");
                    m_instance = go.AddComponent<Debug2Mono>();
                }
            }
            return m_instance;
        }
    }

    public static void DrawText(Vector3 pos, string text, float duration = 5, Color? color = null)
    {
        instance.Strings.Add(new Debug2Mono.Debug2String() { text = text, color = color, pos = pos, eraseTime = Time.time + duration, });

        List<Debug2Mono.Debug2String> toBeRemoved = new List<Debug2Mono.Debug2String>();

        foreach (var item in instance.Strings)
        {
            if (item.eraseTime <= Time.time)
                toBeRemoved.Add(item);
        }

        foreach (var rem in toBeRemoved)
            instance.Strings.Remove(rem);
    }
}