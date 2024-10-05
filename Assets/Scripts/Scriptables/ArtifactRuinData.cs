using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ArtifactRuinData", menuName = "ScriptableObjects/ArtifactRuinData")]
public class ArtifactRuinData : RuinData
{
    public int m_startingCharge = 1; // How many charges we can store
}
