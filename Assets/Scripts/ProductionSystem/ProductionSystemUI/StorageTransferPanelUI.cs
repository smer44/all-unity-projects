using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageTransferPanelUI : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TextMeshProUGUI itemLabelPrefab;
    [SerializeField] private Button sendButtonPrefab;
    [SerializeField] private MouseManager mouseManager;

    private Storage currentStorage;

    public void ShowNode(Storage storage)
    {
        currentStorage = storage;
        Refresh();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        //Refresh();
    }

    public void Hide()
    {
        currentStorage = null;
        ClearRows();
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        ClearRows();

        if (currentStorage == null)
            return;

        string text = $"Stored: {currentStorage.StoredItems.Count}";
        StoragePanelUI.ConstructStoredItemLabel(itemLabelPrefab, root, text);

        for (int i = 0; i < currentStorage.StoredItems.Count; i++)
        {
            AmountOf<ItemDefinition> entry = currentStorage.StoredItems[i];
            string labelText = $"{entry.Item.DisplayName}: {entry.Amount:0.##}";

            TextMeshProUGUI labelInstance = Instantiate(itemLabelPrefab, root, false);

            UIGameObjectFactory.CreateTransferLabel(root, labelInstance, labelText);

            Button buttonInstance = Instantiate(sendButtonPrefab, root, false);
            
            UIGameObjectFactory.CreateTransferButton(
                root,
                buttonInstance,
                "Send",
                () => mouseManager.BeginTransportSelection(currentStorage, entry));
            
        }
    }

    public static GameObject ConstructTransferRowOld(
        RectTransform root,
        TextMeshProUGUI labelPrefab,
        Button buttonPrefab,
        MouseManager mouseManager,
        Storage storage,
        AmountOf<ItemDefinition> entry)
    {
        GameObject row = new GameObject("TransferRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(root, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 1f;

        TextMeshProUGUI label = Instantiate(labelPrefab, row.transform);
        LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.minWidth = 150f;
        labelLayout.flexibleWidth = 1f;


        label.text = $"{entry.Item.DisplayName}: {entry.Amount:0.##}";
        //label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;


        Button sendButton = Instantiate(buttonPrefab, row.transform);
        LayoutElement buttonLayout = label.gameObject.AddComponent<LayoutElement>();
        buttonLayout.minWidth = 80;
        buttonLayout.preferredWidth = 80;
        buttonLayout.flexibleWidth = 0f;        

    
        TextMeshProUGUI buttonLabel = sendButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonLabel != null)
            buttonLabel.text = "Send";

        AmountOf<ItemDefinition> package = new AmountOf<ItemDefinition>
        {
            Item = entry.Item,
            Amount = entry.Amount
        };

        sendButton.onClick.RemoveAllListeners();
        sendButton.onClick.AddListener(() =>
        {
            mouseManager.BeginTransportSelection(storage, package);
        });

        return row;
    }

    private void ClearRows()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }
}