using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestInfoSO", menuName = "ScriptableObjects/QuestInfoSO")]
public class QuestInfoSO : ScriptableObject
{
    [field: SerializeField]
    public string m_id { get; private set; }

    [Header("General")]
    public string m_displayName;
    
    [Header("Requirements")]
    public int m_levelRequirement;
    public bool m_byPassRequirements;
    public QuestInfoSO[] m_questPrerequisites;

    [Header("Steps")]
    public GameObject[] m_questStepPrefabs;

    [Header("Rewards")]
    public int m_woodReward;

    public int m_experienceReward;
    
    private void OnValidate()
    {
#if UNITY_EDITOR
        m_id = name;
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}