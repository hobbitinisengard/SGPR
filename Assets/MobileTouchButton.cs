using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Each control tracks its own fingers so steering, throttle and tricks can be held together.
public sealed class MobileTouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IInitializePotentialDragHandler, IDragHandler, ICanvasRaycastFilter
{
    public Action<bool> heldChanged;
    public Action tapped;
    // Relative horizontal stick: the initial press is neutral, not the control's centre.
    public bool horizontalStick;
    public Action<float> axisChanged;
    int? stickPointer;
    Vector2 stickOrigin;
    Text stickIndicator;
    public Action<MobileTouchButton> layoutSelected;
    public bool Circular { get; private set; }
    static Sprite circleSprite;
    bool selectedForLayout;
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
                    ContainsScreenPoint(touch.position.ReadValue(), null))
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
            layoutSelected?.Invoke(this);
            if (!dragPointer.HasValue && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)touchRect.parent, data.position, data.pressEventCamera, out lastDragPoint))
                dragPointer = data.pointerId;
            return;
        }
        if (horizontalStick)
        {
            if (stickPointer.HasValue || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    touchRect, data.position, data.pressEventCamera, out stickOrigin)) return;
            stickPointer = data.pointerId;
            stickIndicator = GetComponentInChildren<Text>();
            UpdateStick(data);
            if (highlightWhenHeld) SetHighlighted(true);
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
        if (horizontalStick)
        {
            if (stickPointer == data.pointerId) ReleaseStick();
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
        ReleaseStick();
        editingLayout = value;
        selectedForLayout = false;
        dragPointer = null;
        screenHeld = false;
        fingers.Clear();
        SetHighlighted(false);
        heldChanged?.Invoke(false);
    }
    public void OnInitializePotentialDrag(PointerEventData data)
    {
        if (editingLayout || horizontalStick) data.useDragThreshold = false;
    }
    public void OnDrag(PointerEventData data)
    {
        if (!editingLayout && horizontalStick)
        {
            if (stickPointer == data.pointerId) UpdateStick(data);
            return;
        }
        if (!editingLayout || dragPointer != data.pointerId) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)touchRect.parent, data.position, data.pressEventCamera, out var point)) return;
        touchRect.anchoredPosition += point - lastDragPoint;
        lastDragPoint = point;
        ClampToParent();
    }
    void UpdateStick(PointerEventData data)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                touchRect, data.position, data.pressEventCamera, out var point)) return;
        float radius = Mathf.Max(1, Mathf.Min(touchRect.rect.width, touchRect.rect.height) * .5f);
        float displacement = Mathf.Clamp((point.x - stickOrigin.x) / radius, -1, 1);
        const float deadZone = .06f;
        float axis = Mathf.Sign(displacement) * Mathf.Clamp01((Mathf.Abs(displacement) - deadZone) / (1 - deadZone));
        axisChanged?.Invoke(axis);
        if (stickIndicator)
        {
            stickIndicator.text = "\u25CF";
            stickIndicator.rectTransform.anchoredPosition = stickOrigin - touchRect.rect.center + Vector2.right * (displacement * radius);
        }
    }
    public void ReleaseStick()
    {
        stickPointer = null;
        if (!horizontalStick) return;
        axisChanged?.Invoke(0);
        if (stickIndicator)
        {
            stickIndicator.text = "STEER";
            stickIndicator.rectTransform.anchoredPosition = Vector2.zero;
        }
        if (highlightWhenHeld) SetHighlighted(false);
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
        ReleaseStick();
        InputSystem.onAfterUpdate -= ReadScreenTouches;
        screenHeld = false;
        dragPointer = null;
        fingers.Clear();
        if (highlightWhenHeld) SetHighlighted(false);
        heldChanged?.Invoke(false);
    }
    public void SetLayoutSelected(bool value)
    {
        selectedForLayout = value;
        SetHighlighted(false);
    }
    public void SetCircular(bool value)
    {
        Circular = value;
        if (value && !circleSprite)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Touch control circle";
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * (size * .5f));
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(size * .5f - distance));
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
        }
        var image = GetComponent<Image>();
        image.sprite = value ? circleSprite : null;
        image.type = Image.Type.Simple;
    }
    bool ContainsScreenPoint(Vector2 point, Camera eventCamera)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(touchRect, point, eventCamera)) return false;
        if (!Circular) return true;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(touchRect, point, eventCamera, out var local)) return false;
        Vector2 relative = local - touchRect.rect.center;
        return relative.sqrMagnitude <= Mathf.Pow(Mathf.Min(touchRect.rect.width, touchRect.rect.height) * .5f, 2);
    }
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => ContainsScreenPoint(screenPoint, eventCamera);
    public void SetHighlighted(bool value)
    {
        GetComponent<Image>().color = value ? highlightColor :
            (editingLayout && selectedForLayout ? new Color(.2f, .55f, .85f, .85f) : new Color(.08f, .09f, .12f, .65f));
    }
}
