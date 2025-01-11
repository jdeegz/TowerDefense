using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ScenePlayModeHotkey : EditorWindow
{
    private const string menuPath = "MyGame/Enter Play Mode (Ctrl+G)";
    private const string scenePath = "Assets/Scenes/Initialize.unity"; // Replace with your scene path
    private static KeyCode hotkey = KeyCode.G;
    private static EventModifiers modifier = EventModifiers.Control;

    [MenuItem(menuPath)]
    private static void EnterPlayMode()
    {
        // Save the current scene if there are unsaved changes
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Open the specified scene
        EditorSceneManager.OpenScene(scenePath);

        // Start Play Mode
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        // Subscribe to the editor's global update event
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event currentEvent = Event.current;

        // Check if the correct hotkey is pressed
        if (currentEvent != null && currentEvent.type == EventType.KeyDown)
        {
            if (currentEvent.modifiers == modifier && currentEvent.keyCode == hotkey)
            {
                Debug.Log("Hotkey pressed: " + hotkey); // Debug log for testing
                EnterPlayMode();
                currentEvent.Use(); // Consume the event to prevent propagation
            }
        }
    }
}