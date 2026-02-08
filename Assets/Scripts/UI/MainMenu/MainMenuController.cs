using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;  // New Input System

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Core";
    [SerializeField] private string worldMapSceneName = "MainMap";

    [Header("Panels")]
    [Tooltip("Root panel for the main menu UI (buttons, background, etc).")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("References")]
    [SerializeField] private SettingsMenuController settingsMenu;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;

    private bool isPaused = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (settingsMenu != null)
            settingsMenu.HideSettingsImmediate();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Ensure the correct initial state based on the active scene.
        ApplyMainMenuPanelState(SceneManager.GetActiveScene().name);

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void Update()
    {
        // ESC toggles the pause menu (New Input System)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePauseMenu();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMainMenuPanelState(scene.name);

        // Always come out of pause on scene changes
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void ApplyMainMenuPanelState(string loadedSceneName)
    {
        if (mainMenuPanel == null)
            return;

        if (loadedSceneName == mainMenuSceneName)
        {
            mainMenuPanel.SetActive(true);
        }
        else if (loadedSceneName == worldMapSceneName)
        {
            mainMenuPanel.SetActive(false);
        }
    }

    #region Main Menu Buttons

    // Called by Start button
    public void OnStartButtonPressed()
    {
        // No fading/transition manager: direct load only.
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (!string.IsNullOrEmpty(worldMapSceneName))
            SceneManager.LoadScene(worldMapSceneName);
    }

    // Called by Settings button
    public void OnSettingsButtonPressed()
    {
        if (settingsMenu != null)
            settingsMenu.ShowSettings();
        else
            Debug.LogWarning("MainMenuController: SettingsMenu reference is not set.");
    }

    // Called by Close/Back button inside the Settings panel
    public void OnSettingsCloseButtonPressed()
    {
        if (settingsMenu != null)
            settingsMenu.HideSettings();
    }

    // Optional: wire this to a "Main Menu" button in your world map UI if desired
    public void OnReturnToMainMenuPressed()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }

    // Called by Exit/Quit button
    public void OnExitButtonPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region Pause Menu Logic

    public void TogglePauseMenu()
    {
        if (pauseMenuPanel == null)
        {
            Debug.LogWarning("MainMenuController: Pause menu panel is not assigned.");
            return;
        }

        if (isPaused)
            ResumeFromPause();
        else
            PauseGame();
    }

    public void OnResumeButtonPressed()
    {
        if (isPaused)
            TogglePauseMenu();
    }

    private void PauseGame()
    {
        isPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void ResumeFromPause()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    #endregion
}
