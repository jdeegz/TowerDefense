using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

public class ArcTowerController : Tower
{
    public VisualEffect m_flameTowerProjectile;
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private GameObject m_activeProjectileObj;
    private float m_rotationModifier = 1;

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        if (m_towerData.m_hasSecondaryAttack) HandleSecondaryAttack();

        RotateTowardsTarget();

        if (m_curTarget == null)
        {
            //If target is not in range, destroy the flame cone if there is one.
            m_flameTowerProjectile.Stop();
            
            /*if (m_activeProjectileObj != null)
            {
                //Destroy(m_activeProjectileObj);
                //ObjectPoolManager.OrphanObject(m_activeProjectileObj, 0.25f);
                
            }*/

            FindTarget();
            return;
        }


        if (!IsTargetInRange(m_curTarget.transform.position) || m_curTarget.GetCurrentHP() <= 0)
        {
            m_rotationModifier = 1;
            m_curTarget = null;
        }
        else
        {
            m_timeUntilFire += Time.deltaTime;

            //If we have elapsed time, and are looking at the target, fire.
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && IsTargetInSight())
            {
                if (m_activeProjectileObj == null)
                {
                    //m_activeProjectileObj = Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation, m_muzzlePoint.transform);
                    //m_activeProjectileObj = ObjectPoolManager.SpawnObject(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation);
                    //m_activeProjectileObj.transform.SetParent(m_muzzlePoint.transform);
                    m_flameTowerProjectile.Play();
                }

                //int a = Random.Range(0, m_towerData.m_audioFireClips.Count);
                //m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[0]);
                Fire();
                m_timeUntilFire = 0;
            }
        }
    }

    private float m_timeUntilSecondaryFire;

    private void HandleSecondaryAttack()
    {
        m_timeUntilSecondaryFire += Time.deltaTime;

        if (m_timeUntilSecondaryFire >= 1f / m_towerData.m_secondaryfireRate)
        {
            //Reset Counter
            m_timeUntilSecondaryFire = 0f;

            //Spawn VFX
            ObjectPoolManager.SpawnObject(m_towerData.m_secondaryProjectilePrefab, transform.position, Quaternion.identity, ObjectPoolManager.PoolType.ParticleSystem);
            Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_secondaryfireRange, m_layerMask.value);

            //Find enemies and deal damage
            if (hits.Length <= 0) return;
            for (int i = 0; i < hits.Length; ++i)
            {
                // Target is within the cone.
                EnemyController enemyHit = hits[i].transform.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_towerData.m_secondaryDamage);
            }
        }
    }

    private void Fire()
    {
        //Get all the enemies in the cone and deal damage to them. VFX is visual layer ontop of that.
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_targetRange, m_layerMask.value);
        if (hits.Length <= 0) return;

        for (int i = 0; i < hits.Length; ++i)
        {
            Vector3 direction = hits[i].transform.position - transform.position;
            float coneAngleCosine = Mathf.Cos(Mathf.Deg2Rad * (m_towerData.m_fireConeAngle / 2f));

            if (Vector3.Dot(direction.normalized, m_turretPivot.forward.normalized) > coneAngleCosine && IsTargetInRange(hits[i].transform.position))
            {
                // Target is within the cone.
                EnemyController enemyHit = hits[i].transform.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_towerData.m_baseDamage);

                if (m_statusEffectData)
                {
                    StatusEffect statusEffect = new StatusEffect();
                    statusEffect.SetTowerSender(this);
                    statusEffect.m_data = m_statusEffectData;
                    enemyHit.ApplyEffect(statusEffect);
                }
            }
        }
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_targetRange, m_layerMask.value);
        float closestDistance = 999;
        int closestIndex = -1;
        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; ++i)
            {
                float distance = Vector3.Distance(transform.position, hits[i].transform.position);
                if (distance <= closestDistance)
                {
                    closestIndex = i;
                    closestDistance = distance;
                }
            }

            m_curTarget = hits[closestIndex].transform.GetComponent<EnemyController>();
        }
    }

    private bool IsTargetInSight()
    {
        Vector3 directionOfTarget = m_curTarget.transform.position - transform.position;
        return Vector3.Angle(m_turretPivot.transform.forward, directionOfTarget) <= m_facingThreshold;
    }

    private bool IsTargetInRange(Vector3 targetPos)
    {
        return Vector3.Distance(transform.position, targetPos) < m_towerData.m_fireRange;
    }

    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        data.m_towerDescription = m_towerData.m_towerDescription;

        //Details string creation.
        string baseDamage = null;
        if (m_towerData.m_baseDamage > 0)
        {
            baseDamage = $"Damage: {m_towerData.m_baseDamage}{data.m_damageIconString}<br>" +
                         $"Fire Rate: {m_towerData.m_fireRate}{data.m_timeIconString}<br>" +
                         $"Cone: {m_towerData.m_fireConeAngle} Degrees";
        }
        else
        {
            baseDamage = $"Fire Rate: {m_towerData.m_fireRate}{data.m_timeIconString}<br>" +
                         $"Cone: {m_towerData.m_fireConeAngle} Degrees";
        }

        //If has secondary attack (Eruption)
        string secondaryDamage = null;
        if (m_towerData.m_hasSecondaryAttack)
        {
            secondaryDamage = $"<br>" +
                              $"Eruption Damage: {m_towerData.m_secondaryDamage}{data.m_damageIconString}<br>" +
                              $"Eruption Rate: {m_towerData.m_secondaryfireRate}{data.m_timeIconString}";
        }

        //If tower applies a status effect.
        string statusEffect = null;
        if (m_statusEffectData)
        {
            statusEffect = data.BuildStatusEffectString(m_statusEffectData);
        }

        StringBuilder descriptionBuilder = new StringBuilder();

        if (!string.IsNullOrEmpty(baseDamage))
            descriptionBuilder.Append(baseDamage);

        if (!string.IsNullOrEmpty(secondaryDamage))
            descriptionBuilder.Append(secondaryDamage);

        if (!string.IsNullOrEmpty(statusEffect))
            descriptionBuilder.Append(statusEffect);

        data.m_towerDetails = descriptionBuilder.ToString();
        return data;
    }
    
    public override TowerUpgradeData GetUpgradeData()
    {
        TowerUpgradeData data = new TowerUpgradeData();

        data.m_turretRotation = GetTurretRotation();

        return data;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        SetTurretRotation(data.m_turretRotation);
    }
}