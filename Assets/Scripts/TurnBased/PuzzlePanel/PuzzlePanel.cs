using UnityEngine;
using System;
using System.Collections.Generic;


public class PuzzlePanel : MonoBehaviour
{   
    [Header("References")]
    [SerializeField] private Transform tableu;
    [SerializeField] private AbstractInteractable MainPanel;

    [NonSerialized, HideInInspector] 
    private string [] ItemNames = {
        "A_Circle_Left" ,
        "A_Circle_Mid",
        "A_Circle_Right",

        "A_Corner_Left",
        "A_Corner_Mid",
        "A_Corner_Right",

        "A_Wing_Left",
        "A_Wing_Right",

        "B_Circle_Left",
        "B_Circle_Mid",
        "B_Circle_Right",

        "B_Corner_Left",
        "B_Corner_Mid",
        "B_Corner_Right",

        "B_Wing_Left",
        "B_Wing_Right",

        "C_Circle_Left",
        "C_Circle_Mid",
        "C_Circle_Right",
        
        "C_Corner_Left",
        "C_Corner_Mid",
        "C_Corner_Right",

        "C_Wing_Left",
        "C_Wing_Right",
        
        "D_East",
        "D_North",
        "D_South",
        "D_West",
        
        "E_Top_Left",
        "E_Bottom_Left",
        "E_Top_Right",
        "E_Bottom_Right",
        
        
        
        "F_Top",
        "F_Bottom",
        
        "G_Top_Left",
        "G_Bottom_Left",
        "G_Top_Right",
        "G_Bottom_Right",
        
        
        };

    [NonSerialized, HideInInspector] 
    private string [] WinCombination =
    {
        "A_Corner_Left",
        "A_Corner_Mid",
        "A_Corner_Right",
        "A_Circle_Left" ,
        "A_Circle_Mid",

        "B_Corner_Left",
        "B_Corner_Mid",
        "B_Corner_Right",
        "B_Circle_Left",
        "B_Circle_Mid",


        "C_Corner_Left",
        "C_Corner_Right",
        "C_Wing_Left",
        "C_Wing_Right",
        "C_Circle_Left",
        "C_Circle_Right",

        "D_West",
        "D_East",


        "E_Top_Left",
        "E_Bottom_Left",

        "G_Top_Right",
        "G_Bottom_Right",

    };

    private string [] FirstCombination =
    {
        "A_Circle_Mid",
        "B_Circle_Right",
        "C_Circle_Left",

        "D_East",
        "D_North",
        "D_South",
        "D_West",      

        "E_Top_Left",
        "E_Bottom_Left",
        "E_Top_Right",
        "E_Bottom_Right",          

    };


    private string [] SecondCombination =
    {
        "A_Corner_Left",
        "A_Corner_Right",
        "A_Wing_Left",
        "A_Wing_Right",

        "B_Corner_Left",
        "B_Corner_Right",
        "B_Wing_Left",
        "B_Wing_Right",

        "C_Corner_Left",
        "C_Corner_Right",

        "C_Wing_Left",
        "C_Wing_Right",

        "G_Top_Left",
        "G_Bottom_Left",
        "G_Top_Right",
        "G_Bottom_Right", 

    };


    private string [] ThirdCombination =
    {
        "A_Corner_Mid",
        "A_Wing_Right",

        "B_Corner_Mid",
        "B_Wing_Right",


        "C_Corner_Mid",
        "C_Wing_Right",

        "A_Circle_Left" ,
        "A_Circle_Mid",
        "A_Circle_Right",

        "B_Circle_Left",
        "B_Circle_Mid",
        "B_Circle_Right",

        "C_Circle_Left",
        "C_Circle_Mid",
        "C_Circle_Right",


    };


    private string [] FourthCombination =
    {
        "A_Circle_Left",
        "A_Circle_Mid",
        "A_Circle_Right",
        "A_Corner_Mid",
        "A_Wing_Left",

        "B_Circle_Left",
        "B_Circle_Mid",
        "B_Corner_Mid",
        "B_Wing_Left",

        "C_Circle_Left",
        "C_Circle_Mid",

        "C_Corner_Mid",

        "C_Wing_Left",

        "D_North",
        "D_South",

        "G_Top_Left",
        "G_Bottom_Left"
    };

    private string [] FifthCombination =
    {
        "A_Wing_Right",
        "A_Circle_Right",
        "A_Circle_Mid",
        "B_Circle_Right",
        "B_Wing_Right",
        "C_Circle_Right",
        "C_Circle_Left",
        "C_Circle_Mid",

        "C_Corner_Mid",

        "C_Wing_Left",


        "E_Top_Right",
        "E_Bottom_Right"

    };

    private string [] Selection = Array.Empty<string>();

    private string[][] combinations;

    void UpdateSelectionVisuals()
    {
        if (tableu == null)
        {
            Debug.LogError("PuzzlePanel.UpdateSelectionVisuals: tableu reference is missing.");
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
                Debug.LogError($"PuzzlePanel.UpdateSelectionVisuals: child '{name}' not found under tableu.");
                continue;
            }

            child.gameObject.SetActive(true);
        }
    }



    
    [NonSerialized, HideInInspector] 
    private Dictionary<string, int> indexes ;
    
    [NonSerialized, HideInInspector] 
    public bool SolutionFound = false;

    void Awake()
    {
        indexes = ListYUtil.ToIndexDictionary(ItemNames);
        combinations = new string[][]
        {
            FirstCombination,
            ThirdCombination,
            SecondCombination,
            FourthCombination,
            FifthCombination,
            ItemNames
        };
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        UpdateSelectionVisuals();
        Test();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Test()
    {
        string [] start = Array.Empty<string>();

        var first = ListYUtil.SwapBySecondArray(start,FirstCombination);
        var second = ListYUtil.SwapBySecondArray(first,SecondCombination);
        var fourth = ListYUtil.SwapBySecondArray(second, FourthCombination);
        var fifth = ListYUtil.SwapBySecondArray(fourth, FifthCombination);

        var good = ListYUtil.UnorderedEqual(WinCombination,fifth);
        Debug.Log($"Test is good : {good}");
    }


    private void OnEnable()
    {   
        //Debug.Log($"PuzzlePanel OnEnable");
        ClickNumberInvoke.ClickedNumber += HandleClickedNumber;
    }

    private void OnDisable()
    {   
        //Debug.Log($"PuzzlePanel OnDisable");
        ClickNumberInvoke.ClickedNumber -= HandleClickedNumber;
    }

    private void HandleClickedNumber(int n)
    {
        //Debug.Log($"PuzzlePanel received: {n}");
        if (SolutionFound)
        {
            return;
        }

        if(n == 6)
        {
            Selection = Array.Empty<string>();
            UpdateSelectionVisuals();
            return;
        }
        if (n == 7)
        {
            var good = ListYUtil.UnorderedEqual(WinCombination,Selection);
            Debug.Log($"Is combination correct : {good}");
            if (good)
            {
                OnSolutionFound();
            }
            return;
        }

        if(n > combinations.Length)
        {
            Debug.LogWarning("PuzzlePanel: wrong button number");
            return;
        }
        var combination = combinations[n];

        Selection = ListYUtil.SwapBySecondArray(Selection,combination);
        UpdateSelectionVisuals();
    }    

    private void OnSolutionFound()
    {
        SolutionFound = true;

        if (MainPanel is FocusInteractable focusInteractable)
        {
            focusInteractable.CompleteInteraction();
            return;
        }

        MainPanel.KillInteraction();
    }




}
