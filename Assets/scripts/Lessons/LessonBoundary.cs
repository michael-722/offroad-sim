using UnityEngine;

public class LessonBoundary : MonoBehaviour
{
    public float graceSeconds = 2f;

    [Range(0f, 100f)]
    public float penaltyPerSecond = 10f;

    private bool _armed = false;
    private bool _outside = false;
    private float _outsideTimer = 0f;

    public void Arm()
    {
        _armed = true;
        _outside = false;
        _outsideTimer = 0f;
    }

    public void Disarm()
    {
        _armed = false;
        _outside = false;
    }

    private void Update()
    {
        if (!_armed || !_outside) return;

        _outsideTimer += Time.deltaTime;

        //apply score penalty each second while outside
        if (penaltyPerSecond > 0f)
        {
            float currentScore = LessonManager.Instance != null ? Mathf.Max(0f, 100f - (_outsideTimer * penaltyPerSecond)) : 0f;
            LessonManager.Instance?.ReportAssessmentScore(currentScore, 2f);
        }

        if (_outsideTimer >= graceSeconds)
            LessonManager.Instance?.TriggerAssessmentEnd(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!_armed)
            return;
        if (!other.CompareTag("Player"))
            return;

        _outside = true;
        _outsideTimer = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        //player returned inside grace period - cancel the fail countdown
        if (!_armed)
            return;
        if (!other.CompareTag("Player"))
            return;

        _outside = false;
        _outsideTimer = 0f;
    }
}