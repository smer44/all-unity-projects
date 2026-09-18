using System.Collections.Generic;
using UnityEngine;

public class ChildConstructorUI : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private GameObject childPrefab;
    [SerializeField] private ChildConstructorUISupplier supplier;

    private readonly List<GameObject> spawnedChildren = new List<GameObject>();

    private void Start()
    {
        ConstructChildren();
    }

    public GameObject GetChild(int n)
    {
        if (n < 0 || n >= spawnedChildren.Count)
            return null;

        return spawnedChildren[n];
    }

    public void SetValue(int n, string text)
    {
        GameObject child = GetChild(n);
        if (child == null)
            return;

        ValueUI valueUI = child.GetComponent<ValueUI>();
        if (valueUI == null || valueUI.valueText == null)
            return;

        valueUI.valueText.text = text;
    }

    public void SetNamedValue(int n, NamedValue namedValue)
    {
        GameObject child = GetChild(n);
        if (child == null)
            return;

        NamedValueUI namedValueUI = child.GetComponent<NamedValueUI>();
        if (namedValueUI == null)
            return;

        namedValueUI.namedValue = namedValue;
    }

    private void ConstructChildren()
    {
        if (root == null)
            root = transform as RectTransform;

        if (supplier == null)
            supplier = GetComponent<ChildConstructorUISupplier>();

        NamedValues namedValues = supplier != null ? supplier.GetNamedValues() : null;

        if (root == null || childPrefab == null || namedValues == null)
            return;

        ClearRoot();

        for (int i = 0; i < namedValues.Count; i++)
        {
            NamedValue namedValue = namedValues.GetNamedValueAt(i);
            if (namedValue == null)
                continue;

            GameObject child = Instantiate(childPrefab, root);
            spawnedChildren.Add(child);
            SetNamedValue(spawnedChildren.Count - 1, namedValue);
        }
    }

    private void ClearRoot()
    {
        spawnedChildren.Clear();

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
