using UnityEngine;
using TMPro;
using System.Collections;

public class Reset : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 3f;
    [SerializeField] private TextMeshProUGUI confirmText;

    private Vector3 originalPos;
    private Quaternion originalRot;

    private Vector3 updatedPos;
    private Quaternion updatedRot;

    private Rigidbody rb;
    private CarController controller;

    private Coroutine fadeCoroutine;
    private PauseController pauseController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CarController>();
        pauseController = FindAnyObjectByType<PauseController>();
        confirmText.gameObject.SetActive(false);

        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.5f);

        originalPos = transform.position;
        originalRot = transform.rotation;

        updatedPos = originalPos;
        updatedRot = originalRot;

        InputManager.Instance.OnResetEvent += ResetPos;
    }

    public void ResetPos()
    {
        if (pauseController != null && pauseController.IsPaused)
            return;

        if (!confirmText.gameObject.activeSelf)
        {
            confirmText.gameObject.SetActive(true);

            Color c = confirmText.color;
            c.a = 1f;
            confirmText.color = c;

            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeAndDismiss());
        }
        else
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            DoReset();
        }
    }

    public void DoReset()
    {
        transform.position = updatedPos;
        transform.rotation = updatedRot;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        controller.ResetWheels();
        DismissUI();
    }

    private void DismissUI()
    {
        confirmText.gameObject.SetActive(false);

        Color c = confirmText.color;
        c.a = 1f;
        confirmText.color = c;
    }

    private IEnumerator FadeAndDismiss()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            Color c = confirmText.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            confirmText.color = c;

            yield return null;
        }

        DismissUI();
    }

    public void UpdateResetPos()
    {
        updatedPos = transform.position;
        updatedRot = transform.rotation;
    }
}