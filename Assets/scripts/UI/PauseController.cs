using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject settingsUI;

    private void Start()
    {
        pauseUI.SetActive(false);
        settingsUI.SetActive(false);

        InputManager.Instance.OnPauseEvent += PauseGame;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnPauseEvent -= PauseGame;
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        
        pauseUI.SetActive(true);
        settingsUI.SetActive(false);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene(0);//main menu
    }

    public void ResumeGame()
    {
        Time.timeScale = 1.0f;

        pauseUI.SetActive(false);
        settingsUI.SetActive(false);
    } 
}
