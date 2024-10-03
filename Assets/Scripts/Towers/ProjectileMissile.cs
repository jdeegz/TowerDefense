using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ProjectileMissile : Projectile
{
    public GameObject m_impactEffect;
    public float m_impactRadius;
    public LayerMask m_areaLayerMask;
    public LayerMask m_raycastLayerMask;
    public float m_lookSpeed = 10;
    public float m_lookAcceleration = 5;
    public float m_speedAcceleration = 0.3f;

    private float m_storedProjectileSpeed;
    private float m_storedLookSpeed;

    void Awake()
    {
        m_storedProjectileSpeed = m_projectileSpeed;
        m_storedLookSpeed = m_lookSpeed;
    }

    void OnEnable()
    {
        //Reset Data
        m_projectileSpeed = m_storedProjectileSpeed;
        m_lookSpeed = m_storedLookSpeed;
        if (m_renderer) m_renderer.enabled = true;
    }

    void FixedUpdate()
    {
        if (!m_isComplete && IsTargetInStoppingDistance())
        {
            DealDamage();
            RemoveProjectile();
        }

        if (m_isFired)
        {
            TravelToTargetFixedUpdate();
        }
        
        if (m_enemy)
        {
            m_targetPos = m_enemy.m_targetPoint.position;
            
            if (m_enemy.GetCurrentHP() <= 0)
            {
                m_enemy = null;
            }
        }
    }

    void TravelToTargetFixedUpdate()
    {
        //Rotate towards Target.
        Vector3 direction = m_targetPos - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_lookSpeed * Time.fixedDeltaTime);

        //Move Forward.
        transform.position += transform.forward * (m_projectileSpeed * Time.fixedDeltaTime);

        //Increase Lookspeed (greatly)
        var lookStep = (m_lookAcceleration + Time.fixedDeltaTime);
        m_lookSpeed += lookStep;

        //Increase Move Speed up to 50%
        var speedStep = (m_speedAcceleration + Time.fixedDeltaTime);
        m_projectileSpeed += speedStep;
    }

    void DealDamage()
    {
        //Spawn VFX
        ObjectPoolManager.SpawnObject(m_impactEffect, transform.position, Util.GetRandomRotation(Quaternion.identity, new Vector3(0, 180, 0)), null, ObjectPoolManager.PoolType.ParticleSystem);

        //Find affected enemies
        Collider[] hits = Physics.OverlapSphere(m_targetPos, m_impactRadius, m_areaLayerMask.value);
        if (hits.Length <= 0)
        {
            return;
        }

        foreach (Collider col in hits)
        {
            //Shoot a ray to each hit. If we hit a shield we stop and go to the next Sphere Overlap hit.
            Vector3 rayDirection = (col.bounds.center - transform.position).normalized;
            //float rayLength = Vector3.Distance(transform.position, col.transform.position);

            Ray ray = new Ray(transform.position, rayDirection);
            RaycastHit[] raycastHits = Physics.RaycastAll(ray, Mathf.Infinity, m_raycastLayerMask.value);

            if (raycastHits.Length == 0)
            {
                //Debug.Log($"No hits.");
                return;
            }

            //Check each hit's layer, if we hit a shield before we hit our target (ideally the last item in our list) escape.
            for (int i = 0; i < raycastHits.Length; i++)
            {
                if (raycastHits[i].collider.gameObject.layer == m_shieldLayer)
                {
                    //We hit the shield.
                    //In the future we may want to tell the enemy we hit their shield so they can animate.
                    return;
                }
            }

            EnemyController enemyHit = col.GetComponent<EnemyController>();
            enemyHit.OnTakeDamage(m_projectileDamage);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, enemyHit.transform.position, transform.rotation, null, ObjectPoolManager.PoolType.ParticleSystem);

            //Apply Status Effect
            if (m_statusEffect != null)
            {
                enemyHit.ApplyEffect(m_statusEffect);
            }
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        if (m_isComplete) return;

        if (collision.collider.gameObject.layer == m_shieldLayer)
        {
            Quaternion spawnVFXdirection = Quaternion.LookRotation(collision.transform.position - m_startPos);
            ObjectPoolManager.SpawnObject(m_hitVFXPrefab, transform.position, spawnVFXdirection, null, ObjectPoolManager.PoolType.ParticleSystem);
            DealDamage();
            RemoveProjectile();
        }
    }
}