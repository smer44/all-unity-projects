using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Temporary migration helper; removed after the move and validation finish.
[InitializeOnLoad]
internal static class CityBuilderIsolationMigration
{
    private const string Work = "F:/unityProjects/VerticalSliceBackup/.utmp/city-builder-isolation/";
    private const string DestinationRoot = "Assets/ScriptsCityBuilder";
    [Serializable] private class Entry
    {
        public string source, destination, guid, assetHash, metaHash, reason;
    }
    [Serializable] private class Plan
    {
        public string scene, sceneHash;
        public Entry[] entries;
    }

    static CityBuilderIsolationMigration()
    {
        File.WriteAllText(Work + "initialized.txt", Directory.GetCurrentDirectory());
        if (!File.Exists(Work + "finished.txt") && !File.Exists(Work + "error.txt"))
            EditorApplication.update += Execute;
    }

    private static void Execute()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= Execute;
        try
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("The asset migration needs Edit Mode.");
            var plan = JsonUtility.FromJson<Plan>(File.ReadAllText(Work + "move-plan.json"));
            if (File.Exists(Work + "moved.txt"))
            {
                foreach (var entry in plan.entries)
                {
                    Require(AssetDatabase.GUIDToAssetPath(entry.guid) == entry.destination, "GUID changed: " + entry.destination);
                    Require(!File.Exists(entry.source) && !File.Exists(entry.source + ".meta"), "Source remains: " + entry.source);
                    Require(Hash(entry.destination) == entry.assetHash, "Asset content changed: " + entry.destination);
                    Require(Hash(entry.destination + ".meta") == entry.metaHash, "Metadata changed: " + entry.destination);
                }
                Require(Hash(plan.scene) == plan.sceneHash, "Scene contents changed.");
                File.WriteAllLines(Work + "unity-dependencies-after.txt", AssetDatabase.GetDependencies(plan.scene, true).OrderBy(x => x));
                File.WriteAllText(Work + "finished.txt", "Unity imported all 44 moved assets after compilation; all asset contents, metadata and GUIDs are preserved. The scene is unchanged.");
                Debug.Log("City builder isolation validated: " + plan.entries.Length + " assets moved.");
                return;
            }

            string workspace = Path.GetFullPath(".") + Path.DirectorySeparatorChar;
            foreach (var entry in plan.entries)
            {
                Require(entry.source.StartsWith("Assets/", StringComparison.Ordinal) &&
                    entry.destination == DestinationRoot + "/" + entry.source.Substring(7), "Unexpected move path.");
                Require(Path.GetFullPath(entry.source).StartsWith(workspace, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFullPath(entry.destination).StartsWith(workspace, StringComparison.OrdinalIgnoreCase), "Path outside workspace.");
                Require(!File.Exists(entry.destination) && !File.Exists(entry.destination + ".meta"), "Destination exists: " + entry.destination);
                Require(AssetDatabase.AssetPathToGUID(entry.source) == entry.guid, "Source GUID mismatch: " + entry.source);
                Require(Hash(entry.source) == entry.assetHash && Hash(entry.source + ".meta") == entry.metaHash, "Source changed: " + entry.source);
            }
            Require(Hash(plan.scene) == plan.sceneHash, "Scene changed since the dependency audit.");
            var dependencies = AssetDatabase.GetDependencies(plan.scene, true);
            File.WriteAllLines(Work + "unity-dependencies-before.txt", dependencies.OrderBy(x => x));
            var missing = dependencies.Where(x => x.StartsWith("Assets/") && (x.EndsWith(".cs") || x.EndsWith(".prefab")))
                .Where(x => !plan.entries.Any(e => e.source == x)).ToArray();
            Require(missing.Length == 0, "Unity found additional dependencies: " + string.Join(", ", missing));

            foreach (var entry in plan.entries)
                CreateFolder(Path.GetDirectoryName(entry.destination).Replace('\\', '/'));

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var entry in plan.entries)
                {
                    string error = AssetDatabase.MoveAsset(entry.source, entry.destination);
                    Require(string.IsNullOrEmpty(error), error);
                }
                File.WriteAllText(Work + "moved.txt", DateTime.UtcNow.ToString("O"));
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Work + "error.txt", exception.ToString());
            Debug.LogException(exception);
        }
    }

    private static void CreateFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        CreateFolder(parent);
        Require(!string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path))), "Could not create " + path);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }
}
