using UnityEngine;

public class SessionData : MonoBehaviour
{
    public static SessionData Instance;

    public const string SceneMainMenu = "MainMenu";
    public const string SceneTutorial = "Tutorial";
    public const string SceneTraining = "Training";
    public const string SceneFreeplay = "Freeplay";

    [Header("current lesson")]
    public Lesson PendingLesson { get; set; }
    public string PendingFreeplayMap { get; set; } = SceneFreeplay;

    //player pref saved vars
    public const string PrefMasterVolume = "Settings_MasterVolume";
    public const string PrefMusicVolume = "Settings_MusicVolume";
    public const string PrefSfxVolume = "Settings_SfxVolume";
    public const string PrefFullscreen = "Settings_Fullscreen";
    public const string PrefFFBstate= "Settings_FFBstate";
    public const string PrefFFBstrength = "Settings_FFBstrength";
    public const string PrefFFBlimit = "Settings_FFBlimit";

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    public float MasterVolume
    {
        get => PlayerPrefs.GetFloat(PrefMasterVolume, 1f);
        set
        {
            PlayerPrefs.SetFloat(PrefMasterVolume, value);
            PlayerPrefs.Save();
        }
    }

    public float MusicVolume
    {
        get => PlayerPrefs.GetFloat(PrefMusicVolume, 0.8f);
        set
        {
            PlayerPrefs.SetFloat(PrefMusicVolume, value);
            PlayerPrefs.Save();
        }
    }

    public float SfxVolume
    {
        get => PlayerPrefs.GetFloat(PrefSfxVolume, 1f);
        set
        {
            PlayerPrefs.SetFloat(PrefSfxVolume, value);
            PlayerPrefs.Save();
        }
    }

    public bool Fullscreen
    {
        get => PlayerPrefs.GetInt(PrefFullscreen, 1) == 1;
        set
        { 
            PlayerPrefs.SetInt(PrefFullscreen, value ? 1 : 0);
            Screen.fullScreen = value;
            PlayerPrefs.Save();
        }
    }

    public bool FFBstate
    {
        get => PlayerPrefs.GetInt(PrefFFBstate, 1) == 1;
        set
        { 
            PlayerPrefs.SetInt(PrefFFBstate, value ? 1 : 0);
            PlayerPrefs.Save(); 
        }
    }

    public float FFBstrength
    {
        get => PlayerPrefs.GetFloat(PrefFFBstrength, 0.5f);
        set
        {
            PlayerPrefs.SetFloat(PrefFFBstrength, value);
            PlayerPrefs.Save();
        }
    }

    public int FFBlimit
    {
        get => PlayerPrefs.GetInt(PrefFFBlimit, 270);
        set
        {
            PlayerPrefs.SetInt(PrefFFBlimit, value);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyPersistedSettings();
    }

    private void ApplyPersistedSettings()
    {
        Screen.fullScreen = Fullscreen;

        //set setting for audio and wheel when created=======================
    }
}