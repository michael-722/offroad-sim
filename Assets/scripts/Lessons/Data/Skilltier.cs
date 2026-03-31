using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "OffRoadSim/Skill Tier", fileName = "Tier_New")]
public class SkillTier : ScriptableObject
{
    [Header("identity")]
    public string tierId;
    public string displayName;
    [TextArea(1, 3)] public string description;

    public Sprite icon;

    [Header("unlock")]
    public bool unlockedByDefault = false;
    public List<SkillTier> prerequisiteTier;

    [Header("Lessons")]
    public List<Lesson> lessons = new List<Lesson>();//lesson appear in order of list

    public string PrefKeyUnlocked => $"SkillTier_{tierId}_Unlocked";


    public bool IsUnlocked()
    {
        if (unlockedByDefault) return true;
        return PlayerPrefs.GetInt(PrefKeyUnlocked, 0) == 1;
    }

    public bool IsFullyComplete()
    {
        foreach (var lesson in lessons)
        {
            if (lesson == null) continue;
            if (!lesson.IsComplete) return false;
        }
        return true;
    }

    public int CompletedLessonCount()
    {
        int count = 0;
        foreach (var lesson in lessons)
            if (lesson != null && lesson.IsComplete) count++;
        return count;
    }

    public bool TryUnlock()
    {
        if (IsUnlocked()) return false;

        //check prerqusite tier is fully complete
        if (prerequisiteTier != null)
        {
            for(int i = 0; i < prerequisiteTier.Count; i++) 
                if (!prerequisiteTier[i].IsFullyComplete())
                    return false;
        }

        PlayerPrefs.SetInt(PrefKeyUnlocked, 1);
        PlayerPrefs.Save();
        return true;
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PrefKeyUnlocked);
        PlayerPrefs.Save();
        foreach (var lesson in lessons)
            lesson?.ResetProgress();
    }

    public List<Lesson> GetAvailableLessons()
    {
        var available = new List<Lesson>();
        foreach (var lesson in lessons)
        {
            if (lesson != null && lesson.IsUnlocked())
                available.Add(lesson);
        }
        return available;
    }

    //saftey when creating scriptable objs
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(tierId))
            tierId = name.ToLower().Replace(" ", "_");
    }
#endif
}