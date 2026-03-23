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

    void Start()
    {
        InputManager.Instance.OnResetEvent += ResetPos;

        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CarController>();

        originalPos = transform.position;
        originalRot = transform.rotation;

        updatedPos = originalPos;
        updatedRot = originalRot;
    }

    void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnResetEvent -= ResetPos;
    }

    public void ResetPos()
    {
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

    private void DoReset()
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