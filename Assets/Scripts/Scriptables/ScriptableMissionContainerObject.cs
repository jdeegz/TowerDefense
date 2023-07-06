using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MissionContainer", menuName = "Mission/MissionContainer")]
public class ScriptableMissionContainerObject : ScriptableObject
{
    [FormerlySerializedAs("m_MissionContainer")] public ScriptableMissionDataObject[] m_MissionList;
}
