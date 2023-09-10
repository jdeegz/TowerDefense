using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ScenePlayModeHotkey : EditorWindow
{
    private const string menuPath = "MyGame/Enter Play Mode (Ctrl+G)";
    private const string scenePath = "Assets/Scenes/Initialize.unity"; // Replace with your scene path

    [MenuItem(menuPath)]
    private static void EnterPlayMode()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update += CheckShortcut;
    }

    private static void CheckShortcut()
    {
        // Check for Ctrl+G only if the focused window is the scene view
        if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.titleContent.text == "Scene")
        {
            if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.modifiers == EventModifiers.Control && Event.current.keyCode == KeyCode.G)
            {
                EnterPlayMode();
                Event.current.Use();
            }
        }
    }
}