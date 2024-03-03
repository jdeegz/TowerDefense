using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class BasicTower : Tower
{
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private int m_shotsFired;
    private float m_timeUntilBurst;

    void Start()
    {
        m_timeUntilFire = 999f;
        m_timeUntilBurst = 999f;
    }

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        RotateTowardsTarget();

        m_timeUntilFire += Time.deltaTime;
        m_timeUntilBurst += Time.deltaTime;

        if (m_curTarget == null)
        {
            m_shotsFired = 0;
            FindTarget();
            return;
        }

        if (!IsTargetInRange(m_curTarget.transform.position))
        {
            m_curTarget = null;
        }
        else
        {
            //If we have elapsed time, and are looking at the target, fire.
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && m_timeUntilBurst >= m_towerData.m_burstFireRate &&
                IsTargetInSight())
            {
                Fire();
                m_timeUntilFire = 0;
                ++m_shotsFired;

                //Reset Burst Fire counters
                if (m_shotsFired >= m_towerData.m_burstSize)
                {
                    m_timeUntilBurst = 0;
                    m_shotsFired = 0;
                }
            }
        }
    }

    private void Fire()
    {
        GameObject projectileObj = Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoint.position, m_turretPivot.rotation);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        if (m_statusEffectData)
        {
            StatusEffect statusEffect = new StatusEffect();
            statusEffect.SetTowerSender(this);
            statusEffect.m_data = m_statusEffectData;
            projectileScript.SetProjectileStatusEffect(statusEffect);
        }
        projectileScript.SetProjectileData(m_curTarget, m_curTarget.m_targetPoint, m_towerData.m_baseDamage, m_muzzlePoint.position);

        int i = Random.Range(0, m_towerData.m_audioFireClips.Count - 1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);
        m_animator.SetTrigger("Fire");
    }
    
    void FireVFX()
    {
        m_fireVFX.SetActive(true);
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
            HasTargets(true);
        }
        else
        {
            HasTargets(false);
        }
    }

    private void HasTargets(bool b)
    {
        if (m_hasTargets == b) return;
        
        m_hasTargets = b;
        if (m_hasTargets)
        {
            m_animator.SetTrigger("TargetAcquired");
        }
        else
        {
            m_animator.SetTrigger("TargetUnacquired");
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
        //If consistent fire rate:
        string baseDamage;
        baseDamage = $"Damage: {m_towerData.m_baseDamage}{data.m_damageIconString}<br>" +
                     $"Fire Rate: {m_towerData.m_fireRate}{data.m_timeIconString}";
        
        //If burst fire:
        if (m_towerData.m_burstFireRate > 0)
        {
            float burstRate = 1f / m_towerData.m_burstFireRate;
            string burstRateString = burstRate.ToString("F1");
            
            baseDamage = $"Damage: {m_towerData.m_baseDamage}{data.m_damageIconString}<br>" +
                         $"Burst Fire Rate: {burstRateString}{data.m_timeIconString}<br>" +
                         $"Burst Size: {m_towerData.m_burstSize}";
        }
        
        string statusEffect = null;
        if (m_statusEffectData)
        {
            statusEffect = data.BuildStatusEffectString(m_statusEffectData);
        }
        
        StringBuilder descriptionBuilder = new StringBuilder();

        if (!string.IsNullOrEmpty(baseDamage))
            descriptionBuilder.Append(baseDamage);

        if (!string.IsNullOrEmpty(statusEffect))
            descriptionBuilder.Append(statusEffect);

        data.m_towerDetails = descriptionBuilder.ToString();
        return data;
    }
}