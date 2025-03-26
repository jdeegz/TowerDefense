using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MissionTableController))]
public class MissionTableEditor : Editor
{
    private MissionTableController m_missionTableController;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        m_missionTableController = (MissionTableController)target;

        if (GUILayout.Button("Align Spires"))
        {
            if (m_missionTableController.MissionButtonList.Count == 0)
            {
                Debug.Log($"You must assign Mission Button to the Mission Table Controller.");
                return;
            }
            
            Debug.Log("Aligning Spires");

            AlignSpires(m_missionTableController.MissionButtonList);
            
            Debug.Log($"Spires Aligned.");
        }
    }

    void AlignSpires(List<MissionButtonInteractable> spires)
    {
        Vector3 tablePos = m_missionTableController.transform.position;
        tablePos.y = 0;

        foreach (MissionButtonInteractable spire in spires)
        {
            Vector3 spirePos = spire.transform.position;
            spirePos.y = 0;

            Vector3 direction = tablePos - spirePos;

            Quaternion lookDirection = Quaternion.LookRotation(direction, Vector3.up);
            spire.transform.rotation = lookDirection;
        }
    }
}
