using TMPro;
using UnityEngine;

public class DailyActionsControllerDisplayer : MonoBehaviour
{

    [Header("Controller")]
    [SerializeField]
    private DailyActionsController controller;

    [Header("UI Components")]
    [SerializeField] private TMP_Text currentEnergyPointsText;
    [SerializeField] private TMP_Text maxEnergyPointsText;
    [SerializeField] private TMP_Text currentDateText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (controller == null)
            return;

        if (currentEnergyPointsText != null)
            currentEnergyPointsText.text = controller.CurrentEnergyPoints.ToString();

        if (maxEnergyPointsText != null)
            maxEnergyPointsText.text = controller.MaxEnergyPoints.ToString();

        if (currentDateText != null)
        {
            AbstractDate currentDate = MemoryBehaviour.Instance != null && MemoryBehaviour.Instance.StoryMemory != null
                ? MemoryBehaviour.Instance.StoryMemory.CurrentDate
                : null;
            currentDateText.text = currentDate != null ? currentDate.PP() : string.Empty;
        }
    }
}
