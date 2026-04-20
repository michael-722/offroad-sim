using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollViewContiller : MonoBehaviour
{
    private ScrollRect scrollRect;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    //private void OnEnable()
    //{
    //    if (InputManager.Instance != null)
    //    {
    //        InputManager.Instance.OnCycleCameraEvent += UpdatePos;
    //        InputManager.Instance.OnResetEvent += UpdatePos;
    //    }
    //}
    
    //private void OnDisable()
    //{
    //    if (InputManager.Instance != null)
    //    {
    //        InputManager.Instance.OnCycleCameraEvent -= UpdatePos;
    //        InputManager.Instance.OnResetEvent -= UpdatePos;
    //    }
    //}

    private void UpdatePos()
    {
        scrollRect.content.localPosition = GetSnapPos(scrollRect, EventSystem.current.currentSelectedGameObject.GetComponent<RectTransform>());
    }

    private Vector2 GetSnapPos(ScrollRect rect, RectTransform child)
    {
        Canvas.ForceUpdateCanvases();
        Vector2 viewportLocalPosition = rect.viewport.localPosition;
        Vector2 childLocalPosition = child.localPosition;
        Vector2 result = new Vector2(0 - (viewportLocalPosition.x + childLocalPosition.x), 0 - (viewportLocalPosition.y + childLocalPosition.y));
        return result;
    }
}