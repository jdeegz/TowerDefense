using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TearData", menuName = "ScriptableObjects/TearData")]
public class TearData : ScriptableObject
{
    public string m_tearName;
    
    [TextArea(10,4)]
    public string m_tearDescription;
    
    [TextArea(10,4)]
    public string m_tearDetails;

    public AudioClip m_audioSpawnerCreated;
    public List<AudioClip> m_audioSpawnerActiveLoops;
}
