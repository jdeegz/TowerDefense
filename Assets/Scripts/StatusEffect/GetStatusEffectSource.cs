using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class StatusEffectSource : MonoBehaviour
{
    [SerializeField] private VisualEffect m_visualEffect;
    
    public void SetStatusEffectSource(Tower tower)
    {
        m_visualEffect.SetVector3("_SourcePosition", tower.GetTowerMuzzlePoint());
    }
}
