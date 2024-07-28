using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class StatusEffectSource : MonoBehaviour
{
    [SerializeField] private VisualEffect m_visualEffect;
    
    public void SetStatusEffectSource(GameObject obj) //Used for informing visual effects where they came from, like the Slow Tower.
    {
        Vector3 pos = obj.transform.position;
        
        Tower tower = obj.GetComponent<Tower>();
        if (tower != null)
        {
            pos = tower.GetTowerMuzzlePoint();
        }
        
        m_visualEffect.SetVector3("_SourcePosition", pos);
    }
}
