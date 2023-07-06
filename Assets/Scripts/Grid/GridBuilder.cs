using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridBuilder))]
public class GridBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GridBuilder buildGrid = (GridBuilder)target;

        if (GUILayout.Button("Build Grid"))
        {
            buildGrid.BuildGrid();
        }
    }
}