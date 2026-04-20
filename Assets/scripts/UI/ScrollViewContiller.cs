using UnityEngine;
using UnityEngine.UI;

public class ScrollViewContiller : MonoBehaviour
{
    [SerializeField] float scrollStep = 0.15f;

    private ScrollRect _scrollRect;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnCycleCameraEvent += ScrollDown;
            InputManager.Instance.OnResetEvent += ScrollUp;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnCycleCameraEvent -= ScrollDown;
            InputManager.Instance.OnResetEvent -= ScrollUp;
        }
    }

    private void ScrollUp()
    {
        if (!InputManager.Instance.IsControllerMode) return;

        _scrollRect.verticalNormalizedPosition += scrollStep;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition);
    }

    private void ScrollDown()
    {
        if (!InputManager.Instance.IsControllerMode) return;

        _scrollRect.verticalNormalizedPosition -= scrollStep;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition);
    }
}