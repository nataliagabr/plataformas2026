using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public static class BootScenePlayMode
    {
        // Flag to avoid re-entrancy
        static bool _sProcessed;
        // Store the scene paths that were open before we changed them
        static string[] _sPreviousScenePaths;

        static BootScenePlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // About to enter Play mode in Editor
                if (_sProcessed) return;
                _sProcessed = true;

                // Ask user to save modified scenes; if they cancel, abort entering play mode
                if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorApplication.isPlaying = false;
                    _sProcessed = false;
                    return;
                }

                // Find the _Boot scene asset
                string bootPath = FindBootScenePath();
                if (string.IsNullOrEmpty(bootPath))
                {
                    Debug.LogWarning("[BootScenePlayMode] Could not find a scene named '_Boot' in the project. Play will continue normally.");
                    _sProcessed = false;
                    return;
                }

                // Store currently open scenes so we can restore them later
                int sceneCount = UnityEditor.SceneManagement.EditorSceneManager.sceneCount;
                List<string> openPaths = new List<string>(sceneCount);
                var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                string activePath = active.path;

                for (int i = 0; i < sceneCount; i++)
                {
                    var s = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                    if (!string.IsNullOrEmpty(s.path)) openPaths.Add(s.path);
                }

                _sPreviousScenePaths = openPaths.ToArray();

                // Open the boot scene first (single)
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(bootPath, OpenSceneMode.Single);

                // Re-open previous scenes additively (skip boot if it was already in the list)
                bool bootWasOriginallyOpen = false;
                foreach (var p in _sPreviousScenePaths)
                {
                    if (string.IsNullOrEmpty(p)) continue;
                    if (p == bootPath) { bootWasOriginallyOpen = true; continue; }
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(p, OpenSceneMode.Additive);
                }

                // Restore the previously active scene if possible
                if (!string.IsNullOrEmpty(activePath))
                {
                    var prevActive = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(activePath);
                    if (prevActive.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(prevActive);
                }

                // If boot wasn't originally open we can close it now so it doesn't remain loaded during Play
                if (!bootWasOriginallyOpen)
                {
                    var bootScene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(bootPath);
                    if (bootScene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(bootScene, true);
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // We returned to Edit mode after stopping Play. Restore previous open scenes if we modified them.
                if (!_sProcessed) return;
                _sProcessed = false;

                if (_sPreviousScenePaths == null || _sPreviousScenePaths.Length == 0) return;

                // If the editor already has the same scenes open, do nothing
                bool same = UnityEditor.SceneManagement.EditorSceneManager.sceneCount == _sPreviousScenePaths.Length;
                if (same)
                {
                    for (int i = 0; i < _sPreviousScenePaths.Length; i++)
                    {
                        var sc = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                        if (sc.path != _sPreviousScenePaths[i]) { same = false; break; }
                    }
                }
                if (same)
                {
                    _sPreviousScenePaths = null;
                    return;
                }

                // Open the saved scenes: first as Single then others Additive
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(_sPreviousScenePaths[0], OpenSceneMode.Single);
                for (int i = 1; i < _sPreviousScenePaths.Length; i++)
                {
                    if (string.IsNullOrEmpty(_sPreviousScenePaths[i])) continue;
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(_sPreviousScenePaths[i], OpenSceneMode.Additive);
                }

                // Set active to the first previous scene
                var first = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(_sPreviousScenePaths[0]);
                if (first.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.SetActiveScene(first);

                _sPreviousScenePaths = null;
            }
        }

        static string FindBootScenePath()
        {
            // Try to find a scene asset named exactly "_Boot"
            string[] guids = AssetDatabase.FindAssets("_Boot t:Scene");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == "_Boot") return path;
            }

            // Fallback: search all scenes for name match (case-insensitive)
            guids = AssetDatabase.FindAssets("t:Scene");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(path).Equals("_Boot", System.StringComparison.OrdinalIgnoreCase)) return path;
            }

            return null;
        }
    }
}




