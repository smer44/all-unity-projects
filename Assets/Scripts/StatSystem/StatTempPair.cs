using System;
using UnityEngine;

[Serializable]
public class StatTempPair
{
    public string statName;
    [Min(0)] public int value;
    [Min(0)] public int maxValue;
}
