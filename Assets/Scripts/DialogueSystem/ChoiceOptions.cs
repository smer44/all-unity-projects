using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

//[MovedFrom(false, sourceNamespace: null, sourceAssembly: null, sourceClassName: "ChoiceEntry")]
[CreateAssetMenu(fileName = "ChoiceOptions", menuName = "Scriptable Objects/Fabler/ChoiceOptions")]
public class ChoiceOptions : ScriptableObject
{
    public string[] texts;

    public DialogueEntry[] transitions;


}
