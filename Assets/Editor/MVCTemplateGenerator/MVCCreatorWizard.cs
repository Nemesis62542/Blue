using UnityEditor;
using UnityEngine;
using System.IO;

public class MVCCreatorWizard : ScriptableWizard
{
    public string className = "NewEntity";

    [MenuItem("Assets/Create/MVC Scripts", false, 80)]
    static void CreateWizard()
    {
        DisplayWizard<MVCCreatorWizard>("Create MVC Scripts", "Create");
    }

    void OnWizardCreate()
    {
        string folder_path = GetSelectedPath();

        string controller_path = "Editor/MVCTemplateGenerator/Templates/ControllerTemplate.txt";
        string model_path = "Editor/MVCTemplateGenerator/Templates/ModelTemplate.txt";
        string view_path = "Editor/MVCTemplateGenerator/Templates/ViewTemplate.txt";

        string controller_code = GetTemplate(controller_path, className);
        string model_code = GetTemplate(model_path, className);
        string view_code = GetTemplate(view_path, className);

        File.WriteAllText(Path.Combine(folder_path, $"{className}Controller.cs"), controller_code);
        File.WriteAllText(Path.Combine(folder_path, $"{className}Model.cs"), model_code);
        File.WriteAllText(Path.Combine(folder_path, $"{className}View.cs"), view_code);

        AssetDatabase.Refresh();
    }

    private static string GetSelectedPath()
    {
        Object obj = Selection.activeObject;
        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return "Assets";
        if (Path.GetExtension(path) != "") return Path.GetDirectoryName(path);
        return path;
    }

    private static string GetTemplate(string relativePath, string name)
    {
        string full_path = Path.Combine(Application.dataPath, relativePath);
        string content = File.ReadAllText(full_path);
        return content.Replace("#NAME#", name);
    }
}