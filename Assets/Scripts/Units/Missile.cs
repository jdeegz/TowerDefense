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
        if (CheckTargetDistance())
        {
            DestroyProjectile();
            return;
        }

        TravelToTarget();
    }

    private bool CheckTargetDistance()
    {
        float distanceToTarget = Vector3.Distance(transform.position, m_target.position);
        return distanceToTarget <= m_stoppingDistance;
    }

    void TravelToTarget()
    {
        float t = m_elapsedTime;

        //Straight line position at this step.
        m_directPos = Vector3.LerpUnclamped(m_startPos, m_target.position, m_curveDistance.Evaluate(t));

        //Lateral adjustment.
        Quaternion beeLineRotation = Quaternion.LookRotation(m_target.position - m_startPos, Vector3.up);
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
        if (hits.Length <= 0) return;
        foreach (Collider col in hits)
        {
            UnitEnemy enemyHit = col.GetComponent<UnitEnemy>();
            enemyHit.OnTakeDamage(m_projectileDamage);
        }

        //Spawn VFX
        Vector3 groundPos = new Vector3(transform.position.x, 0.1f, transform.position.z);
        Instantiate(m_impactEffect, groundPos, Quaternion.identity);

        //Destroy this missile.
        Destroy(gameObject);
    }
}