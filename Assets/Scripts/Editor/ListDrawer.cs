using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SpawnerWaves))]
public class CreepWaveDataEditor : Editor
{
    private SerializedProperty m_introWaves;
    private SerializedProperty m_loopingWaves;
    private SerializedProperty m_challengingWaves;
    private SerializedProperty m_newEnemyTypeWaves;
    private bool foldoutsExpanded = false; // Track expand/collapse state

    private void OnEnable()
    {
        // Cache the SerializedProperties
        m_introWaves = serializedObject.FindProperty("m_introWaves");
        m_loopingWaves = serializedObject.FindProperty("m_loopingWaves");
        m_challengingWaves = serializedObject.FindProperty("m_challengingWaves");
        m_newEnemyTypeWaves = serializedObject.FindProperty("m_newEnemyTypeWaves");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Expand/Collapse all button
        if (GUILayout.Button(foldoutsExpanded ? "Collapse All" : "Expand All"))
        {
            foldoutsExpanded = !foldoutsExpanded;
            SetFoldoutState(m_introWaves, foldoutsExpanded);
            SetFoldoutState(m_loopingWaves, foldoutsExpanded);
            SetFoldoutState(m_challengingWaves, foldoutsExpanded);
            SetFoldoutState(m_newEnemyTypeWaves, foldoutsExpanded);
        }

        // Use the updated DrawListWithoutMaxHeight
        DrawListWithoutMaxHeight(m_introWaves, "Intro Waves", Color.cyan);
        DrawListWithoutMaxHeight(m_loopingWaves, "Looping Waves", Color.green);
        DrawListWithoutMaxHeight(m_challengingWaves, "Challenge Waves", Color.yellow);
        DrawListWithoutMaxHeight(m_newEnemyTypeWaves, "New Enemy Type Waves", Color.magenta);

        serializedObject.ApplyModifiedProperties();
    }

    // Recursively sets the foldout state of all elements in the property
    private void SetFoldoutState(SerializedProperty property, bool state)
    {
        property.isExpanded = state;

        if (property.hasChildren)
        {
            SerializedProperty child = property.Copy();
            int originalDepth = property.depth;

            // Iterate through all child properties
            while (child.NextVisible(true) && child.depth > originalDepth)
            {
                child.isExpanded = state;
            }
        }
    }

    private void DrawListWithoutMaxHeight(SerializedProperty listProperty, string label, Color color)
    {
        // Set the GUI color for the background
        GUI.backgroundColor = color;

        // Draw the property with Unity's default handling
        EditorGUILayout.PropertyField(listProperty, new GUIContent(label), true);

        // Reset the GUI color
        GUI.backgroundColor = Color.white;
    }
}