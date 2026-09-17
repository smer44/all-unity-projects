using TMPro;
using UnityEngine;

public class StoragePanelUI : MonoBehaviour
{
    [SerializeField] private RectTransform storedItemsParent;
    [SerializeField] private TextMeshProUGUI storedItemLabelPrefab;

    private Storage currentStorage;

    public void ShowNode(Storage storage)
    {
        currentStorage = storage;
        Refresh();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        Refresh();

    }

    public void Hide()
    {
        currentStorage = null;
        ClearLabels();
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        ClearLabels();

        if (currentStorage == null)
            return;


        string text = $"Stored: {currentStorage.StoredItems.Count}";
        ConstructStoredItemLabel(storedItemLabelPrefab, storedItemsParent, text);

        for (int i = 0; i < currentStorage.StoredItems.Count; i++)
        {
            AmountOf<ItemDefinition> entry = currentStorage.StoredItems[i];

            text = $"{entry.Item.DisplayName}: {entry.Amount:0.##}";
            ConstructStoredItemLabel(storedItemLabelPrefab, storedItemsParent, text);
        }
    }

    public static TextMeshProUGUI ConstructStoredItemLabel(
        TextMeshProUGUI labelPrefab,
        RectTransform parent,
        string text)
    {
        TextMeshProUGUI labelInstance = Instantiate(labelPrefab, parent);
        labelInstance.text = text;
        return labelInstance;
    }

    private void ClearLabels()
    {
        for (int i = storedItemsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(storedItemsParent.GetChild(i).gameObject);
        }
    }
}