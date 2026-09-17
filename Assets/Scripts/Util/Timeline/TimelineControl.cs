using UnityEngine;
using UnityEngine.Playables;

public class TimelineControl : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;

    public void PauseCutscene()
    {
        Debug.Log("TimelineControl. PauseCutscene");
        director.Pause();
    }

    public void ResumeCutscene()
    {   
        Debug.Log("TimelineControl. ResumeCutscene");
        director.Resume();
    }
}
