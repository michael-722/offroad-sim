using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class LessonManager : MonoBehaviour
{
    public static LessonManager Instance;

    [SerializeField] private List<SkillTier> tiers = new List<SkillTier>();

    [Header("car")]
    [SerializeField] private Transform car;

    [Header("briefing UI")]
    [SerializeField] private GameObject briefingPanel;
    [SerializeField] private TMP_Text briefingTitleText;
    [SerializeField] private TMP_Text briefingBodyText;
    [SerializeField] private TMP_Text briefingCountdownText;

    [Header("assessment HUD")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TMP_Text hudTimerText;
    [SerializeField] private TMP_Text hudScoreText;
    [SerializeField] private TMP_Text hudPassScoreText;
    [SerializeField] private TMP_Text hudObjectiveText;

    [Header("results UI")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text resultsTitleText;
    [SerializeField] private TMP_Text resultsBodyText;

    [Header("timing")]
    [SerializeField] private float briefingCountdownFrom = 5f;
    [SerializeField] private float resultDisplayTime = 4f;

    [Header("events")]
    public UnityEvent<LessonResult> OnLessonComplete;
    public UnityEvent<SkillTier> OnTierUnlocked;

    private Lesson _currentLesson;
    private LessonLayout _currentLayout;
    private LessonPhase _phase = LessonPhase.Inactive;
    private float _assessmentTimer;
    private float _assessmentScore;

    public LessonPhase CurrentPhase => _phase;
    public Lesson CurrentLesson => _currentLesson;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void StartLesson(Lesson lesson)
    {
        if (_phase != LessonPhase.Inactive)
        {
            Debug.LogWarning("cant start lesson while one is already running");
            return;
        }

        if (lesson == null)
        {
            Debug.LogError("start lesson called with null lesson");
            return;
        }

        if (!lesson.IsUnlocked())
        {
            Debug.Log($"lesson '{lesson.title}' is locked");
            return;
        }

        _currentLesson = lesson;

        _currentLayout = LessonLayoutRegister.Instance?.Get(lesson.lessonId);
        if (_currentLayout == null)
            Debug.LogWarning($"no layout registered for lesson '{lesson.lessonId}' name '{lesson.name}'");

        LessonLayoutRegister.Instance?.DeactivateAll();
        _currentLayout?.Activate();

        if (_currentLayout?.spawnPoint != null && car != null)
        {
            var rb = car.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            car.SetPositionAndRotation(_currentLayout.spawnPoint.position, _currentLayout.spawnPoint.rotation);

            if (lesson.isReverseLesson)
            {
                //set only reverse allowed
            }

            if (lesson.isNightLesson)
            {
                //set night and turn on lights - need to add lights
            }
        }

        

        StartCoroutine(LessonRoutine());
    }

    public void ReportAssessmentScore(float score, float weight = 1f)
    {
        if (_phase != LessonPhase.Assessment) return;

        _assessmentScore = Mathf.Lerp(_assessmentScore, score, weight * Time.deltaTime);
        _assessmentScore = Mathf.Clamp(_assessmentScore, 0f, 100f);

        if (hudScoreText != null)
            hudScoreText.text = $"Score: {_assessmentScore:F0}";
    }

    public void TriggerAssessmentEnd(bool passed)
    {
        if (_phase != LessonPhase.Assessment) return;
        StopAllCoroutines();
        StartCoroutine(EndAssessment(passed));
    }

    public IReadOnlyList<SkillTier> GetTiers() => tiers.AsReadOnly();

    private IEnumerator LessonRoutine()
    {
        SetPanels(briefing: false, hud: false, results: false);

        //show briefing with live countdown
        _phase = LessonPhase.Briefing;

        if (briefingTitleText != null) briefingTitleText.text = _currentLesson.title;
        if (briefingBodyText != null) briefingBodyText.text = _currentLesson.briefingText;

        briefingPanel?.SetActive(true);

        float countdown = briefingCountdownFrom;
        while (countdown > 0f)
        {
            countdown -= Time.deltaTime;
            if (briefingCountdownText != null)
                briefingCountdownText.text = $"Starting in {Mathf.CeilToInt(countdown)}...";
            yield return null;
        }
        //enable driving
        FindFirstObjectByType<CarController>().SetCanDrive(true);

        //countdown done - swap to hud and begin assessment
        briefingPanel?.SetActive(false);

        _phase = LessonPhase.Assessment;
        _assessmentScore = 100f;
        _assessmentTimer = _currentLesson.assessmentTimeLimit > 0f ? _currentLesson.assessmentTimeLimit : float.MaxValue;

        if (hudObjectiveText != null) hudObjectiveText.text = _currentLesson.assessmentObjective;
        if (hudPassScoreText != null) hudPassScoreText.text = $"Pass: {_currentLesson.passingScore:F0}";
        if (hudScoreText != null) hudScoreText.text = "Score: 100";

        hudPanel?.SetActive(true);

        //hide timer if this lesson has no time limit
        if (hudTimerText != null)
            hudTimerText.gameObject.SetActive(_currentLesson.assessmentTimeLimit > 0f);

        while (_phase == LessonPhase.Assessment)
        {
            _assessmentTimer -= Time.deltaTime;
            UpdateTimerHUD();

            if (_currentLesson.assessmentTimeLimit > 0f && _assessmentTimer <= 0f)
            {
                _assessmentTimer = 0f;
                yield return EndAssessment(false);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator EndAssessment(bool objectiveReached)
    {
        _phase = LessonPhase.Result;

        //disarm all triggers so they cant fire after assessment ends
        LessonLayoutRegister.Instance?.DeactivateAll();

        bool passed = objectiveReached && _assessmentScore >= _currentLesson.passingScore;

        _currentLesson.SaveResult(passed, _assessmentScore);

        LessonResult result = new LessonResult
        {
            lessonId = _currentLesson.lessonId,
            passed = passed,
            score = _assessmentScore,
            timeSeconds = _currentLesson.assessmentTimeLimit > 0f ? _currentLesson.assessmentTimeLimit - _assessmentTimer : 0f,
            attempts = _currentLesson.Attempts
        };

        CheckTierUnlocks();

        SetPanels(briefing: false, hud: false, results: true);
        ShowResults(result);
        OnLessonComplete?.Invoke(result);

        yield return new WaitForSeconds(resultDisplayTime);

        SetPanels(briefing: false, hud: false, results: false);

        _currentLayout?.Deactivate();
        _currentLayout = null;
        _currentLesson = null;
        _phase = LessonPhase.Inactive;
    }

    private void CheckTierUnlocks()
    {
        foreach (var tier in tiers)
        {
            if (tier == null || tier.IsUnlocked()) continue;
            if (tier.TryUnlock())
            {
                Debug.Log($"tier unlocked: {tier.displayName}");
                OnTierUnlocked?.Invoke(tier);
            }
        }
    }

    private void SetPanels(bool briefing, bool hud, bool results)
    {
        briefingPanel?.SetActive(briefing);
        hudPanel?.SetActive(hud);
        resultsPanel?.SetActive(results);
    }

    private void UpdateTimerHUD()
    {
        if (hudTimerText == null) return;
        float t = Mathf.Max(_assessmentTimer, 0f);
        int mins = Mathf.FloorToInt(t / 60f);
        int secs = Mathf.FloorToInt(t % 60f);
        hudTimerText.text = $"{mins:00}:{secs:00}";
        hudTimerText.color = t < 15f ? Color.red : Color.white;
    }

    private void ShowResults(LessonResult result)
    {
        string status = result.passed ? "<color=#66FF66>PASSED</color>" : "<color=#FF6666>FAILED</color>";

        if (resultsTitleText != null)
            resultsTitleText.text = $"{_currentLesson.title} {status}";

        if (resultsBodyText != null)
            resultsBodyText.text =
                $"Score {result.score:F0} / 100\n" +
                $"Time {result.timeSeconds:F1}s\n" +
                $"Attempts {result.attempts}";
    }

    public void ResetLessonState()
    {
        StopAllCoroutines();
        SetPanels(briefing: false, hud: false, results: false);

        if (_currentLayout != null)
        {
            _currentLayout.Deactivate();
            _currentLayout = null;
        }

        LessonLayoutRegister.Instance?.DeactivateAll();

        _currentLesson = null;
        _assessmentTimer = 0f;
        _assessmentScore = 0f;
        _phase = LessonPhase.Inactive;

        Debug.Log("LessonManager has been fully reset.");
    }
}

public enum LessonPhase { Inactive, Briefing, Assessment, Result }

[System.Serializable]
public class LessonResult
{
    public string lessonId;
    public bool passed;
    public float score;
    public float timeSeconds;
    public int attempts;
}