using TMPro;
using UnityEngine;

public class StateNamePanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stateNameLabel;

    public void SetStateName(string stateName)
    {
        stateNameLabel.text = stateName;
    }
}