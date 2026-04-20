using UnityEngine;
using UnityEngine.EventSystems;

public class UIFocusHandeler : MonoBehaviour
{
    public GameObject firstSelectedButton;

    private void OnEnable()
    {
        InputManager.Instance.OnInputSourceChanged += ToggleFocus;

        //inital check
        if (InputManager.Instance.IsControllerMode)
            FocusUI();
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnInputSourceChanged -= ToggleFocus;
    }

    private void ToggleFocus(bool isController)
    {
        if (isController)
            FocusUI();
        else
            DeselectUI();
    }

    public void FocusUI()
    {
        //highlight set button for controller navigation
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    public void DeselectUI()
    {
        //clears selection so mouse users arent stuck with rnadom highlight
        EventSystem.current.SetSelectedGameObject(null);
    }
}