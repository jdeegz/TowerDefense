using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileData", menuName = "ScriptableObjects/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    public List<AudioClip> m_reloadClips;
    public List<AudioClip> m_impactClips;
}
