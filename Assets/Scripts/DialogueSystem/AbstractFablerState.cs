using System;
using UnityEngine;


public class FablerState : MonoBehaviour
{
    public virtual void OnEnter(Fabler fabler) { }

    public virtual void EnterEntry(Fabler fabler, AbstractDialogueEntry entry) { }
    public virtual void OnExit(Fabler fabler)
    {
        //EnterEntry(null);
    }
    public virtual void OnUpdate(Fabler fabler) { }
}
