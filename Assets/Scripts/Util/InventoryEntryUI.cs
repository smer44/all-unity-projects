using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class InventoryEntryUI : MonoBehaviour
{
    private Image entryImage;
    private InventoryEntry entry;

    public InventoryEntry Entry => entry;

    public void SetInventoryEntry(InventoryEntry entry)
    {
        this.entry = entry;
        if (entryImage == null)
            entryImage = GetComponent<Image>();

        entryImage.sprite = entry != null ? entry.sprite : null;
        entryImage.preserveAspect = true;
    }
}
