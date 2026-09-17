using UnityEngine;

public class ActiveGameObjectSwitch : MonoBehaviour
{
    [SerializeField] private GameObject[] gameObjects;

    private int currentIndex = -1;

    [SerializeField] private int startingIndex;

    void Start()
    {
        if (gameObjects == null || gameObjects.Length == 0)
            return;

        //for (int i = 0; i < gameObjects.Length; i++)
        //{
        //    if (gameObjects[i] != null)
        //        gameObjects[i].SetActive(i == 0);
        //}

        SwitchActive(startingIndex);

    }

    public int GetActiveIndex()
    {
        return currentIndex;
    }

    protected int GetGameObjectCount()
    {
        return gameObjects != null ? gameObjects.Length : 0;
    }

    public void SwitchActive(int activeIndex)
    {
        if (gameObjects == null || activeIndex < 0 || activeIndex >= gameObjects.Length)
            return;

        if (activeIndex == currentIndex)
            return;

        GameObject activeObject = gameObjects[activeIndex];

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] != null)
                gameObjects[i].SetActive(i == activeIndex);
        }

        currentIndex = activeIndex;

        if (activeObject == null)
            return;

        //UIUpdatable[] updatables = activeObject.GetComponentsInChildren<UIUpdatable>(true);

        //for (int i = 0; i < updatables.Length; i++)
        //{
        //    if (updatables[i] != null)
        //        updatables[i].UpdateUI();
        //}
    }
}
