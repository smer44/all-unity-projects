using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugSceneStarter : SceneStarter
{
    [SerializeField] private string sceneName;
    [SerializeField] private Key startKey = Key.Escape;
    [SerializeField] private bool allowRestartCurrentScene = false;


    public static DebugSceneStarter Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }      

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[startKey].wasPressedThisFrame)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(DebugSceneStarter)} on '{name}' has no scene name assigned.", this);
            return;
        }

        if (!allowRestartCurrentScene && SceneManager.GetActiveScene().name == sceneName)
        {
            return;
        }

        StartScene(sceneName);
    }
}
