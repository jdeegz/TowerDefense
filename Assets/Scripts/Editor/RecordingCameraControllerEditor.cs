using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RecordingCameraController))]
public class RecordingCameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RecordingCameraController controller = (RecordingCameraController)target;
        if (GUILayout.Button("Trigger Movement"))
        {
            controller.StartMoving();
        }
        
        if (GUILayout.Button("Stop Movement"))
        {
            controller.StopMoving();
        }
    }
}
