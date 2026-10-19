using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float radius = 70f;
    public Vector2 Input { get; private set; }
    private RectTransform background;

    private void Awake() => background = transform as RectTransform;
    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);
    public void OnDrag(PointerEventData eventData)
    {
        if (background == null) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out local);
        Input = Vector2.ClampMagnitude(local / radius, 1f);
        if (handle != null) handle.anchoredPosition = Input * radius;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        Input = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
