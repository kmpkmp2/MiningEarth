using UnityEditor;
using UnityEngine;
using DeepEarth.Map;

/// <summary>
/// Custom Inspector for MapDebugger.
/// Adds Generate, Random Seed buttons and displays the current seed.
/// </summary>
[CustomEditor(typeof(MapDebugger))]
public class MapDebuggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

        var debugger = (MapDebugger)target;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate", GUILayout.Height(30f)))
        {
            debugger.Generate();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Random Seed", GUILayout.Height(30f)))
        {
            Undo.RecordObject(debugger, "Randomise Map Seed");
            debugger.RandomizeSeed();
            EditorUtility.SetDirty(debugger);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(
            $"Current seed: {debugger.Seed}\nGenerate to see map in Scene View gizmos and Console.",
            MessageType.Info);
    }
}
