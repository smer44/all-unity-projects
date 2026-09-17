using TMPro;
using UnityEngine;

public class ActiveRecipePanelUI : MonoBehaviour
{
    [SerializeField] private RectTransform activeRecipesParent;
    [SerializeField] private TextMeshProUGUI activeRecipeLabelPrefab;

    private ProductionNode currentNode;

    public void ShowNode(ProductionNode node)
    {
        currentNode = node;
        Refresh();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        Refresh();
    }

    public void Hide()
    {
        currentNode = null;
        ClearLabels();
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        ClearLabels();

        if (currentNode == null)
            return;

        string text = $"Work: {currentNode.Work.Count}";
        ConstructActiveRecipeLabel(activeRecipeLabelPrefab, activeRecipesParent, text);

        for (int i = 0; i < currentNode.Work.Count; i++)
        {
            RecipeExecution execution = currentNode.Work[i];
            text = execution.Recipe.DisplayName;
            ConstructActiveRecipeLabel(activeRecipeLabelPrefab, activeRecipesParent, text);
        }
    }

    public static TextMeshProUGUI ConstructActiveRecipeLabel(
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
        for (int i = activeRecipesParent.childCount - 1; i >= 0; i--)
        {
            Destroy(activeRecipesParent.GetChild(i).gameObject);
        }
    }
}