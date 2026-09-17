using System;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class MemoryBehaviour : MonoBehaviour
{
    public static MemoryBehaviour Instance { get; private set; }
    public static event Action<string, int> MemoryChanged;

    [FormerlySerializedAs("storyContext")]
    [SerializeField] private StoryMemory storyMemory;

    public StoryMemory StoryMemory => storyMemory;

    public static int Get(string key)
    {
        return TryGet(key, out int value) ? value : 0;
    }

    public static void Set(string key, int value)
    {
        if (!CheckNulls(key))
        {
            return;
        }

        var dict = Instance.StoryMemory.GetMemory();
        if (dict.TryGetValue(key, out int existingValue) && existingValue == value)
        {
            return;
        }

        dict[key] = value;
        NotifyMemoryChanged(key, value);

    }

    public static void Add(string key, int change)
    {
        if (!CheckNulls(key))
        {
            return;
        }
        var value = 0;
        var dict = Instance.StoryMemory.GetMemory();
        if(dict.TryGetValue(key, out value))
        {
            int newValue = value + change;
            if (newValue == value)
            {
                return;
            }

            dict[key] = newValue;
            NotifyMemoryChanged(key, newValue);
            return;
        }

        dict[key] = change;
        NotifyMemoryChanged(key, change);

    }

    private static void NotifyMemoryChanged(string key, int value)
    {
        MemoryChanged?.Invoke(key, value);
    }

    public static void LogMemoryContents()
    {
        if (Instance == null)
        {
            Debug.LogWarning($"{nameof(MemoryBehaviour)} cannot log memory before the singleton was initialized.");
            return;
        }

        if (Instance.StoryMemory == null)
        {
            Debug.LogWarning($"{nameof(MemoryBehaviour)} has no {nameof(StoryMemory)} assigned.", Instance);
            return;
        }

        var dict = Instance.StoryMemory.GetMemory();
        if (dict.Count == 0)
        {
            Debug.Log($"{nameof(MemoryBehaviour)} is empty.", Instance);
            return;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"{nameof(MemoryBehaviour)} contents:");

        foreach (var pair in dict)
        {
            builder.AppendLine($"{pair.Key}: {pair.Value}");
        }

        Debug.Log(builder.ToString(), Instance);
    }


    public static bool CheckNulls(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning($"{nameof(MemoryBehaviour)} lookup requested with an empty key.");
            return false;
        }

        if (Instance == null)
        {
            Debug.LogWarning($"{nameof(MemoryBehaviour)} lookup requested before the singleton was initialized.");
            return false;
        }

        if (Instance.StoryMemory == null)
        {
            Debug.LogWarning($"{nameof(MemoryBehaviour)} has no {nameof(StoryMemory)} assigned.", Instance);
            return false;
        }
        return true;        


    }


    public static bool TryGet(string key, out int value)
    {   
        value = 0;

        if (!CheckNulls(key))
        {
            return false;
        }        

        var dict = Instance.StoryMemory.GetMemory();

        return dict.TryGetValue(key, out value);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"{nameof(MemoryBehaviour)} duplicate on '{name}' will be destroyed because singleton instance already exists on '{Instance.name}'.",
                this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
        {
            Debug.LogWarning(
                $"{nameof(MemoryBehaviour)} on '{name}' is not a scene root object. {nameof(DontDestroyOnLoad)} only preserves root GameObjects.",
                this);
        }

        var sceneMemoryBehaviours = FindObjectsByType<MemoryBehaviour>(FindObjectsInactive.Exclude);
        foreach (var memoryBehaviour in sceneMemoryBehaviours)
        {
            if (memoryBehaviour != this && memoryBehaviour.gameObject.scene == gameObject.scene)
            {
                Debug.LogWarning(
                    $"{nameof(MemoryBehaviour)} on '{name}' found another {nameof(MemoryBehaviour)} on '{memoryBehaviour.name}' in the same scene before {nameof(DontDestroyOnLoad)}.",
                    this);
                break;
            }
        }

        DontDestroyOnLoad(gameObject);

        if (storyMemory == null)
        {
            storyMemory = ScriptableObject.CreateInstance<StoryMemory>();
            storyMemory.name = nameof(StoryMemory);
            return;
        }

        var sourceStoryMemory = storyMemory;
        Debug.Log("Instantiate(sourceStoryMemory");
        storyMemory = Instantiate(sourceStoryMemory);
        storyMemory.name = $"{sourceStoryMemory.name} Runtime";
        Debug.Log("MemoryBehaviour.Awake");
        //LogMemoryContents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
