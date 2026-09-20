using UnityEngine;

public class PrintoutInteractable : AbstractInteractable
{
    public override void OnEnter(PlayerController player)
    {
        Debug.Log($"{nameof(PrintoutInteractable)} on '{name}': {nameof(OnEnter)} called with player '{(player != null ? player.name : "null")}'.", this);
    }

    public override void OnExit()
    {
        Debug.Log($"{nameof(PrintoutInteractable)} on '{name}': {nameof(OnExit)} called.", this);
    }

    public override void KillInteraction()
    {
        Debug.Log($"{nameof(PrintoutInteractable)} on '{name}': {nameof(KillInteraction)} called.", this);
    }

    public override bool AllowsBreak()
    {
        Debug.Log($"{nameof(PrintoutInteractable)} on '{name}': {nameof(AllowsBreak)} called, returning true.", this);
        return true;
    }

    public override void OnFocusEnter()
    {
        Debug.Log($"{nameof(PrintoutInteractable)} on '{name}': {nameof(OnFocusEnter)} called.", this);
    }

    public override void OnFocusExit()
    {
        Debug.Log($"{nameof(PrintoutInteractable)} on '{name}': {nameof(OnFocusExit)} called.", this);
    }
}
