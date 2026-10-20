using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Each control tracks its own fingers so steering, throttle and tricks can be held together.
public sealed class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IInitializePotentialDragHandler, IDragHandler
{
    public Action<bool> heldChanged;
    public Action tapped;
    // Throttle releases when the finger slides off, without waiting for finger-up.
    public bool releaseOnExit;
    // Race steering reads all screen touches, independently of UI pointer capture.
    public bool readScreenTouches;
    bool screenHeld, editingLayout;
    int? dragPointer;
    Vector2 lastDragPoint;
    RectTransform touchRect;
    bool UsesScreenTouches => readScreenTouches && MobileControls.Enabled;
    public bool highlightWhenHeld;
    public Color highlightColor = new Color(.9f, .65f, .1f, .85f);
    readonly HashSet<int> fingers = new();
    void Awake() => touchRect = (RectTransform)transform;
    void OnEnable() => InputSystem.onAfterUpdate += ReadScreenTouches;
    void ReadScreenTouches()
    {
        if (editingLayout || !UsesScreenTouches) return;
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
        if (editingLayout)
        {
            if (!dragPointer.HasValue && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)touchRect.parent, data.position, data.pressEventCamera, out lastDragPoint))
                dragPointer = data.pointerId;
            return;
        }
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
        if (editingLayout)
        {
            if (dragPointer == data.pointerId) dragPointer = null;
            return;
        }
        if (UsesScreenTouches) return;
        if (fingers.Remove(data.pointerId) && fingers.Count == 0)
        {
            if (highlightWhenHeld) SetHighlighted(false);
            heldChanged?.Invoke(false);
        }
    }
    public void OnPointerExit(PointerEventData data)
    {
        if (!editingLayout && releaseOnExit) OnPointerUp(data);
    }
    public void SetLayoutEditing(bool value)
    {
        editingLayout = value;
        dragPointer = null;
        screenHeld = false;
        fingers.Clear();
        SetHighlighted(false);
        heldChanged?.Invoke(false);
    }
    public void OnInitializePotentialDrag(PointerEventData data)
    {
        if (editingLayout) data.useDragThreshold = false;
    }
    public void OnDrag(PointerEventData data)
    {
        if (!editingLayout || dragPointer != data.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)touchRect.parent, data.position, data.pressEventCamera, out var point)) return;
        touchRect.anchoredPosition += point - lastDragPoint;
        lastDragPoint = point;
        ClampToParent();
    }
    public void ClampToParent()
    {
        var parent = ((RectTransform)touchRect.parent).rect;
        Vector2 position = touchRect.localPosition;
        Vector2 halfSize = touchRect.rect.size * .5f;
        position.x = Mathf.Clamp(position.x, parent.xMin + halfSize.x, parent.xMax - halfSize.x);
        position.y = Mathf.Clamp(position.y, parent.yMin + halfSize.y, parent.yMax - halfSize.y);
        touchRect.anchoredPosition += position - (Vector2)touchRect.localPosition;
    }
    void OnDisable()
    {
        InputSystem.onAfterUpdate -= ReadScreenTouches;
        screenHeld = false;
        dragPointer = null;
        fingers.Clear();
        if (highlightWhenHeld) SetHighlighted(false);
        heldChanged?.Invoke(false);
    }
    public void SetHighlighted(bool value)
    {
        GetComponent<Image>().color = value ? highlightColor : new Color(.08f, .09f, .12f, .65f);
    }
}
