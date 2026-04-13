using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("screens")]
    [SerializeField] private GameObject panelHome;
    [SerializeField] private GameObject panelPlay;
    [SerializeField] private GameObject panelTraining;
    [SerializeField] private GameObject panelLessonList;
    [SerializeField] private GameObject panelLessonDetail;
    [SerializeField] private GameObject panelSettings;
    [SerializeField] private GameObject panelLoading;

    [SerializeField] private TMP_Text versionText;

    [Header("tier buttons")]
    [SerializeField] private List<TierBtnUI> tierButtons = new List<TierBtnUI>();

    [Header("lesson list")]
    [SerializeField] private TMP_Text lessonListTierTitle;
    [SerializeField] private Transform lessonListContainer;
    [SerializeField] private GameObject lessonRowPrefab;

    [Header("lesson details")]
    [SerializeField] private TMP_Text lessonTitle;
    [SerializeField] private TMP_Text lessonBordaUnit;
    [SerializeField] private TMP_Text lessonObjective;
    [SerializeField] private TMP_Text lessonTimeLimit;
    [SerializeField] private TMP_Text lessonPassingScore;
    [SerializeField] private TMP_Text lessonBestScore;
    [SerializeField] private TMP_Text lessonAttempts;
    [SerializeField] private TMP_Text lessonStatus;
    [SerializeField] private Button lessonStartBtn;
    [SerializeField] private TMP_Text lessonStartBtnTxt;

    [Header("loading")]
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TMP_Text loadingLabel;

    [Header("teirs")]
    [SerializeField] private List<SkillTier> tiers = new List<SkillTier>();

    private SkillTier _selectedTier;
    private Lesson _selectedLesson;
    private GameObject _activePanel;
    private List<GameObject> _spawnedLessonRows = new List<GameObject>();

    private void Start()
    {
        versionText.text = $"v{Application.version}";

        //data check
        if (SessionData.Instance == null)
        {
            var go = new GameObject("SessionData");
            go.AddComponent<SessionData>();
        }

        ShowPanel(panelHome);
    }

    public void OnClickPlay() => ShowPanel(panelPlay);
    public void OnClickTrainingMode() => ShowTrainingPanel();
    public void OnClickSettings() => ShowPanel(panelSettings);
    public void OnClickBack() => ShowPanel(panelHome);
    public void OnClickBackToPlay() => ShowPanel(panelPlay);
    public void OnClickBackToTiers() => ShowPanel(panelTraining);
    public void OnClickBackToList() => ShowLessonList(_selectedTier);

    public void OnClickTutorial()
    {
        LoadScene(SessionData.SceneTutorial);
    }

    public void OnClickFreeplay()
    {
        SessionData.Instance.PendingFreeplayMap = SessionData.SceneFreeplay;
        LoadScene(SessionData.SceneFreeplay);
    }

    public void OnClickStartLesson()
    {
        if (_selectedLesson == null) return;
        SessionData.Instance.PendingLesson = _selectedLesson;
        LoadScene(SessionData.SceneTraining);
    }


    public void OnClickQuit()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    private void ShowTrainingPanel()
    {
        RefreshTierButtons();
        ShowPanel(panelTraining);
    }

    private void RefreshTierButtons()
    {
        for (int i = 0; i < tierButtons.Count; i++)
        {
            if (i >= tiers.Count || tierButtons[i] == null) continue;

            SkillTier tier = tiers[i];
            TierBtnUI ui = tierButtons[i];
            bool unlocked = tier.IsUnlocked();

            //setting tiers btn info
            if (ui.nameText != null) ui.nameText.text = tier.displayName;
            if (ui.descText != null) ui.descText.text = tier.description;
            if (ui.progressText != null) ui.progressText.text = $"{tier.CompletedLessonCount()} / {tier.lessons.Count}";
            if (ui.lockIcon != null) ui.lockIcon.SetActive(!unlocked);

            //set btn on click
            int index = i;
            if (ui.button != null)
            {
                ui.button.onClick.RemoveAllListeners();
                ui.button.onClick.AddListener(() => ShowLessonList(tiers[index]));
            }

            if (ui.button != null) ui.button.interactable = unlocked;
        }
    }

    private void ShowLessonList(SkillTier tier)
    {
        _selectedTier = tier;

        if (lessonListTierTitle != null)
            lessonListTierTitle.text = tier.displayName;

        //clear old rows
        foreach (var row in _spawnedLessonRows)
            if (row != null) Destroy(row);
        _spawnedLessonRows.Clear();

        //spawn row per lesson from scriptable obj
        foreach (var lesson in tier.lessons)
        {
            if (lesson == null || lessonRowPrefab == null || lessonListContainer == null)
                continue;

            GameObject rowGO = Instantiate(lessonRowPrefab, lessonListContainer);
            _spawnedLessonRows.Add(rowGO);

            LessonRowUI row = rowGO.GetComponent<LessonRowUI>();
            if (row == null) continue;

            bool unlocked = lesson.IsUnlocked();

            //set data for each lesson
            if (row.titleText != null) row.titleText.text = lesson.title;
            if (row.statusText != null)
            {
                if (!unlocked)
                    row.statusText.text = "<color=#888888>Locked</color>";
                else if (lesson.IsComplete)
                    row.statusText.text = $"Best Score: {lesson.BestScore:F0}";
                else
                    row.statusText.text = "Not attempted";
            }
            if (row.lockIcon != null) row.lockIcon.SetActive(!unlocked);
            if (row.button != null)
            {
                row.button.interactable = unlocked;
                var captured = lesson;
                row.button.onClick.RemoveAllListeners();
                row.button.onClick.AddListener(() => ShowLessonDetail(captured));
            }
        }

        ShowPanel(panelLessonList);
    }

    private void ShowLessonDetail(Lesson lesson)
    {
        _selectedLesson = lesson;

        if (lessonTitle != null) lessonTitle.text = lesson.title;
        if (lessonBordaUnit != null) lessonBordaUnit.text = lesson.bordaUnit;
        if (lessonObjective != null) lessonObjective.text = lesson.assessmentObjective;
        if (lessonPassingScore != null) lessonPassingScore.text = $"Pass mark: {lesson.passingScore:F0}";
        if (lessonTimeLimit != null) lessonTimeLimit.text = lesson.assessmentTimeLimit > 0f ? $"Time limit: {lesson.assessmentTimeLimit:F0}s" : "Time limit: None";
        if (lessonBestScore != null) lessonBestScore.text = lesson.BestScore > 0f ? $"Best score: {lesson.BestScore:F0}"  : "Best score: —";
        if (lessonAttempts != null) lessonAttempts.text = lesson.Attempts > 0 ? $"Attempts: {lesson.Attempts}" : "Not yet attempted";
        if (lessonStatus != null) lessonStatus.text = lesson.IsComplete ? "<color=#66FF66>PASSED</color>" : "<color=#AAAAAA>Not passed</color>";
        if (lessonStartBtn != null) lessonStartBtn.interactable = lesson.IsUnlocked();
        if (lessonStartBtnTxt != null) lessonStartBtnTxt.text = lesson.IsComplete ? "Replay" : "Start";

        ShowPanel(panelLessonDetail);
    }

    private void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        ShowPanel(panelLoading);

        if (loadingLabel != null) loadingLabel.text = $"Loading {sceneName}...";
        if (loadingBar != null) loadingBar.value = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        //waiting for scene to load
        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingBar != null) loadingBar.value = progress;

            //activate once fully loaded
            if (op.progress >= 0.9f)
            {
                if (loadingBar != null) loadingBar.value = 1f;
                yield return new WaitForSeconds(0.45f);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    //only activeate passed obj
    private void ShowPanel(GameObject target)
    {
        panelHome?.SetActive(false);
        panelPlay?.SetActive(false);
        panelTraining?.SetActive(false);
        panelLessonList?.SetActive(false);
        panelLessonDetail?.SetActive(false);
        panelSettings?.SetActive(false);
        panelLoading?.SetActive(false);

        if (target != null) target.SetActive(true);
        _activePanel = target;
    }
}