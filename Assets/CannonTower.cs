using System;
using System.Collections.Generic;
using System.Text;
using GameUtil;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class CannonTower : Tower
{
    public List<Transform> m_muzzlePoints;
    public Projectile[] m_loadedProjectiles;
    private int m_projectileCounter;
    private float m_reloadDelay;
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private int m_shotsFired;
    private float m_timeUntilBurst;

    void Start()
    {
        m_timeUntilFire = 999f;
        m_timeUntilBurst = 999f;
        //Define the size of the projectile collection we want.
        m_loadedProjectiles = new Projectile[m_muzzlePoints.Count];

        //The duration we wait to reload is the number of missiles we launch + 1 * the fire rate.
        m_reloadDelay = 1f / m_towerData.m_fireRate / 2;
        for (var i = 0; i < m_muzzlePoints.Count; ++i)
        {
            Reload(i);
        }
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
            if (IsAbleToInitiateFireSequence() && IsAbleToFire() && IsTargetInSight())
            {
                Fire();
                m_timeUntilBurst = 0;
                ++m_shotsFired;

                //Reset Burst Fire counters
                if (m_shotsFired >= m_towerData.m_burstSize)
                {
                    m_timeUntilFire = 0;
                    m_shotsFired = 0;
                }
            }
        }
    }

    bool IsAbleToInitiateFireSequence()
    {
        return m_timeUntilFire >= 1f / m_towerData.m_fireRate;
    }

    bool IsAbleToFire()
    {
        bool b;
        if (m_towerData.m_burstFireRate == 0) return true;

        return m_timeUntilBurst >= 1f / m_towerData.m_burstFireRate;
    }


    void Reload(int i)
    {
        //Make the projectile objects
        GameObject projectileObj = Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoints[i].transform);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();

        //Assign any necessary effects to the projectile.
        if (m_statusEffectData)
        {
            StatusEffect statusEffect = new StatusEffect();
            statusEffect.SetTowerSender(this);
            statusEffect.m_data = m_statusEffectData;
            projectileScript.SetProjectileStatusEffect(statusEffect);
        }

        //Store the projectiles in a list to pull from later.
        m_loadedProjectiles[i] = projectileScript;
        Debug.Log($"Cannon Tower: Reloaded Index {i}.");
    }

    private void Fire()
    {
        Debug.Log($"Cannon Tower: Fired Index {m_projectileCounter}.");
        //Pull projectile from the pool of loaded projectiles.
        Projectile projectileScript = m_loadedProjectiles[m_projectileCounter];

        //Unparent the projectile so it stops rotating around the tower.
        projectileScript.transform.parent = null;
        m_loadedProjectiles[m_projectileCounter] = null;

        //Give the projectile purpose.
        projectileScript.SetProjectileData(m_curTarget, m_curTarget.m_targetPoint, m_towerData.m_baseDamage, projectileScript.transform.position);

        //Play fire Sound
        int i = Random.Range(0, m_towerData.m_audioFireClips.Count - 1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);

        //Play fire Animation
        m_animator.SetTrigger("Fire");

        //Start a reload timer for this index.
        //Source https://github.com/Mr-sB/UnityTimer
        Debug.Log($"Cannon Tower: Reload Initiated for Index {m_projectileCounter}");
        int reloadIndex = m_projectileCounter;
        Timer.DelayAction(m_reloadDelay, () => { Reload(reloadIndex); }, null, Timer.UpdateMode.GameTime, autoDestroyOwner: this);

        //Update projectileCounter
        ++m_projectileCounter;
        if (m_projectileCounter >= m_muzzlePoints.Count) m_projectileCounter = 0;
    }

    //Fired via a keyframe in Animation.
    void FireVFX()
    {
        if (!m_fireVFX) return;

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