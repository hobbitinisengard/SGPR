using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Add touch behavior to existing selector images without creating extra menu buttons.
public sealed class MobileTapTarget : MonoBehaviour, IPointerClickHandler
{
    Action clicked;
    public static void Bind(GameObject target, Action action)
    {
        if (!MobileControls.Enabled) return;
        var handler = target.GetComponent<MobileTapTarget>() ?? target.AddComponent<MobileTapTarget>();
        handler.clicked = action;
        var graphic = target.GetComponent<Graphic>();
        if (graphic) graphic.raycastTarget = true;
    }
    public void OnPointerClick(PointerEventData data)
    {
        if (!data.dragging) clicked?.Invoke();
    }
}
