using UnityEngine;

public class MouseManager : MonoBehaviour
{

    [Header("States")]
    [SerializeField] public MouseFirstClickState mouseFistClickState;
    [SerializeField] public MouseDestinationClickState mouseDestinationClickState;


    [Header("UI")]
    [SerializeField] private StateNamePanelUI stateNamePanelUI;



    private MouseStateBase currentState;

    public Storage TransportFrom;
    public AmountOf<ItemDefinition> Package;


    private void Awake()
    {
        mouseFistClickState.Initialize(this);
        mouseDestinationClickState.Initialize(this);

        mouseFistClickState.enabled = false;
        mouseDestinationClickState.enabled = false;

        ChangeState(mouseFistClickState);
    }


    public void ChangeState(MouseStateBase state)
    {   
        Debug.Log($"MouseManager : ChangeState {state}");

        if (currentState != null)
        {

            if (currentState != null)
                currentState.Exit();

            currentState.enabled = false;
        }

        currentState = state;
        

        if (currentState != null)
        {
            currentState.enabled = true;

            if (currentState != null)
                currentState.Enter();
        }

        if (currentState != null)
        {
            stateNamePanelUI.SetStateName(currentState.DisplayName());
        }
        else
        {
           stateNamePanelUI.SetStateName("~null~"); 
        }
            

    }

    public void BeginTransportSelection(Storage fro, AmountOf<ItemDefinition> package)
    {   
        //Debug.Log($"MouseManager : BeginTransportSelection {fro} , {package}");
        TransportFrom = fro;
        //mouseDestinationClickState.SetTransportPackage(package);
        Package = package == null
            ? null
            : new AmountOf<ItemDefinition>
            {
                Item = package.Item,
                Amount = package.Amount
            };
        ChangeState(mouseDestinationClickState);
    }


}
