using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class TowerBlast : Tower
{
    [SerializeField] protected Transform m_muzzlePointAlt;
    private bool m_useAltMuzzle;
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private int m_shotsFired;
    private float m_timeUntilBurst;

    private string m_fireTriggerString = "Fire";
    private string m_fireAltTriggerString = "Fire_Alt";
    
    private Transform m_curMuzzleTransform;
    private String m_curMuzzleTriggerString;
    

    void Start()
    {
        m_timeUntilFire = 999f;
        m_timeUntilBurst = 999f;
        m_curMuzzleTransform = m_muzzlePoint;
        m_curMuzzleTriggerString = m_fireTriggerString;
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

        m_targetDetectionTimer += Time.deltaTime;
        if (m_targetDetectionTimer >= m_targetDetectionInterval)
        {
            m_targetDetectionTimer = Random.Range(-2 * Time.deltaTime, 2 * Time.deltaTime);
            FindTarget();
        }

        if (m_curTarget == null)
        {
            m_shotsFired = 0;
            return;
        }

        if (m_curTarget.GetCurrentHP() <= 0)
        {
            m_curTarget = null;
            return;
        }

        if (!IsTargetInFireRange(m_curTarget.transform.position))
        {
            m_curTarget = null;
        }
        else
        {
            //If we have elapsed time, and are looking at the target, fire.
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && m_timeUntilBurst >= m_towerData.m_burstFireRate && IsTargetInSight())
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

    private Quaternion m_projectileRotation;
    private Vector3 m_projectileDirection;
    private void Fire()
    {
        if (m_muzzlePointAlt != null) // If we have multiple muzzles, alternate between them.
        {
            m_curMuzzleTransform = m_useAltMuzzle ? m_muzzlePointAlt : m_muzzlePoint;
            m_curMuzzleTriggerString = m_useAltMuzzle ? m_fireAltTriggerString : m_fireTriggerString;
            m_useAltMuzzle = !m_useAltMuzzle;
        }

        m_projectileDirection = m_curTarget.transform.position - m_curMuzzleTransform.position;
        m_projectileRotation = Quaternion.LookRotation(m_projectileDirection);
        GameObject projectileObj = ObjectPoolManager.SpawnObject(m_towerData.m_projectilePrefab, m_curMuzzleTransform.position, m_projectileRotation, null, ObjectPoolManager.PoolType.Projectile);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        
        projectileScript.SetProjectileData(m_curTarget, m_curTarget.m_targetPoint, m_towerData.m_baseDamage, m_curMuzzleTransform.position, gameObject, m_statusEffectData);

        // AUDIO
        int i = Random.Range(0, m_towerData.m_audioFireClips.Count);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);
        
        // ANIMATION
        m_animator.SetTrigger(m_curMuzzleTriggerString);
        
        // VFX
        FireVFX();
    }
    
    public override void FireVFX()
    {
        if (!m_towerData.m_muzzleFlashPrefab) return;

        ObjectPoolManager.SpawnObject(m_towerData.m_muzzleFlashPrefab, m_curMuzzleTransform.position, m_curMuzzleTransform.rotation, null, ObjectPoolManager.PoolType.ParticleSystem);
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