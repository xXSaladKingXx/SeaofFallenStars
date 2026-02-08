using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("If true, this object will persist across scene loads.")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool enforceSingleton = true;

    private static SettingsMenuController _instance;
    public static SettingsMenuController Instance => _instance;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject musicMenuPanel;
    [SerializeField] private GameObject infoPanel;

    [Header("Resolution & Fullscreen")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;

    [Header("Music Player")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip[] musicPlaylist;
    [SerializeField] private bool autoPlayMusicOnStart = false;

    [Header("Music UI (TMP)")]
    [SerializeField] private TMP_Text currentTrackNameText;
    [SerializeField] private TMP_Text currentTrackTimeText;
    [SerializeField] private TMP_Text playPauseButtonLabel; // optional

    [Header("Travel Menu")]
    [SerializeField] private Button travelMenuButton;
    [SerializeField] private GameObject travelMenuPrefab;
    [SerializeField] private string travelMenuWindowKey = "TravelMenu";


    // ---------------------------
    // Map Camera Settings (NEW)
    // ---------------------------
    [Header("Map Camera Settings (Optional UI Wiring)")]
    [SerializeField] private Slider mapZoomSpeedSlider;
    [SerializeField] private Slider mapPanSpeedSlider;
    [SerializeField] private Slider mapSnapDurationSlider;
    [SerializeField] private Slider mapResetZoomSlider;
    [SerializeField] private Slider mapModifierZoomMultiplierSlider;

    [SerializeField] private TMP_Dropdown mapModifierDropdown;
    [SerializeField] private TMP_Dropdown mapPanButtonDropdown;

    [Header("Map Camera Defaults")]
    [SerializeField] private float defaultMapZoomSpeed = 1000f;
    [SerializeField] private float defaultMapPanSpeed = 30f;
    [SerializeField] private float defaultMapSnapDuration = 0.5f;
    [SerializeField] private Vector3 defaultMapResetPosition = new Vector3(0f, 0f, -10f);
    [SerializeField] private float defaultMapResetZoom = 500f;
    [SerializeField] private float defaultMapModifierZoomMultiplier = 2f;
    [SerializeField] private MapModifierKey defaultMapModifier = MapModifierKey.Ctrl;
    [SerializeField] private MapPanMouseButton defaultMapPanButton = MapPanMouseButton.Right;

    public event Action MapSettingsChanged;

    public float MapZoomSpeed { get; private set; }
    public float MapPanSpeed { get; private set; }
    public float MapSnapDuration { get; private set; }
    public Vector3 MapResetPosition { get; private set; }
    public float MapResetZoom { get; private set; }
    public float MapModifierZoomMultiplier { get; private set; }
    public MapModifierKey MapModifier { get; private set; }
    public MapPanMouseButton MapPanButton { get; private set; }

    public enum MapModifierKey
    {
        None = 0,
        Ctrl = 1,
        Alt = 2,
        Shift = 3
    }

    public enum MapPanMouseButton
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    private bool isSettingsOpen = false;
    private bool isMusicMenuOpen = false;
    private bool isUserPaused = false;

    // Resolution data
    private readonly List<Resolution> availableResolutions = new List<Resolution>();
    private int currentResolutionIndex = 0;

    // PlayerPrefs keys
    private const string PREF_RESOLUTION_INDEX = "ResolutionIndex";
    private const string PREF_FULLSCREEN = "Fullscreen";
    private const string PREF_MASTER_VOLUME = "MasterVolume";
    private const string PREF_MUSIC_TRACK_INDEX = "MusicTrackIndex";
    private const string PREF_MUSIC_TIME = "MusicTime";

    // Map prefs keys (NEW)
    private const string PREF_MAP_ZOOM_SPEED = "Map_ZoomSpeed";
    private const string PREF_MAP_PAN_SPEED = "Map_PanSpeed";
    private const string PREF_MAP_SNAP_DURATION = "Map_SnapDuration";
    private const string PREF_MAP_RESET_X = "Map_ResetX";
    private const string PREF_MAP_RESET_Y = "Map_ResetY";
    private const string PREF_MAP_RESET_Z = "Map_ResetZ";
    private const string PREF_MAP_RESET_ZOOM = "Map_ResetZoom";
    private const string PREF_MAP_MOD_ZOOM_MULT = "Map_ModZoomMult";
    private const string PREF_MAP_MODIFIER = "Map_Modifier";
    private const string PREF_MAP_PAN_BUTTON = "Map_PanButton";

    // Save throttling (avoid PlayerPrefs every frame)
    private float _prefsSaveTimer = 0f;
    [SerializeField] private float prefsSaveIntervalSeconds = 1.0f;

    private void Awake()
    {
        if (enforceSingleton)
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // Panels
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (musicMenuPanel != null) musicMenuPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);

        InitResolutionSettings();
        InitAudioSettings();
        InitMusicPlayer();
        InitMapCameraSettings(); // NEW
        if (travelMenuButton != null)
        {
            travelMenuButton.onClick.RemoveListener(OpenTravelMenu);
            travelMenuButton.onClick.AddListener(OpenTravelMenu);
        }

    }

    private void Update()
    {
        UpdateMusicTimerUI();
        AutoAdvanceTrackIfFinished();
        ThrottledSaveMusicProgress();
    }

    /// <summary>
    /// Moves this object out of the DontDestroyOnLoad scene and into the currently active scene.
    /// Use this when you want to "undo" DDOL behavior at runtime.
    /// </summary>
    public void MoveBackToActiveScene()
    {
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
        dontDestroyOnLoad = false;
    }

    #region Settings Panel

    public void ShowSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("SettingsMenuController: Settings panel is not assigned.");
            return;
        }

        settingsPanel.SetActive(true);
        isSettingsOpen = true;
    }

    public void HideSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);
        isSettingsOpen = false;
    }

    public void HideSettingsImmediate()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        isSettingsOpen = false;
    }

    public bool IsOpen() => isSettingsOpen;

    #endregion

    #region Info Panel
    public void OpenTravelMenu()
    {
        if (travelMenuPrefab == null)
        {
            Debug.LogWarning("SettingsMenuController: Travel menu prefab is not assigned.");
            return;
        }

        UIWindowRegistry.OpenOrFocus(travelMenuWindowKey, () =>
        {
            Transform parent = GetTopLevelUIParent();
            GameObject instance = Instantiate(travelMenuPrefab, parent);

            var controller = instance.GetComponent<TravelMenuController>();
            if (controller != null)
                controller.SetRegistryKey(travelMenuWindowKey);

            return instance;
        });
    }

    private Transform GetTopLevelUIParent()
    {
        // Prefer: same parent as settingsPanel (typically the main Canvas)
        if (settingsPanel != null && settingsPanel.transform.parent != null)
            return settingsPanel.transform.parent;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        return canvas != null ? canvas.transform : transform;
    }


    public void ShowInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(true);
    }

    public void HideInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    public void ToggleInfoPanel()
    {
        if (infoPanel == null) return;
        infoPanel.SetActive(!infoPanel.activeSelf);
    }

    #endregion

    #region Resolution & Fullscreen

    private void InitResolutionSettings()
    {
        if (resolutionDropdown == null)
            return;

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.ClearOptions();
        availableResolutions.Clear();

        Resolution[] allResolutions = Screen.resolutions;
        HashSet<string> usedOptions = new HashSet<string>();
        List<string> options = new List<string>();

        foreach (var res in allResolutions)
        {
            string option = $"{res.width} x {res.height}";
            if (usedOptions.Add(option))
            {
                availableResolutions.Add(res);
                options.Add(option);
            }
        }

        resolutionDropdown.AddOptions(options);

        // Fullscreen
        bool defaultFullscreen = true;
        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, defaultFullscreen ? 1 : 0) == 1;
        Screen.fullScreen = isFullscreen;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        }

        // Resolution index
        int savedIndex = PlayerPrefs.GetInt(PREF_RESOLUTION_INDEX, -1);
        if (savedIndex >= 0 && savedIndex < availableResolutions.Count)
        {
            currentResolutionIndex = savedIndex;
        }
        else
        {
            currentResolutionIndex = 0;
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                var res = availableResolutions[i];
                if (res.width == Screen.width && res.height == Screen.height)
                {
                    currentResolutionIndex = i;
                    break;
                }
            }
        }

        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        ApplyResolution(currentResolutionIndex, Screen.fullScreen);

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    public void OnResolutionChanged(int index)
    {
        currentResolutionIndex = index;
        ApplyResolution(index, Screen.fullScreen);

        PlayerPrefs.SetInt(PREF_RESOLUTION_INDEX, index);
        PlayerPrefs.Save();
    }

    public void OnFullscreenToggleChanged(bool isFullscreen)
    {
        ApplyResolution(currentResolutionIndex, isFullscreen);

        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyResolution(int index, bool fullscreen)
    {
        if (index < 0 || index >= availableResolutions.Count)
            return;

        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, fullscreen);
    }

    #endregion

    #region Audio Settings (Master Volume)

    private void InitAudioSettings()
    {
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PREF_MASTER_VOLUME, 1.0f));

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.value = savedVolume;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        ApplyMasterVolume(savedVolume);
    }

    public void OnMasterVolumeChanged(float value)
    {
        float clamped = Mathf.Clamp01(value);
        ApplyMasterVolume(clamped);

        PlayerPrefs.SetFloat(PREF_MASTER_VOLUME, clamped);
        PlayerPrefs.Save();
    }

    private void ApplyMasterVolume(float value)
    {
        AudioListener.volume = value;
    }

    #endregion

    #region Map Camera Settings (NEW)

    private void InitMapCameraSettings()
    {
        MapZoomSpeed = Mathf.Max(0f, PlayerPrefs.GetFloat(PREF_MAP_ZOOM_SPEED, defaultMapZoomSpeed));
        MapPanSpeed = Mathf.Max(0f, PlayerPrefs.GetFloat(PREF_MAP_PAN_SPEED, defaultMapPanSpeed));
        MapSnapDuration = Mathf.Max(0.01f, PlayerPrefs.GetFloat(PREF_MAP_SNAP_DURATION, defaultMapSnapDuration));

        float rx = PlayerPrefs.GetFloat(PREF_MAP_RESET_X, defaultMapResetPosition.x);
        float ry = PlayerPrefs.GetFloat(PREF_MAP_RESET_Y, defaultMapResetPosition.y);
        float rz = PlayerPrefs.GetFloat(PREF_MAP_RESET_Z, defaultMapResetPosition.z);
        MapResetPosition = new Vector3(rx, ry, rz);

        MapResetZoom = Mathf.Max(0f, PlayerPrefs.GetFloat(PREF_MAP_RESET_ZOOM, defaultMapResetZoom));

        MapModifierZoomMultiplier = Mathf.Max(1f, PlayerPrefs.GetFloat(PREF_MAP_MOD_ZOOM_MULT, defaultMapModifierZoomMultiplier));

        MapModifier = (MapModifierKey)Mathf.Clamp(
            PlayerPrefs.GetInt(PREF_MAP_MODIFIER, (int)defaultMapModifier),
            0,
            Enum.GetValues(typeof(MapModifierKey)).Length - 1
        );

        MapPanButton = (MapPanMouseButton)Mathf.Clamp(
            PlayerPrefs.GetInt(PREF_MAP_PAN_BUTTON, (int)defaultMapPanButton),
            0,
            Enum.GetValues(typeof(MapPanMouseButton)).Length - 1
        );

        // Optional UI hookups
        WireSlider(mapZoomSpeedSlider, MapZoomSpeed, OnMapZoomSpeedChanged);
        WireSlider(mapPanSpeedSlider, MapPanSpeed, OnMapPanSpeedChanged);
        WireSlider(mapSnapDurationSlider, MapSnapDuration, OnMapSnapDurationChanged);
        WireSlider(mapResetZoomSlider, MapResetZoom, OnMapResetZoomChanged);
        WireSlider(mapModifierZoomMultiplierSlider, MapModifierZoomMultiplier, OnMapModifierZoomMultiplierChanged);

        WireDropdownEnum(mapModifierDropdown, typeof(MapModifierKey), (int)MapModifier, OnMapModifierChanged);
        WireDropdownEnum(mapPanButtonDropdown, typeof(MapPanMouseButton), (int)MapPanButton, OnMapPanButtonChanged);

        RaiseMapSettingsChanged();
    }

    private void WireSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null) return;
        slider.onValueChanged.RemoveAllListeners();
        slider.value = value;
        slider.onValueChanged.AddListener(onChanged);
    }

    private void WireDropdownEnum(TMP_Dropdown dropdown, Type enumType, int value, UnityEngine.Events.UnityAction<int> onChanged)
    {
        if (dropdown == null) return;

        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();

        var names = new List<string>(Enum.GetNames(enumType));
        dropdown.AddOptions(names);

        dropdown.value = value;
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(onChanged);
    }

    private void SaveMapPrefs()
    {
        PlayerPrefs.SetFloat(PREF_MAP_ZOOM_SPEED, MapZoomSpeed);
        PlayerPrefs.SetFloat(PREF_MAP_PAN_SPEED, MapPanSpeed);
        PlayerPrefs.SetFloat(PREF_MAP_SNAP_DURATION, MapSnapDuration);

        PlayerPrefs.SetFloat(PREF_MAP_RESET_X, MapResetPosition.x);
        PlayerPrefs.SetFloat(PREF_MAP_RESET_Y, MapResetPosition.y);
        PlayerPrefs.SetFloat(PREF_MAP_RESET_Z, MapResetPosition.z);

        PlayerPrefs.SetFloat(PREF_MAP_RESET_ZOOM, MapResetZoom);

        PlayerPrefs.SetFloat(PREF_MAP_MOD_ZOOM_MULT, MapModifierZoomMultiplier);

        PlayerPrefs.SetInt(PREF_MAP_MODIFIER, (int)MapModifier);
        PlayerPrefs.SetInt(PREF_MAP_PAN_BUTTON, (int)MapPanButton);

        PlayerPrefs.Save();
    }

    private void RaiseMapSettingsChanged()
    {
        MapSettingsChanged?.Invoke();
    }

    // Public API for UI or other scripts
    public void OnMapZoomSpeedChanged(float value)
    {
        MapZoomSpeed = Mathf.Max(0f, value);
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    public void OnMapPanSpeedChanged(float value)
    {
        MapPanSpeed = Mathf.Max(0f, value);
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    public void OnMapSnapDurationChanged(float value)
    {
        MapSnapDuration = Mathf.Max(0.01f, value);
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    public void OnMapResetZoomChanged(float value)
    {
        MapResetZoom = Mathf.Max(0f, value);
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    public void OnMapModifierZoomMultiplierChanged(float value)
    {
        MapModifierZoomMultiplier = Mathf.Max(1f, value);
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    public void OnMapModifierChanged(int enumIndex)
    {
        MapModifier = (MapModifierKey)Mathf.Clamp(enumIndex, 0, Enum.GetValues(typeof(MapModifierKey)).Length - 1);
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    public void OnMapPanButtonChanged(int enumIndex)
    {
        MapPanButton = (MapPanMouseButton)Mathf.Clamp(enumIndex, 0, Enum.GetValues(typeof(MapPanMouseButton)).Length - 1);
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    public void SetMapResetPosition(Vector3 value)
    {
        MapResetPosition = value;
        SaveMapPrefs();
        RaiseMapSettingsChanged();
    }

    #endregion

    #region Music Player & Music Menu

    private void InitMusicPlayer()
    {
        if (musicSource == null)
        {
            Debug.LogWarning("SettingsMenuController: No music AudioSource assigned.");
            UpdateMusicUI(null);
            return;
        }

        if (musicPlaylist == null || musicPlaylist.Length == 0)
        {
            UpdateMusicUI(null);
            return;
        }

        int savedTrackIndex = Mathf.Clamp(PlayerPrefs.GetInt(PREF_MUSIC_TRACK_INDEX, 0), 0, musicPlaylist.Length - 1);
        float savedTrackTime = Mathf.Max(0f, PlayerPrefs.GetFloat(PREF_MUSIC_TIME, 0f));

        musicSource.clip = musicPlaylist[savedTrackIndex];
        if (musicSource.clip != null)
            musicSource.time = Mathf.Clamp(savedTrackTime, 0f, musicSource.clip.length);

        if (autoPlayMusicOnStart)
        {
            musicSource.Play();
            isUserPaused = false;
        }
        else
        {
            isUserPaused = true;
        }

        UpdateMusicUI(musicSource.clip);
        UpdatePlayPauseLabel();
    }

    public void ToggleMusicMenu()
    {
        if (musicMenuPanel == null)
            return;

        isMusicMenuOpen = !musicMenuPanel.activeSelf;
        musicMenuPanel.SetActive(isMusicMenuOpen);
    }

    public void OnMusicPlayPauseButton()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        if (musicSource.isPlaying && !isUserPaused)
        {
            musicSource.Pause();
            isUserPaused = true;
        }
        else
        {
            musicSource.Play();
            isUserPaused = false;
        }

        UpdatePlayPauseLabel();
    }

    public void OnMusicNextButton()
    {
        if (musicSource == null || musicPlaylist == null || musicPlaylist.Length == 0)
            return;

        int currentIndex = GetCurrentTrackIndex();
        int nextIndex = (currentIndex + 1) % musicPlaylist.Length;
        PlayTrack(nextIndex);
    }

    public void OnMusicPreviousButton()
    {
        if (musicSource == null || musicPlaylist == null || musicPlaylist.Length == 0)
            return;

        int currentIndex = GetCurrentTrackIndex();
        int prevIndex = (currentIndex - 1 + musicPlaylist.Length) % musicPlaylist.Length;
        PlayTrack(prevIndex);
    }

    private void PlayTrack(int index)
    {
        if (musicSource == null || musicPlaylist == null || musicPlaylist.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, musicPlaylist.Length - 1);

        musicSource.clip = musicPlaylist[index];
        musicSource.time = 0f;
        musicSource.Play();
        isUserPaused = false;

        PlayerPrefs.SetInt(PREF_MUSIC_TRACK_INDEX, index);
        PlayerPrefs.SetFloat(PREF_MUSIC_TIME, 0f);
        PlayerPrefs.Save();

        UpdateMusicUI(musicSource.clip);
        UpdatePlayPauseLabel();
    }

    private int GetCurrentTrackIndex()
    {
        if (musicSource == null || musicPlaylist == null || musicPlaylist.Length == 0)
            return 0;

        AudioClip currentClip = musicSource.clip;
        if (currentClip == null)
            return 0;

        for (int i = 0; i < musicPlaylist.Length; i++)
            if (musicPlaylist[i] == currentClip)
                return i;

        return 0;
    }

    private void AutoAdvanceTrackIfFinished()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        if (isUserPaused)
            return;

        if (!musicSource.isPlaying && musicSource.time >= musicSource.clip.length - 0.05f)
            OnMusicNextButton();
    }

    private void UpdateMusicTimerUI()
    {
        if (musicSource == null || musicSource.clip == null || currentTrackTimeText == null)
            return;

        string currentTimeStr = FormatTime(musicSource.time);
        string totalTimeStr = FormatTime(musicSource.clip.length);

        currentTrackTimeText.text = $"{currentTimeStr} / {totalTimeStr}";
    }

    private void ThrottledSaveMusicProgress()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        _prefsSaveTimer += Time.unscaledDeltaTime;
        if (_prefsSaveTimer < prefsSaveIntervalSeconds)
            return;

        _prefsSaveTimer = 0f;

        PlayerPrefs.SetInt(PREF_MUSIC_TRACK_INDEX, GetCurrentTrackIndex());
        PlayerPrefs.SetFloat(PREF_MUSIC_TIME, musicSource.time);
        PlayerPrefs.Save();
    }

    private void UpdateMusicUI(AudioClip clip)
    {
        if (currentTrackNameText != null)
            currentTrackNameText.text = (clip != null) ? clip.name : "No Track";

        if (currentTrackTimeText != null)
            currentTrackTimeText.text = (clip != null)
                ? $"{FormatTime(0f)} / {FormatTime(clip.length)}"
                : "00:00 / 00:00";
    }

    private void UpdatePlayPauseLabel()
    {
        if (playPauseButtonLabel == null)
            return;

        bool playing = (musicSource != null && musicSource.isPlaying && !isUserPaused);
        playPauseButtonLabel.text = playing ? "Pause" : "Play";
    }

    private string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int totalSeconds = Mathf.FloorToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes:00}:{secs:00}";
    }

    #endregion
}
