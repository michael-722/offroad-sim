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

    [Header("training UI")]
    [SerializeField] private GameObject trainingPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;

    [Header("results UI")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text resultsTitleText;
    [SerializeField] private TMP_Text resultsBodyText;

    [Header("timing")]
    [SerializeField] private float briefingDuration = 6f;
    [SerializeField] private float practiceMinDuration = 15f;
    [SerializeField] private float resultDisplayTime = 4f;

    [Header("events")]
    public UnityEvent<LessonResult> OnLessonComplete;
    public UnityEvent<SkillTier> OnTierUnlocked;

    private Lesson _currentLesson;
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
        StartCoroutine(LessonRoutine());
    }

    public void ReportAssessmentScore(float score, float weight = 1f)
    {
        if (_phase != LessonPhase.Assessment) return;

        _assessmentScore = Mathf.Lerp(_assessmentScore, score, weight * Time.deltaTime);
        _assessmentScore = Mathf.Clamp(_assessmentScore, 0f, 100f);

        if (scoreText != null)
            scoreText.text = $"Score: {_assessmentScore:F0}";
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
        trainingPanel.SetActive(true);

        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);

        //briefing - auto advances after duration or input
        _phase = LessonPhase.Briefing;
        SetTitle(_currentLesson.title);
        SetPhase("Briefing");
        SetInstruction(_currentLesson.briefingText);

        float briefTimer = 0f;
        while (briefTimer < briefingDuration && !AnyGameplayInput())
        {
            briefTimer += Time.deltaTime;
            yield return null;
        }

        yield return null; //absorb input frame

        //practice - player drives freely
        _phase = LessonPhase.Practice;
        SetPhase("Practice");
        SetInstruction(
            $"{_currentLesson.assessmentObjective}\n\n" +
            $"(Practice time: {practiceMinDuration:F0}s — then press any input to begin assessment)"
        );

        float practiceTimer = 0f;
        while (true)
        {
            practiceTimer += Time.deltaTime;
            if (practiceTimer >= practiceMinDuration && AnyGameplayInput())
                break;
            yield return null;
        }

        yield return null;//absorb input frame

        //assessment - runs until assessment end is called or time runs out
        _phase = LessonPhase.Assessment;
        _assessmentScore = 100f;
        _assessmentTimer = _currentLesson.assessmentTimeLimit > 0f ? _currentLesson.assessmentTimeLimit : float.MaxValue;

        SetPhase("Assessment");
        SetInstruction(_currentLesson.assessmentObjective);

        if (timerText != null)
            timerText.gameObject.SetActive(_currentLesson.assessmentTimeLimit > 0f);

        while (_phase == LessonPhase.Assessment)
        {
            _assessmentTimer -= Time.deltaTime;
            UpdateTimerUI();

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
        ShowResults(result);
        OnLessonComplete?.Invoke(result);

        yield return new WaitForSeconds(resultDisplayTime);

        trainingPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

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

    private void SetTitle(string text) { if (titleText != null) titleText.text = text; }
    private void SetPhase(string text) { if (phaseText != null) phaseText.text = text; }
    private void SetInstruction(string text) { if (instructionText != null) instructionText.text = text; }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        float t = Mathf.Max(_assessmentTimer, 0f);
        int mins = Mathf.FloorToInt(t / 60f);
        int secs = Mathf.FloorToInt(t % 60f);
        timerText.text = $"{mins:00}:{secs:00}";
        timerText.color = t < 15f ? Color.red : Color.white;
    }

    private void ShowResults(LessonResult result)
    {
        if (resultsPanel != null) resultsPanel.SetActive(true);

        string status = result.passed ? "<color=#66FF66>PASSED</color>" : "<color=#FF6666>FAILED</color>";

        if (resultsTitleText != null)
            resultsTitleText.text = $"{_currentLesson.title}  —  {status}";

        if (resultsBodyText != null)
            resultsBodyText.text =
                $"Score     {result.score:F0} / 100\n" +
                $"Required  {_currentLesson.passingScore:F0}\n" +
                $"Time      {result.timeSeconds:F1}s\n" +
                $"Attempts  {result.attempts}";
    }

    //any keyboard key or gamepad button/axis - not mouse=======need to test with wheel
    private bool AnyGameplayInput()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl btn && btn.wasPressedThisFrame)
                    return true;

                if (control is UnityEngine.InputSystem.Controls.AxisControl axis && Mathf.Abs(axis.ReadValue()) > 0.3f)
                    return true;
            }
        }

        return false;
    }
}

public enum LessonPhase { Inactive, Briefing, Practice, Assessment, Result }

[System.Serializable]
public class LessonResult
{
    public string lessonId;
    public bool passed;
    public float score;
    public float timeSeconds;
    public int attempts;
}