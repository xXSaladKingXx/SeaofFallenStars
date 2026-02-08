using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionDropdownController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private List<Resolution> availableResolutions = new List<Resolution>();
    private int currentResolutionIndex = 0;

    private const string PREF_RESOLUTION_INDEX = "ResolutionIndex";
    private const string PREF_FULLSCREEN = "Fullscreen";

    private void Awake()
    {
        if (resolutionDropdown == null)
        {
            resolutionDropdown = GetComponent<Dropdown>();
        }

        PopulateResolutionOptions();
        LoadSavedResolutionSettings();
    }

    private void PopulateResolutionOptions()
    {
        resolutionDropdown.ClearOptions();
        availableResolutions.Clear();

        Resolution[] allResolutions = Screen.resolutions;
        HashSet<string> usedOptions = new HashSet<string>();

        List<string> options = new List<string>();

        // Filter unique width x height combinations
        foreach (var res in allResolutions)
        {
            string option = $"{res.width} x {res.height}";
            if (!usedOptions.Contains(option))
            {
                usedOptions.Add(option);
                availableResolutions.Add(res);
                options.Add(option);
            }
        }

        resolutionDropdown.AddOptions(options);
    }

    private void LoadSavedResolutionSettings()
    {
        // Fullscreen
        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;
        Screen.fullScreen = isFullscreen;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        }

        // Resolution
        int savedIndex = PlayerPrefs.GetInt(PREF_RESOLUTION_INDEX, -1);

        if (savedIndex >= 0 && savedIndex < availableResolutions.Count)
        {
            currentResolutionIndex = savedIndex;
        }
        else
        {
            // Fallback: match current Screen size to nearest resolution
            Resolution current = Screen.currentResolution;
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

        // Link the dropdown callback
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    public void OnResolutionChanged(int index)
    {
        currentResolutionIndex = index;
        bool isFullscreen = Screen.fullScreen;
        ApplyResolution(index, isFullscreen);

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
}
