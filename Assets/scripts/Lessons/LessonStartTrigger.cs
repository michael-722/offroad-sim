using UnityEngine;

public class LessonStartTrigger : MonoBehaviour
{
    void Start()
    {
        var lesson = SessionData.Instance?.PendingLesson;
        if (lesson != null)
            LessonManager.Instance.StartLesson(lesson);
    }
}
