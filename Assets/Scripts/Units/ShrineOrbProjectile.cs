using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

public class ShrineOrbProjectile : MonoBehaviour
{
    public GameObject m_deathVFX;
    public float m_projectileSpeed;
    
    private GathererController m_targetGatherer;
    private float m_stoppingDistance = 0.05f;
    private bool m_isComplete;
    
    public void Setup(GathererController gatherer)
    {
        m_isComplete = false;
        m_targetGatherer = gatherer;
    }

    void FixedUpdate()
    {
        if (m_isComplete) return;
        
        // Move to target
        transform.position += transform.forward * (m_projectileSpeed * Time.fixedDeltaTime);

        // Look at target
        Vector3 direction = m_targetGatherer.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
        
        // Remove Object if we're near.
        if (Vector3.Distance(transform.position, m_targetGatherer.transform.position) <= m_stoppingDistance)
        {
            RemoveObject();
        }
    }

    void RemoveObject()
    {
        // Spawn Death VFX
        ObjectPoolManager.SpawnObject(m_deathVFX, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
        
        // Remove this object
        m_isComplete = true;
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Projectile);
    }
}
