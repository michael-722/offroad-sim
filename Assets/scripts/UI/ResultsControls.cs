using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultsControls : MonoBehaviour
{
    public void PlayAgain()
    {
        var lesson = SessionData.Instance?.PendingLesson;
        
        FindFirstObjectByType<Reset>().DoReset();
        LessonManager.Instance.StartLesson(lesson);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
