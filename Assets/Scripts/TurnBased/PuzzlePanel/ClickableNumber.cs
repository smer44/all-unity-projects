using UnityEngine;

public class ClickableNumber : MonoBehaviour
{
    [field: SerializeField]
    public int Number { get; private set; } = 0;
}