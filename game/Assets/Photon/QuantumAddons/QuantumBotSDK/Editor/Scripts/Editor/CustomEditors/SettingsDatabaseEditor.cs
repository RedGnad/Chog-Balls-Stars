//using UnityEditor;
//using Circuit;
//using UnityEngine;
//using Quantum;
//using System.IO;
//using System.Linq;
//using Unity.CodeEditor;

//[CustomEditor(typeof(SettingsDatabase))]
//public class SettingsDatabaseEditor : Editor
//{
//  public static void OnOpenInEditor(string className, bool isIntegratedSolution)
//  {
//    className += ".cs";

//    string searchPath;
//    if (isIntegratedSolution == false)
//    {
//      searchPath = Application.dataPath + "/../../quantum_code/quantum.code";
//    }
//    else
//    {
//      searchPath = Application.dataPath;
//    }

//    var absoluteFilePath = Directory.GetFiles(searchPath, "*.cs", SearchOption.AllDirectories).
//      FirstOrDefault(path => path.Contains(className))?.Replace('\\', '/');

//    Quantum.Assert.Check(string.IsNullOrEmpty(absoluteFilePath) == false, $"Could not find file with name {className} at raw search path {searchPath}");

//    CodeEditor.CurrentEditor.OpenProject(absoluteFilePath);
//  }

//  public override void OnInspectorGUI()
//  {
//    base.OnInspectorGUI();

//    GUILayout.BeginHorizontal();

//    if (GUILayout.Button("Change Folder", EditorStyles.miniButton))
//    {
//      EditorApplication.delayCall += () =>
//      {
//        var path = UnityEditor.EditorUtility.OpenFolderPanel("Select search path", "Assets", "");
//        path = path.Replace(Application.dataPath, "Assets");
//        if (AssetDatabase.IsValidFolder(path))
//        {
//          serializedObject.FindProperty("BotSDKOutputFolder").SetValue(path);
//          serializedObject.Update();

//          EditorUtility.SetDirty(serializedObject.targetObject);
//          AssetDatabase.SaveAssets();
//        }
//      };
//    }

//    if (GUILayout.Button("Reset Folder", EditorStyles.miniButton))
//    {
//      serializedObject.FindProperty("BotSDKOutputFolder").SetValue("Assets/Resources/DB");
//      serializedObject.Update();

//      EditorUtility.SetDirty(serializedObject.targetObject);
//      AssetDatabase.SaveAssets();
//    }

//    GUILayout.EndHorizontal();

//    EditorGUILayout.HelpBox("Please make sure that the chosen location is included in Quantum's Asset Database paths, " +
//      "otherwise the output will not be loaded by the DB.", MessageType.Info);
//  }
//}
