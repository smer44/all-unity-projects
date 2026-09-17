using UnityEngine;
using UnityEngine.InputSystem;

public class MouseDestinationClickState : MouseStateBase
{
    [Header("Controls")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TransportController transportController;


    [Header("UI")]
    [SerializeField] private ProductionNodePanelUI productionNodePanelUI;
    [SerializeField] private StoragePanelUI storagePanelUI;
    [SerializeField] private ActiveRecipePanelUI activeRecipePanelUI;

    public override string DisplayName()
    {
        return "MouseDestinationClickState";
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            mouseManager.ChangeState(mouseManager.mouseFistClickState);
            return;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            mouseManager.ChangeState(mouseManager.mouseFistClickState);
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;


        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Storage targetStorage = hit.collider.GetComponentInParent<Storage>();

        if (targetStorage == null)
            return;

        //Click on the same storage is ignored
        if (targetStorage == mouseManager.TransportFrom)
            return;

        bool sent = transportController.Send(
            mouseManager.TransportFrom,
            targetStorage,
            mouseManager.Package);
        Debug.Log($"MouseDestinationClickState sent : {sent}");

        //If not able to send, click is ignored
        if (sent)
        {
            mouseManager.ChangeState(mouseManager.mouseFistClickState);
        }
        else
        {
            
            
        }

        
        
    }


}