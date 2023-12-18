using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (fileName = "ResourceManagerData", menuName = "ScriptableObjects/ResourceManagerData")]
public class ResourceManagerData : ScriptableObject
{
    
    [Header("Starting Resources")]
    public int m_startingStone;
    public int m_startingWood;
    
    [Header("Ruin Data")]
    public GameObject m_ruinObj;
    public int m_totalRuinsPossible = 4; //Total number of ruins possible in this mission.
    public int m_ruinsChance = 1; //1 in Chance of discovering a ruin on depletion.
    public int m_minDepletions = 0; //Number of depletions needed before ruins can be found.
}
