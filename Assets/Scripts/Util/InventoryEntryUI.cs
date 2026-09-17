using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class InventoryEntryUI : MonoBehaviour
{
    private Image entryImage;
    private InventoryEntry entry;

    public InventoryEntry Entry
    {
        get => entry;
        set
        {
            entry = value;
            if (entryImage == null)
                entryImage = GetComponent<Image>();

            entryImage.sprite = entry?.sprite;
            entryImage.preserveAspect = true;
        }
    }
}
