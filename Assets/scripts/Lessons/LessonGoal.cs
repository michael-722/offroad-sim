using System.Collections;
using UnityEngine;

public class LessonGoal : MonoBehaviour
{
    [Header("speed settings")]
    [SerializeField] private float targetSlowSpeed = 20f;
    [SerializeField] private float entrySpeedRequired = 40f;

    [Header("UI settings")]
    [SerializeField] private TMPro.TMP_Text _errorTxt;
    [SerializeField] private float fadeDuration = 2f;

    [Header("assessment settings")]
    [Range(0f, 100f)] public float scoreValue = 100f;
    public bool endsAssessment = true;
    public bool triggerOnce = true;

    private bool _armed = false;
    private bool _triggered = false;
    private bool _hasSlowedDown = false;
    private bool _isToolSlow =false;

    public void Arm()
    {
        _armed = true;
        _triggered = false;
        _hasSlowedDown = false;
    }

    public void Disarm()
    {
        _armed = false;
        _triggered = false;
        _hasSlowedDown = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_armed || _triggered) return;
        if (!other.CompareTag("Player")) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            float currentSpeed = rb.linearVelocity.magnitude * 3.6f;
            if (currentSpeed < entrySpeedRequired)
            {
                _errorTxt.text = $"Too slow!\nEnter at going {entrySpeedRequired} or faster.";

                _isToolSlow = true;
                HandleFailure();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_armed || _triggered || _hasSlowedDown) return;
        if (!other.CompareTag("Player")) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null &! _isToolSlow)
        {
            float currentSpeed = rb.linearVelocity.magnitude * 3.6f;

            if (currentSpeed <= targetSlowSpeed + 2)
            {
                _hasSlowedDown = true;
                HandleSuccess();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_armed || _triggered) return;
        if (!other.CompareTag("Player")) return;

        if (!_hasSlowedDown &!_isToolSlow)
        {
            _errorTxt.text = $"Failed!\nYou didnt slow down to or below {targetSlowSpeed} inside the zone.";
            HandleFailure();
        }
        else
            _isToolSlow = false;
    }

    private void HandleSuccess()
    {
        _triggered = true;
        LessonManager.Instance?.ReportAssessmentScore(scoreValue);

        gameObject.SetActive(false);

        if (endsAssessment)
            LessonManager.Instance?.TriggerAssessmentEnd(true);
    }

    private void HandleFailure()
    {
        _triggered = true;
        _errorTxt.gameObject.SetActive(true);

        StartCoroutine(FadeAndDismiss());

        LessonManager.Instance?.ReportAssessmentScore(0);

        if (endsAssessment)
            LessonManager.Instance?.TriggerAssessmentEnd(false);
    }

    private IEnumerator FadeAndDismiss()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = _errorTxt.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            _errorTxt.color = c;
            yield return null;
        }
        DismissUI();
    }

    private void DismissUI()
    {
        _errorTxt.gameObject.SetActive(false);
        Color c = _errorTxt.color;
        c.a = 1f;
        _errorTxt.color = c;
    }
}