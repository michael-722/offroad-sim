using System.Collections.Generic;
using UnityEngine;

public class LessonLayout : MonoBehaviour
{
    public string lessonId;
    public Transform spawnPoint;

    [HideInInspector] public List<LessonGoal> goals = new List<LessonGoal>();
    [HideInInspector] public List<LessonBoundary> boundaries = new List<LessonBoundary>();
    private void Awake()
    {
        GetComponentsInChildren(true, goals);
        GetComponentsInChildren(true, boundaries);

        LessonLayoutRegister.Instance?.Register(this);

        //all layouts start inactive - LessonManager handels activation
        gameObject.SetActive(false);
    }

    public void Activate()
    {
        gameObject.SetActive(true);

        foreach (var g in goals) g.Arm();
        foreach (var b in boundaries) b.Arm();
    }

    public void Deactivate()
    {
        if (lessonId == LessonManager.Instance._currentLayout.lessonId)
            return;

        foreach (var g in goals) g.Disarm();
        foreach (var b in boundaries) b.Disarm();

        gameObject.SetActive(false);
        Destroy(this.gameObject);
    }
}