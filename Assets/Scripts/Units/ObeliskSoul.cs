using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;

public class ObeliskSoul : MonoBehaviour
{
    public float m_moveDuration = 2f;
    public VisualEffect m_soulVFX;
    
    private Vector3 m_endPos;
    private Obelisk m_obelisk;
    private Tween m_tweenToObelisk;
    private int m_soulValue;
    
    public void SetupSoul(Vector3 endPos, Obelisk obelisk, int soulValue)
    {
        m_endPos = endPos;
        m_obelisk = obelisk;
        m_soulValue = soulValue;
        HandleMovement();
    }

    void HandleMovement()
    {
        m_tweenToObelisk = gameObject.transform.DOJump(m_endPos, 2, 1, m_moveDuration).OnComplete(RequestObeliskCharge);
        m_tweenToObelisk.Play();
    }

    void RequestObeliskCharge()
    {
        m_obelisk.IncreaseObeliskCharge(m_soulValue);
        m_soulVFX.Stop();
        ObjectPoolManager.OrphanObject(gameObject, 1.2f, ObjectPoolManager.PoolType.ParticleSystem); //1.2f is the max life duration of the vfx particle.
    }
}
