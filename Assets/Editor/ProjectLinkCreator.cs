using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ProjectLinkCreator
{
    private const string MenuPath = "Assets/Create/Project Link/This Root";
    private const string Extension = ".ylink";
    private const string IconPath = "Assets/Editor/Icons/ylink_icon.png";

    private static Texture2D linkIcon;

    static ProjectLinkCreator()
    {
        EditorApplication.projectWindowItemOnGUI -= DrawProjectLinkIcon;
        EditorApplication.projectWindowItemOnGUI += DrawProjectLinkIcon;
    }

    [Serializable]
    private class ProjectLinkData
    {
        public string name;
        public string path;
    }

    [MenuItem(MenuPath, false, 80)]
    public static void CreateThisRootLink()
    {
        string targetFolder = GetSelectedProjectFolder();

        string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRootPath))
        {
            Debug.LogError("Could not determine Unity project root path.");
            return;
        }

        string projectName = new DirectoryInfo(projectRootPath).Name;

        ProjectLinkData data = new ProjectLinkData
        {
            name = projectName,
            path = NormalizePath(Application.dataPath)
        };

        string json = JsonUtility.ToJson(data, true);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{targetFolder}/{projectName}{Extension}"
        );
        string absoluteFilePath = Path.Combine(projectRootPath, assetPath);

        File.WriteAllText(absoluteFilePath, json, Encoding.UTF8);

        AssetDatabase.ImportAsset(assetPath);
        AssetDatabase.Refresh();

        Object createdAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
        Selection.activeObject = createdAsset;
        EditorGUIUtility.PingObject(createdAsset);

        Debug.Log($"Created Project Link file: {assetPath}");
    }

    private static string GetSelectedProjectFolder()
    {
        Object selectedObject = Selection.activeObject;

        if (selectedObject == null)
            return "Assets";

        string selectedPath = AssetDatabase.GetAssetPath(selectedObject);

        if (string.IsNullOrEmpty(selectedPath))
            return "Assets";

        if (AssetDatabase.IsValidFolder(selectedPath))
            return selectedPath;

        string parentFolder = Path.GetDirectoryName(selectedPath);

        if (string.IsNullOrEmpty(parentFolder))
            return "Assets";

        return NormalizePath(parentFolder);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("\\", "/");
    }

    private static void DrawProjectLinkIcon(string guid, Rect selectionRect)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (!assetPath.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
            return;

        Texture2D icon = GetLinkIcon();
        if (icon == null)
            return;

        GUI.DrawTexture(GetIconRect(selectionRect), icon, ScaleMode.ScaleToFit, true);
    }

    private static Texture2D GetLinkIcon()
    {
        if (linkIcon == null)
            linkIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);

        return linkIcon;
    }

    private static Rect GetIconRect(Rect selectionRect)
    {
        if (selectionRect.height <= 20f)
            return new Rect(selectionRect.x, selectionRect.y + 1f, 16f, 16f);

        float size = Mathf.Min(selectionRect.width, selectionRect.height - 16f);
        return new Rect(
            selectionRect.x + (selectionRect.width - size) * 0.5f,
            selectionRect.y,
            size,
            size
        );
    }
}

[CustomEditor(typeof(DefaultAsset))]
public class ProjectLinkInspector : Editor
{
    private const string Extension = ".ylink";

    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        string assetPath = AssetDatabase.GetAssetPath(target);
        if (!assetPath.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            base.OnInspectorGUI();
            return;
        }

        DrawProjectLinkInspector(assetPath);
    }

    private void DrawProjectLinkInspector(string assetPath)
    {
        EditorGUILayout.LabelField(Path.GetFileName(assetPath), EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        string filePath = GetAbsoluteFilePath(assetPath);
        if (!File.Exists(filePath))
        {
            EditorGUILayout.HelpBox("Project link file was not found on disk.", MessageType.Warning);
            return;
        }

        string content = File.ReadAllText(filePath, Encoding.UTF8);
        GUIStyle textStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = false
        };

        float viewWidth = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 40f);
        float textHeight = Mathf.Max(120f, textStyle.CalcHeight(new GUIContent(content), viewWidth));

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.SelectableLabel(content, textStyle, GUILayout.Height(textHeight));
        EditorGUILayout.EndScrollView();
    }

    private static string GetAbsoluteFilePath(string assetPath)
    {
        string projectRootPath = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRootPath))
            return assetPath;

        return Path.Combine(projectRootPath, assetPath);
    }
}
