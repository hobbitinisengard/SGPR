using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Each control tracks its own fingers so steering, throttle and tricks can be held together.
public sealed class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Action<bool> heldChanged;
    public Action tapped;
    // Throttle releases when the finger slides off, without waiting for finger-up.
    public bool releaseOnExit;
    // Race steering reads all screen touches, independently of UI pointer capture.
    public bool readScreenTouches;
    bool screenHeld;
    RectTransform touchRect;
    bool UsesScreenTouches => readScreenTouches && MobileControls.Enabled;
    public bool highlightWhenHeld;
    public Color highlightColor = new Color(.9f, .65f, .1f, .85f);
    readonly HashSet<int> fingers = new();
    void Awake() => touchRect = (RectTransform)transform;
    void OnEnable() => InputSystem.onAfterUpdate += ReadScreenTouches;
    void ReadScreenTouches()
    {
        if (!UsesScreenTouches) return;
        bool pressed = false;
        var screen = Touchscreen.current;
        if (Application.isFocused && screen != null)
            foreach (var touch in screen.touches)
                if (touch.press.isPressed &&
                    RectTransformUtility.RectangleContainsScreenPoint(touchRect, touch.position.ReadValue(), null))
                {
                    pressed = true;
                    break;
                }
        if (pressed == screenHeld) return;
        screenHeld = pressed;
        if (highlightWhenHeld) SetHighlighted(pressed);
        heldChanged?.Invoke(pressed);
    }
    public void OnPointerDown(PointerEventData data)
    {
        if (UsesScreenTouches) return;
        if (!fingers.Add(data.pointerId)) return;
        if (fingers.Count == 1)
        {
            if (highlightWhenHeld) SetHighlighted(true);
            heldChanged?.Invoke(true);
            tapped?.Invoke();
        }
    }
    public void OnPointerUp(PointerEventData data)
    {
        if (UsesScreenTouches) return;
        if (fingers.Remove(data.pointerId) && fingers.Count == 0)
        {
            if (highlightWhenHeld) SetHighlighted(false);
            heldChanged?.Invoke(false);
        }
    }
    public void OnPointerExit(PointerEventData data)
    {
        if (releaseOnExit) OnPointerUp(data);
    }
    void OnDisable()
    {
        InputSystem.onAfterUpdate -= ReadScreenTouches;
        screenHeld = false;
        fingers.Clear();
        if (highlightWhenHeld) SetHighlighted(false);
        heldChanged?.Invoke(false);
    }
    public void SetHighlighted(bool value)
    {
        GetComponent<Image>().color = value ? highlightColor : new Color(.08f, .09f, .12f, .65f);
    }
}
