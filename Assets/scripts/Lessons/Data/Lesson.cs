using UnityEngine;

[CreateAssetMenu(menuName = "OffRoadSim/Lesson", fileName = "Lesson_New")]
public class Lesson : ScriptableObject
{
    [Header("id")]
    public string lessonId;
    public string title;
    public string bordaUnit;

    [Header("briefing")]
    [TextArea(4, 10)] public string briefingText;

    [Header("assessment")]
    [TextArea(2, 4)] public string assessmentObjective;
    public float assessmentTimeLimit = 120f;//in s
    [Range(0f, 100f)] public float passingScore = 70f;//need to have score break down so can see where points gained/lost==================

    [Header("unlock")]
    public Lesson[] prerequisites;

    public bool isNightLesson = false;
    public bool isReverseLesson = false;

    public string PrefKeyCompleted => $"SkillLesson_{lessonId}_Completed";
    public string PrefKeyBestScore => $"SkillLesson_{lessonId}_BestScore";
    public string PrefKeyAttempts => $"SkillLesson_{lessonId}_Attempts";

    public bool IsComplete => PlayerPrefs.GetInt(PrefKeyCompleted, 0) == 1;
    public float BestScore => PlayerPrefs.GetFloat(PrefKeyBestScore, 0f);
    public int Attempts => PlayerPrefs.GetInt(PrefKeyAttempts, 0);

    public bool IsUnlocked()
    {
        if (prerequisites == null || prerequisites.Length == 0) return true;
        foreach (var pre in prerequisites)
        {
            if (pre == null) continue;
            if (!pre.IsComplete) return false;
        }
        return true;
    }

    public void SaveResult(bool passed, float score)
    {
        if (passed)
            PlayerPrefs.SetInt(PrefKeyCompleted, 1);

        if (score > BestScore)
            PlayerPrefs.SetFloat(PrefKeyBestScore, score);

        PlayerPrefs.SetInt(PrefKeyAttempts, Attempts + 1);
        PlayerPrefs.Save();
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PrefKeyCompleted);
        PlayerPrefs.DeleteKey(PrefKeyBestScore);
        PlayerPrefs.DeleteKey(PrefKeyAttempts);
        PlayerPrefs.Save();
    }

    //saftey when editing scriptable objs
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(lessonId))
            lessonId = name.ToLower().Replace(" ", "_");
    }
#endif
}