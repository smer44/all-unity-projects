using UnityEngine;
using UnityEngine.InputSystem;


public class PauseManager : MonoBehaviour
{

    [SerializeField] private GameObject[] disableOnUnpause;
    [SerializeField] private GameObject[] disableOnPause;
    [SerializeField] private GameObject[] enableOnUnpause;
    [SerializeField] private GameObject[] enableOnPause;

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

    public void SetActiveAdditionalObjects(bool active, GameObject[] objs){
        foreach (var obj in objs)
        {   
            if (obj != null){
                obj.SetActive(active);
            }
            
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
        SetActiveAdditionalObjects(false,disableOnPause);
        SetActiveAdditionalObjects(true,enableOnPause);
        Time.timeScale = 0f;


        MouseVisibilityController.Instance.BackupCursor();
        MouseVisibilityController.Instance.ActivateCursor();
        isPaused = true;
    }

    public void ResumeGame()
    {
        SetActiveAdditionalObjects(false,disableOnUnpause);
        SetActiveAdditionalObjects(true,enableOnUnpause);
        Time.timeScale = 1f;


        MouseVisibilityController.Instance.RestoreCursor();
        isPaused = false;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

