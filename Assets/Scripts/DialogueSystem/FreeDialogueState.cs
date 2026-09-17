using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[Serializable]
public sealed class FreeDialogueState : FablerState
{
    public bool getInteractibleWithMouse = false;
    public override void OnEnter(Fabler fabler)
    {
        var playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null)
            return;

        playerController.SetState(playerController.IdleState);
    }

    public override void OnUpdate(Fabler fabler)
    {
        if (!getInteractibleWithMouse)
            return;
    
        if (fabler == null)
            return;

        if (Mouse.current == null || EventSystem.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        var interactable = TryGetClickedInteractable();
        if (interactable == null)
            return;

        interactable.TryStartDialogue(fabler);
    }

    public override void EnterEntry(Fabler fabler, AbstractDialogueEntry entry)
    {

        if (entry != null)
        {
            fabler.ChangeState(fabler.dialogueState);
            fabler.Enter(entry);
        }
    }

    private static DialogueInteractable TryGetClickedInteractable()
    {
        var data = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        //Debug.Log($"FreeDialogueState : data : {data}, results.Count :{results.Count}");
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var obj = result.gameObject;

            //if (obj == null || !obj.CompareTag("Interactable"))
            if (obj == null)
                continue;
            var interactable = obj.GetComponent<DialogueInteractable>();
            if (interactable != null)
            {
                Debug.Log($"FreeDialogueState : interactable : {interactable.gameObject}");
                return interactable;
            }

        }

        return null;
    }

}
