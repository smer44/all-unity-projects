using UnityEngine;
using UnityEngine.InputSystem;


public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Key pauseKey = Key.Escape;

    [SerializeField] private bool pausingAllowed = false;

    private bool isPaused;


    private void Start()
    {
        ResumeGame();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (pausingAllowed && keyboard != null && pauseKey != Key.None && keyboard[pauseKey].wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void SetPausingAllowed(bool allowed)
    {
        this.pausingAllowed = allowed;
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        Time.timeScale = 0f;


        MouseVisibilityController.Instance.BackupCursor();
        MouseVisibilityController.Instance.ActivateCursor();
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        Time.timeScale = 1f;


        MouseVisibilityController.Instance.RestoreCursor();
        isPaused = false;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}

