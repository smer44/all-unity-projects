using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialougeUI : MonoBehaviour
{
    [SerializeField] private TMP_Text who;
    [SerializeField] private GameObject whoPanel;
    [SerializeField] private TMP_Text what;
    [SerializeField] private GameObject whatPanel;
    [SerializeField] private Image whoImage;
    [SerializeField] private GameObject whoImagePanel;

    [SerializeField] private GameObject choiceButtons;
    [SerializeField] private Button choiceButtonPrefab;

    public bool CanCreateChoiceButtons => choiceButtons != null && choiceButtonPrefab != null;

    public void ApplyWho(string text)
    {
        bool hasWho = !string.IsNullOrEmpty(text);
        if (whoPanel != null)
            whoPanel.SetActive(hasWho);

        if (who != null)
            who.text = hasWho ? text : string.Empty;
    }

    public void ApplyWhat(string text)
    {
        bool hasWhat = !string.IsNullOrEmpty(text);
        if (whatPanel != null)
            whatPanel.SetActive(hasWhat);

        if (what != null)
            what.text = hasWhat ? text : string.Empty;
    }

    public void ApplyImage(Sprite image)
    {
        bool hasImage = image != null;
        if (whoImagePanel != null)
            whoImagePanel.SetActive(hasImage);

        if (whoImage == null)
            return;

        whoImage.sprite = image;
        whoImage.preserveAspect = true;
    }

    public void SetChoicesButtonsActive(bool isActive)
    {
        if (choiceButtons != null)
            choiceButtons.SetActive(isActive);
    }

    public Button CreateChoiceButton()
    {
        if (!CanCreateChoiceButtons)
            return null;

        return Instantiate(choiceButtonPrefab, choiceButtons.transform);
    }

    public void ClearChoiceButtons()
    {
        if (choiceButtons == null)
            return;

        var parent = choiceButtons.transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    public void HideAll()
    {
        ApplyWho(string.Empty);
        ApplyWhat(string.Empty);
        ApplyImage(null);
        SetChoicesButtonsActive(false);
        ClearChoiceButtons();
    }
}
