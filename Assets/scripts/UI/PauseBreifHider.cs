using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class PauseBreifHider : MonoBehaviour
{
    [SerializeField] private GameObject breif;

    private PauseController pauseController;
    private bool isOpen = false;

    private void Start()
    {
        pauseController = GetComponent<PauseController>();
        pauseController.OnOpenClosePauseUI += ToggleBrief;
    }
    private void OnDisable()
    {
        if (pauseController != null)
            GetComponent<PauseController>().OnOpenClosePauseUI -= ToggleBrief;
    }

    private void ToggleBrief()
    {
        if (breif.activeInHierarchy)
            isOpen = true;

        if (FindAnyObjectByType<PauseController>().IsPaused)
        {
            if (isOpen)
            {
                breif.SetActive(true);
                isOpen = false;
            }
        }
        else
        {
            if (breif.activeInHierarchy)
            {
                breif.SetActive(false);
                isOpen = true;
            }
        }
    }
}
