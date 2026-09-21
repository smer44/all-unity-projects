using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class StatSetTests
{
    private StatSet statSet;
    private StatTempSet tempSet;
    private string assetFolder;

    [SetUp]
    public void SetUp()
    {
        statSet = ScriptableObject.CreateInstance<StatSet>();
        tempSet = ScriptableObject.CreateInstance<StatTempSet>();
    }

    [TearDown]
    public void TearDown()
    {
        if (statSet != null && !EditorUtility.IsPersistent(statSet))
            Object.DestroyImmediate(statSet);
        Object.DestroyImmediate(tempSet);
        if (assetFolder != null)
            AssetDatabase.DeleteAsset(assetFolder);
    }

    [Test]
    public void PairTypesAreGlobalPublicSerializableClasses()
    {
        foreach (Type type in new[] { typeof(StatPair), typeof(StatTempPair) })
        {
            Assert.That(type.IsClass && type.IsPublic && !type.IsNested, Is.True);
            Assert.That(type.Namespace, Is.Null);
            Assert.That(Attribute.IsDefined(type, typeof(SerializableAttribute)), Is.True);
        }
    }

    [Test]
    public void InspectorCanConfigureStatPairClassEntries()
    {
        Configure(statSet, "initialStats", ("Health", 100), ("Armor", 0));

        Assert.That(statSet.GetStat("Health"), Is.EqualTo(100));
        Assert.That(statSet.GetStat("Armor"), Is.Zero);
        statSet.SetStat("Health", 40);
        statSet.SetStat("NewStat", 7);
        Assert.That(statSet.GetStat("Health"), Is.EqualTo(40));
        Assert.That(statSet.GetStat("NewStat"), Is.EqualTo(7));
        using (var serialized = new SerializedObject(statSet))
            Assert.That(serialized.FindProperty("initialStats").GetArrayElementAtIndex(0)
                .FindPropertyRelative("value").intValue, Is.EqualTo(100));
    }

    [Test]
    public void InspectorConfiguredClassEntriesSurviveAssetReload()
    {
        Configure(statSet, "initialStats", ("Health", 100), ("Armor", 20));
        string folderName = "__StatSetTests_" + Guid.NewGuid().ToString("N");
        assetFolder = "Assets/" + folderName;
        AssetDatabase.CreateFolder("Assets", folderName);
        string path = assetFolder + "/Stats.asset";
        AssetDatabase.CreateAsset(statSet, path);
        AssetDatabase.SaveAssetIfDirty(statSet);
        statSet.SetStat("Health", 1);
        Resources.UnloadAsset(statSet);

        statSet = AssetDatabase.LoadAssetAtPath<StatSet>(path);
        Assert.That(statSet.GetStat("Health"), Is.EqualTo(100));
        Assert.That(statSet.GetStat("Armor"), Is.EqualTo(20));
    }

    [Test]
    public void ExistingInlinePairDataCanBeDeserializedAsClasses()
    {
        JsonUtility.FromJsonOverwrite(
            "{\"initialStats\":[{\"statName\":\"Health\",\"value\":75}]}", statSet);
        StatSet loaded = Object.Instantiate(statSet);
        try { Assert.That(loaded.GetStat("Health"), Is.EqualTo(75)); }
        finally { Object.DestroyImmediate(loaded); }
    }

    [Test]
    public void InspectorPairsDefineTemporaryMaximumsAndCurrentValuesStartFull()
    {
        Configure(tempSet, "maximumStats", ("Health", 100), ("Mana", 30));

        Assert.That(tempSet.GetStat("Health"), Is.EqualTo(100));
        Assert.That(tempSet.GetStat("Mana"), Is.EqualTo(30));
        tempSet.SetStat("Health", 25);
        Assert.That(tempSet.GetStat("Health"), Is.EqualTo(25));
        using (var serialized = new SerializedObject(tempSet))
            Assert.That(serialized.FindProperty("maximumStats").GetArrayElementAtIndex(0)
                .FindPropertyRelative("value").intValue, Is.EqualTo(100));

        StatTempSet loaded = Object.Instantiate(tempSet);
        try { Assert.That(loaded.GetStat("Health"), Is.EqualTo(100)); }
        finally { Object.DestroyImmediate(loaded); }
    }

    [Test]
    public void FullHealRestoresAllMaximumsAndGreaterThanZeroUsesCurrentValue()
    {
        Configure(tempSet, "maximumStats", ("Health", 100), ("Mana", 30), ("Empty", 0));
        tempSet.SetStat("Health", 0);
        tempSet.SetStat("Mana", 1);
        Assert.That(tempSet.IsGreaterThenZero("Health"), Is.False);
        Assert.That(tempSet.IsGreaterThenZero("Mana"), Is.True);

        tempSet.FullHeal();

        Assert.That(tempSet.GetStat("Health"), Is.EqualTo(100));
        Assert.That(tempSet.GetStat("Mana"), Is.EqualTo(30));
        Assert.That(tempSet.IsGreaterThenZero("Health"), Is.True);
        Assert.That(tempSet.IsGreaterThenZero("Empty"), Is.False);
    }

    [Test]
    public void ValuesAreClampedAndLastDuplicateWins()
    {
        Configure(statSet, "initialStats", ("Health", 10), ("Health", 50), ("Armor", -2));
        Configure(tempSet, "maximumStats", ("Health", 10), ("Health", 50), ("Armor", -2));
        Assert.That(statSet.GetStat("Health"), Is.EqualTo(50));
        Assert.That(tempSet.GetStat("Health"), Is.EqualTo(50));
        Assert.That(statSet.GetStat("Armor"), Is.Zero);
        Assert.That(tempSet.GetStat("Armor"), Is.Zero);
        statSet.SetStat("Health", -1);
        tempSet.SetStat("Health", -1);
        Assert.That(statSet.GetStat("Health"), Is.Zero);
        Assert.That(tempSet.GetStat("Health"), Is.Zero);
        tempSet.SetStat("Health", int.MaxValue);
        Assert.That(tempSet.GetStat("Health"), Is.EqualTo(50));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("Missing")]
    public void UnknownTemporaryStatsReturnZeroAndCannotBeHealed(string statName)
    {
        Assert.That(statSet.GetStat(statName), Is.Zero);
        tempSet.SetStat(statName, 10);
        tempSet.FullHeal();
        Assert.That(tempSet.GetStat(statName), Is.Zero);
        Assert.That(tempSet.IsGreaterThenZero(statName), Is.False);
    }

    private static void Configure(Object target, string listName, params (string name, int value)[] entries)
    {
        using (var serialized = new SerializedObject(target))
        {
            SerializedProperty list = serialized.FindProperty(listName);
            Assert.That(list, Is.Not.Null, "The list must be exposed to the Inspector.");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty pair = list.GetArrayElementAtIndex(i);
                pair.FindPropertyRelative("statName").stringValue = entries[i].name;
                pair.FindPropertyRelative("value").intValue = entries[i].value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
