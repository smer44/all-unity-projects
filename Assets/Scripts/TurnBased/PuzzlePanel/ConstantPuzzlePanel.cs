using System;
using System.Collections.Generic;
using UnityEngine;

public class ConstantPuzzlePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform tableu;

    [Header("A")]
    [SerializeField] private bool A_Circle_Left = true;
    [SerializeField] private bool A_Circle_Mid = true;
    [SerializeField] private bool A_Circle_Right;
    [SerializeField] private bool A_Corner_Left = true;
    [SerializeField] private bool A_Corner_Mid = true;
    [SerializeField] private bool A_Corner_Right = true;
    [SerializeField] private bool A_Wing_Left = false;
    [SerializeField] private bool A_Wing_Right= false;

    [Header("B")]
    [SerializeField] private bool B_Circle_Left = true;
    [SerializeField] private bool B_Circle_Mid = true;
    [SerializeField] private bool B_Circle_Right;
    [SerializeField] private bool B_Corner_Left = true;
    [SerializeField] private bool B_Corner_Mid = true;
    [SerializeField] private bool B_Corner_Right = true;
    [SerializeField] private bool B_Wing_Left= false;
    [SerializeField] private bool B_Wing_Right= false;

    [Header("C")]
    [SerializeField] private bool C_Circle_Left = true;
    [SerializeField] private bool C_Circle_Mid= false;
    [SerializeField] private bool C_Circle_Right = true;
    [SerializeField] private bool C_Corner_Left = true;
    [SerializeField] private bool C_Corner_Mid= false;
    [SerializeField] private bool C_Corner_Right = true;
    [SerializeField] private bool C_Wing_Left = true;
    [SerializeField] private bool C_Wing_Right = true;

    [Header("D")]
    [SerializeField] private bool D_East = true;
    [SerializeField] private bool D_North= false;
    [SerializeField] private bool D_South= false;
    [SerializeField] private bool D_West = true;

    [Header("E")]
    [SerializeField] private bool E_Top_Left = true;
    [SerializeField] private bool E_Bottom_Left = true;
    [SerializeField] private bool E_Top_Right= false;
    [SerializeField] private bool E_Bottom_Right= false;

    [Header("F")]
    [SerializeField] private bool F_Top= false;
    [SerializeField] private bool F_Bottom= false;

    [Header("G")]
    [SerializeField] private bool G_Top_Left= false;
    [SerializeField] private bool G_Bottom_Left= false;
    [SerializeField] private bool G_Top_Right = true;
    [SerializeField] private bool G_Bottom_Right = true;

    [NonSerialized, HideInInspector]
    private string[] Selection = Array.Empty<string>();

    private void Start()
    {
        Selection = BuildSelectionFromInspector();
        UpdateSelectionVisuals();
    }

    private string[] BuildSelectionFromInspector()
    {
        var selection = new List<string>(38);

        AddIfSelected(selection, A_Circle_Left, nameof(A_Circle_Left));
        AddIfSelected(selection, A_Circle_Mid, nameof(A_Circle_Mid));
        AddIfSelected(selection, A_Circle_Right, nameof(A_Circle_Right));
        AddIfSelected(selection, A_Corner_Left, nameof(A_Corner_Left));
        AddIfSelected(selection, A_Corner_Mid, nameof(A_Corner_Mid));
        AddIfSelected(selection, A_Corner_Right, nameof(A_Corner_Right));
        AddIfSelected(selection, A_Wing_Left, nameof(A_Wing_Left));
        AddIfSelected(selection, A_Wing_Right, nameof(A_Wing_Right));

        AddIfSelected(selection, B_Circle_Left, nameof(B_Circle_Left));
        AddIfSelected(selection, B_Circle_Mid, nameof(B_Circle_Mid));
        AddIfSelected(selection, B_Circle_Right, nameof(B_Circle_Right));
        AddIfSelected(selection, B_Corner_Left, nameof(B_Corner_Left));
        AddIfSelected(selection, B_Corner_Mid, nameof(B_Corner_Mid));
        AddIfSelected(selection, B_Corner_Right, nameof(B_Corner_Right));
        AddIfSelected(selection, B_Wing_Left, nameof(B_Wing_Left));
        AddIfSelected(selection, B_Wing_Right, nameof(B_Wing_Right));

        AddIfSelected(selection, C_Circle_Left, nameof(C_Circle_Left));
        AddIfSelected(selection, C_Circle_Mid, nameof(C_Circle_Mid));
        AddIfSelected(selection, C_Circle_Right, nameof(C_Circle_Right));
        AddIfSelected(selection, C_Corner_Left, nameof(C_Corner_Left));
        AddIfSelected(selection, C_Corner_Mid, nameof(C_Corner_Mid));
        AddIfSelected(selection, C_Corner_Right, nameof(C_Corner_Right));
        AddIfSelected(selection, C_Wing_Left, nameof(C_Wing_Left));
        AddIfSelected(selection, C_Wing_Right, nameof(C_Wing_Right));

        AddIfSelected(selection, D_East, nameof(D_East));
        AddIfSelected(selection, D_North, nameof(D_North));
        AddIfSelected(selection, D_South, nameof(D_South));
        AddIfSelected(selection, D_West, nameof(D_West));

        AddIfSelected(selection, E_Top_Left, nameof(E_Top_Left));
        AddIfSelected(selection, E_Bottom_Left, nameof(E_Bottom_Left));
        AddIfSelected(selection, E_Top_Right, nameof(E_Top_Right));
        AddIfSelected(selection, E_Bottom_Right, nameof(E_Bottom_Right));

        AddIfSelected(selection, F_Top, nameof(F_Top));
        AddIfSelected(selection, F_Bottom, nameof(F_Bottom));

        AddIfSelected(selection, G_Top_Left, nameof(G_Top_Left));
        AddIfSelected(selection, G_Bottom_Left, nameof(G_Bottom_Left));
        AddIfSelected(selection, G_Top_Right, nameof(G_Top_Right));
        AddIfSelected(selection, G_Bottom_Right, nameof(G_Bottom_Right));

        return selection.ToArray();
    }

    private static void AddIfSelected(List<string> selection, bool isSelected, string itemName)
    {
        if (isSelected)
        {
            selection.Add(itemName);
        }
    }

    private void UpdateSelectionVisuals()
    {
        if (tableu == null)
        {
            Debug.LogError("ConstantPuzzlePanel.UpdateSelectionVisuals: tableu reference is missing.");
            return;
        }

        for (int i = 0; i < tableu.childCount; i++)
        {
            var child = tableu.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < Selection.Length; i++)
        {
            var name = Selection[i];
            var child = tableu.Find(name);
            if (child == null)
            {
                Debug.LogError($"ConstantPuzzlePanel.UpdateSelectionVisuals: child '{name}' not found under tableu.");
                continue;
            }

            child.gameObject.SetActive(true);
        }
    }
}
