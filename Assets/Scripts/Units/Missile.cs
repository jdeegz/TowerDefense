using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Missile : Projectile
{
    public GameObject m_impactEffect;
    public float m_impactRadius;
    public LayerMask m_layerMask;
    public AnimationCurve m_curveLateral;
    public AnimationCurve m_curveHeight;
    public AnimationCurve m_curveDistance;
    
    void Update()
    {
        if (m_enemy) m_targetPos = m_enemy.m_targetPoint.position;
        
        if (CheckTargetDistance())
        {
            DestroyProjectile();
            return;
        }

        TravelToTarget();
    }

    private bool CheckTargetDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, m_targetPos);
        return distanceToTarget <= m_stoppingDistance;
    }

    void TravelToTarget()
    {
        float t = m_elapsedTime;

        //Straight line position at this step.
        m_directPos = Vector3.LerpUnclamped(m_startPos, m_targetPos, m_curveDistance.Evaluate(t));

        //Lateral adjustment.
        Quaternion beeLineRotation = Quaternion.LookRotation(m_targetPos - m_startPos, Vector3.up);
        Vector3 localRightInWorldSpace = beeLineRotation * Vector3.right;
        Vector3 offsetLateral = localRightInWorldSpace * m_curveLateral.Evaluate(t);

        //Vertical adjustment.
        Vector3 offsetHeight = new Vector3(0, m_curveHeight.Evaluate(t), 0);

        //Combine for new position.
        Vector3 newPos = m_directPos + offsetLateral + offsetHeight;

        //Rotation
        Quaternion lookRotation = Quaternion.LookRotation((newPos - transform.position).normalized);
        transform.rotation = lookRotation;

        transform.position = newPos;

        m_elapsedTime += Time.deltaTime * m_projectileSpeed;
    }

    void DestroyProjectile()
    {
        //Deal Damage
        Collider[] hits = Physics.OverlapSphere(transform.position, m_impactRadius, m_layerMask.value);
        if (hits.Length > 0)
        {
            foreach (Collider col in hits)
            {
                EnemyController enemyHit = col.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_projectileDamage);
                
                //Apply Status Effect
                if (m_statusEffect)
                {
                    enemyHit.ApplyEffect(m_statusEffect);
                }
            }
        }
        
        //Spawn VFX
        Vector3 groundPos = transform.position;
        Instantiate(m_impactEffect, groundPos, Quaternion.identity);

        //Destroy this missile.
        Destroy(gameObject);
    }
}