using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    [SerializeField] private bool isMenu = false;

    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject settingsUI;

    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown wheelLimitDropdown;
    [SerializeField] private Toggle FFBtoggle;
    [SerializeField] private Slider FFBstrengthSlider;
    [SerializeField] private List<string> FFBLimits;

    public event Action OnOpenClosePauseUI;

    public bool IsPaused => pauseUI.activeInHierarchy || settingsUI.activeInHierarchy;

    private void Start()
    {
        InitialiseSettingsPanel();

        if (!isMenu)
        {
            pauseUI.SetActive(false);
            settingsUI.SetActive(false);

            InputManager.Instance.OnPauseEvent += PauseGame;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnPauseEvent -= PauseGame;

        if (wheelLimitDropdown != null)
            wheelLimitDropdown.onValueChanged.RemoveAllListeners();
        if (FFBstrengthSlider != null)
            FFBstrengthSlider.onValueChanged.RemoveAllListeners();
        if (FFBtoggle != null)
            FFBtoggle.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveAllListeners();
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
    }

    private void PauseGame()
    {
        if (IsPaused)
            return;

        Application.targetFrameRate = 400;

        Time.timeScale = 0f;
        OnOpenClosePauseUI?.Invoke();

        pauseUI.SetActive(true);
        settingsUI.SetActive(false);
    }

    public void MainMenu()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void ResumeGame()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1.0f;
        OnOpenClosePauseUI?.Invoke();

        pauseUI.SetActive(false);
        settingsUI.SetActive(false);
    }

    private void InitialiseSettingsPanel()
    {
        if (SessionData.Instance == null)
            return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = SessionData.Instance.MasterVolume;
            masterVolumeSlider.onValueChanged.AddListener(v => SessionData.Instance.MasterVolume = v);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = SessionData.Instance.MusicVolume;
            musicVolumeSlider.onValueChanged.AddListener(v => SessionData.Instance.MusicVolume = v);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = SessionData.Instance.SfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(v => SessionData.Instance.SfxVolume = v);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = SessionData.Instance.Fullscreen;
            fullscreenToggle.onValueChanged.AddListener(v => SessionData.Instance.Fullscreen = v);
        }

        if (FFBtoggle != null)
        {
            FFBtoggle.isOn = SessionData.Instance.FFBstate;
            FFBtoggle.onValueChanged.AddListener(v => SessionData.Instance.FFBstate = v);
        }

        if (FFBstrengthSlider != null)
        {
            FFBstrengthSlider.value = SessionData.Instance.FFBstrength;
            FFBstrengthSlider.onValueChanged.AddListener(v => SessionData.Instance.FFBstrength = v);
        }

        if (wheelLimitDropdown != null)
        {
            wheelLimitDropdown.ClearOptions();
            wheelLimitDropdown.AddOptions(FFBLimits);
            wheelLimitDropdown.onValueChanged.AddListener(v => SessionData.Instance.FFBlimit = Int32.Parse(FFBLimits[v]));
        }
    }
}
