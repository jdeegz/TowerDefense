using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MissionContainer", menuName = "ScriptableObjects/MissionContainer")]
public class MissionContainerData : ScriptableObject
{
    public MissionData[] m_MissionList;
}

/*[Serializable]
public class MissionCategory
{
    public string m_categoryName;
    public MissionData[] m_categoryMissionList;
}*/
