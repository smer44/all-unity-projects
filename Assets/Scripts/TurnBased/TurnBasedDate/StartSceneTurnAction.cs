using UnityEngine;
using UnityEngine.SceneManagement;

//[CreateAssetMenu(fileName = "StartSceneTurnAction", menuName = "Scriptable Objects/StartSceneTurnAction")]
[CreateAssetMenu(fileName = "AbstractTurnAction", menuName = "Turn Based/StartSceneTurnAction")]
public class StartSceneTurnAction : AbstractTurnAction
{

    public string sceneName;

    public override void Execute()
    {
        SceneManager.LoadScene(sceneName);
    }

}
