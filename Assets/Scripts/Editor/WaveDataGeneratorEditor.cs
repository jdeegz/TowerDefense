using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveDataGenerator))]
public class WaveDataGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WaveDataGenerator groupGenerator = (WaveDataGenerator)target;

        if (GUILayout.Button("Generate Wave Data"))
        {
            groupGenerator.GenerateLoopingWaveData();
            Debug.Log("Wave data generation complete!");
        }
    }
}
