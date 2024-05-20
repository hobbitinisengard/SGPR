using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValue : MonoBehaviour
{
   public TextMeshProUGUI text;
   public Slider slider;
   public void SetText(float value)
   {
      text.text = value.ToString("F1");
   }
}
