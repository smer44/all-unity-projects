using System;
using UnityEngine;

[Serializable]
public class StatPair
{
    public string statName;
    [Min(0)] public int value;
}
