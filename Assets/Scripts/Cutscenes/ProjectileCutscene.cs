using UnityEngine;

public class ProjectileCutscene : Projectile
{
    void Update()
    {
        if (!m_isComplete && IsTargetInStoppingDistance())
        {
            CutsceneRemoveProjectile();
        }

        if (!m_isComplete && !m_enemy)
        {
            CutsceneRemoveProjectile();
        }

        if(!m_isComplete) TravelToTargetFixedUpdate();
    }

    void TravelToTargetFixedUpdate()
    {
        transform.position += transform.forward * (m_projectileSpeed * Time.unscaledDeltaTime);

        //Get Direction
        Vector3 direction = m_targetPos - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"Collision detected.");
        if (collision.collider == null && m_enemy == null) return;
        
        if (m_isComplete) return;
        
        if (collision.collider.gameObject.layer == m_shieldLayer || collision.gameObject == m_enemy.gameObject)
        {
            Debug.Log($"Collided with a shield.");
            Quaternion spawnVFXdirection = Quaternion.LookRotation(collision.transform.position - m_startPos);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, transform.position, spawnVFXdirection, null, ObjectPoolManager.PoolType.ParticleSystem);
            CutsceneRemoveProjectile();
        }

        
    }
    
    public void CutsceneRemoveProjectile()
    {
        Debug.Log($"Removing cutscene projectile.");
        
        Quaternion spawnVFXdirection = Quaternion.LookRotation(m_enemy.transform.position - m_startPos);
        Instantiate(m_hitVFXPrefab, transform.position, spawnVFXdirection);
        
        m_isComplete = true;

        m_isFired = false;
        
        m_enemy = null;
        
        if (m_renderer)
        {
            m_renderer.enabled = false;
        }

        Destroy(gameObject, .5f);
    }
}
