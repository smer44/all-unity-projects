using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStarter : MonoBehaviour
{
    

    public void StartScene(string sceneName)
    {        
        SceneManager.LoadScene(sceneName);
    }
}
