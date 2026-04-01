using UnityEngine;

public class LessonGoal : MonoBehaviour
{
    [SerializeField] private float speedRequired;
    [SerializeField] private float waitTimeRequired;

    [Range(0f, 100f)] public float scoreValue = 100f;
    public bool endsAssessment = true;//when reaches assesment end
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

        Rigidbody playerRb = other.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            float currentSpeed = playerRb.linearVelocity.magnitude;

            if (currentSpeed >= speedRequired)
                HandleSuccess();
            else
                HandleFailure();
        }
    }

    private void HandleSuccess()
    {
        _triggered = true;
        LessonManager.Instance?.ReportAssessmentScore(scoreValue);

        if (endsAssessment)
            LessonManager.Instance?.TriggerAssessmentEnd(true);
    }

    private void HandleFailure()
    {
        //tell player to try again they wernt going fast enough=====================

        _triggered = true;
        LessonManager.Instance?.ReportAssessmentScore(0);

        if (endsAssessment)
            LessonManager.Instance?.TriggerAssessmentEnd(false);
    }
}