using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProductionGraphControllerUI : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField] private ProductionGraphController productionGraphController;

    [Header("Current Goals View")]
    [SerializeField] private RectTransform goalsRoot;
    [SerializeField] private TextMeshProUGUI goalLabelPrefab;

    [Header("New Goal Controls")]
    [SerializeField] private TMP_Dropdown itemTypeDropdown;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TextMeshProUGUI amountLabel;
    [SerializeField] private Button makeButton;

    private void Awake()
    {
        PopulateItemTypeDropdown();
        ConfigureAmountSlider();
        BindControls();
        UpdateAmountLabel();
    }

    private void Update()
    {
        RefreshGoals();
    }

    private void PopulateItemTypeDropdown()
    {
        itemTypeDropdown.ClearOptions();

        string[] enumNames = Enum.GetNames(typeof(ItemType));
        itemTypeDropdown.AddOptions(new System.Collections.Generic.List<string>(enumNames));
    }

    private void ConfigureAmountSlider()
    {
        amountSlider.minValue = 1f;
        amountSlider.maxValue = 100f;
        amountSlider.wholeNumbers = true;
    }

    private void BindControls()
    {
        amountSlider.onValueChanged.RemoveAllListeners();
        amountSlider.onValueChanged.AddListener(_ => UpdateAmountLabel());

        makeButton.onClick.RemoveAllListeners();
        makeButton.onClick.AddListener(OnMakeClicked);
    }

    private void OnMakeClicked()
    {
        ItemType selectedItemType = (ItemType)itemTypeDropdown.value;
        Debug.Log($"OnMakeClicked: selectedItemType {selectedItemType}");
        if (selectedItemType == ItemType.None)
        {
            return;
        }
        int selectedAmount = Mathf.RoundToInt(amountSlider.value);

        productionGraphController.AddGoal(selectedItemType, selectedAmount);
        RefreshGoals();
    }

    private void UpdateAmountLabel()
    {
        amountLabel.text = $"Amount: {Mathf.RoundToInt(amountSlider.value)}";
    }

    public void RefreshGoals()
    {
        ClearGoals();

        if (productionGraphController == null)
            return;

        string text = $"Goals: {productionGraphController.Goals.Count}";
        CreateGoalLabel(goalsRoot, goalLabelPrefab, text);

        for (int i = 0; i < productionGraphController.Goals.Count; i++)
        {
            AmountOf<ItemType> goal = productionGraphController.Goals[i];
            text = $"{goal.Item}: {goal.Amount}";
            CreateGoalLabel(goalsRoot, goalLabelPrefab, text);
        }
    }

    public static TextMeshProUGUI CreateGoalLabel(
        RectTransform root,
        TextMeshProUGUI labelPrefab,
        string labelText)
    {
        TextMeshProUGUI labelInstance = Instantiate(labelPrefab, root, false);
        labelInstance.text = labelText;
        labelInstance.textWrappingMode = TextWrappingModes.NoWrap;
        labelInstance.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layout = labelInstance.GetComponent<LayoutElement>();
        if (layout == null)
            layout = labelInstance.gameObject.AddComponent<LayoutElement>();

        layout.minHeight = 24f;
        layout.preferredHeight = 30f;
        layout.flexibleWidth = 1f;

        return labelInstance;
    }

    private void ClearGoals()
    {
        for (int i = goalsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(goalsRoot.GetChild(i).gameObject);
        }
    }
}
