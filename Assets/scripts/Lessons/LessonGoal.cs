using UnityEngine;

public class LessonGoal : MonoBehaviour
{
    [Range(0f, 100f)] public float scoreValue = 100f;
    public bool endsAssessment = true;//when reached should assesment end
    public bool triggerOnce = true;

    private bool _armed = false;
    private bool _triggered = false;

    public void Arm()
    {
        _armed = true;
        _triggered = false;
    }

    public void Disarm()
    {
        _armed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_armed) return;
        if (triggerOnce && _triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        LessonManager.Instance?.ReportAssessmentScore(scoreValue);

        if (endsAssessment)
            LessonManager.Instance?.TriggerAssessmentEnd(true);
    }
}