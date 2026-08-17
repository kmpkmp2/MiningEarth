using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepEarth.Editor
{
    // 개발 중 자주 오가는 씬(Assets/Game/Scenes/*.unity)을 목록으로 띄워 한 번에 전환하는 도구.
    // Build Settings 등록 여부와 무관하게 폴더 내 모든 씬을 보여준다(MapDebugScene 등 디버그 전용 씬 포함).
    public class SceneNavigatorWindow : EditorWindow
    {
        private const string ScenesFolder = "Assets/Game/Scenes";

        private List<SceneInfo> _scenes = new List<SceneInfo>();
        private Vector2 _scroll;

        private struct SceneInfo
        {
            public string Path;
            public string Name;
            public int BuildIndex; // -1 = Build Settings 미등록
        }

        [MenuItem("Tools/Scene Navigator")]
        private static void Open()
        {
            var window = GetWindow<SceneNavigatorWindow>("Scene Navigator");
            window.minSize = new Vector2(240, 120);
            window.RefreshSceneList();
        }

        private void OnEnable() => RefreshSceneList();
        private void OnFocus() => RefreshSceneList();

        private void RefreshSceneList()
        {
            var buildScenes = EditorBuildSettings.scenes;
            var guids = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder });

            _scenes = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path =>
                {
                    int buildIndex = System.Array.FindIndex(buildScenes, s => s.path == path);
                    return new SceneInfo
                    {
                        Path = path,
                        Name = System.IO.Path.GetFileNameWithoutExtension(path),
                        BuildIndex = buildIndex
                    };
                })
                .OrderBy(s => s.BuildIndex < 0 ? int.MaxValue : s.BuildIndex)
                .ThenBy(s => s.Name)
                .ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Refresh")) RefreshSceneList();
            EditorGUILayout.Space(4);

            string activeScenePath = SceneManager.GetActiveScene().path;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var scene in _scenes)
            {
                bool isActive = scene.Path == activeScenePath;

                EditorGUILayout.BeginHorizontal(isActive ? EditorStyles.helpBox : GUIStyle.none);

                string label = scene.BuildIndex >= 0 ? $"{scene.Name}  (Build #{scene.BuildIndex})" : $"{scene.Name}  (Not in Build)";
                EditorGUILayout.LabelField(label, isActive ? EditorStyles.boldLabel : EditorStyles.label);

                GUI.enabled = !isActive;
                if (GUILayout.Button("Open", GUILayout.Width(60)))
                    OpenScene(scene.Path);
                if (GUILayout.Button("Additive", GUILayout.Width(70)))
                    OpenSceneAdditive(scene.Path);
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void OpenScene(string path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; // 사용자가 취소하면 전환하지 않는다
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static void OpenSceneAdditive(string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
    }
}
