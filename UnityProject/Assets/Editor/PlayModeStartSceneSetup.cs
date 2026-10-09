using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ensures Play Mode always starts from the login scene, regardless of the
/// scene currently open in the Unity Editor.
/// </summary>
[InitializeOnLoad]
internal static class PlayModeStartSceneSetup
{
    private const string LoginScenePath = "Assets/Scenes/Login.unity";

    static PlayModeStartSceneSetup()
    {
        SetLoginSceneAsPlayModeStartScene();
    }

    private static void SetLoginSceneAsPlayModeStartScene()
    {
        SceneAsset loginScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoginScenePath);

        if (loginScene == null)
        {
            Debug.LogWarning($"Could not find the Play Mode start scene at '{LoginScenePath}'.");
            return;
        }

        if (EditorSceneManager.playModeStartScene != loginScene)
        {
            EditorSceneManager.playModeStartScene = loginScene;
            Debug.Log($"Play Mode will now always start from '{LoginScenePath}'.");
        }
    }
}