using UnityEngine;
using UnityEngine.InputSystem;


public class MouseFirstClickState : MouseStateBase
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ProductionNodePanelUI productionNodePanelUI;
    [SerializeField] private StoragePanelUI storagePanelUI;

    [SerializeField] private ActiveRecipePanelUI activeRecipePanelUI;

    [SerializeField] private StorageTransferPanelUI storageTransferPanelUI;


    public override string DisplayName()
    {
        return "MouseFirstClickState";
    }

    public override void Enter()
    {
        mouseManager.TransportFrom = null;
        mouseManager.Package = null;
        HideAllPanels();
    }

    private void Update()
    {
        //if (!Input.GetMouseButtonDown(0))
        //    return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Enter();
            return;
        }

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        //Input.mousePosition
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        //Ray ray = mouseManager.MainCamera.ScreenPointToRay(mousePosition);


        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log($"MouseFirstClickState : hit {hit.collider}");
            ProductionNode productionNode = hit.collider.GetComponentInParent<ProductionNode>();
            Storage storage = hit.collider.GetComponentInParent<Storage>();
            //Debug.Log($"MouseFirstClickState : hit productionNode {productionNode}");
            //Debug.Log($"MouseFirstClickState : hit storage {storage}");

            if (productionNode != null)
            {
                productionNodePanelUI.ShowNode(productionNode);
                activeRecipePanelUI.ShowNode(productionNode);
            }


            else
            {
                productionNodePanelUI.Hide();
                activeRecipePanelUI.Hide();
            }


            if (storage != null)
            {
                storagePanelUI.ShowNode(storage);
                storageTransferPanelUI.ShowNode(storage);
            }

            else
            {
                storagePanelUI.Hide();
                storageTransferPanelUI.Hide();
            }

        }
    }

    public void HideAllPanels()
    {
        productionNodePanelUI.Hide();
        storagePanelUI.Hide();
        activeRecipePanelUI.Hide();
        storageTransferPanelUI.Hide();
    }
}