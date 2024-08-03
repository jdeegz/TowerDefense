using System;
using System.Collections.Generic;
using System.Text;
using GameUtil;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class TowerCannon : Tower
{
    public List<Transform> m_muzzlePoints;
    public Projectile[] m_loadedProjectiles;
    private int m_projectileCounter;
    private float m_reloadDelay;
    private float m_timeUntilFire;
    private int m_shotsFired;
    private float m_timeUntilBurst;

    void Start()
    {
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

        m_timeUntilFire += Time.deltaTime;
        m_timeUntilBurst += Time.deltaTime;


        m_targetDetectionTimer += Time.deltaTime;
        if (m_targetDetectionTimer >= m_targetDetectionInterval)
        {
            m_targetDetectionTimer = 0f;
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
            if (IsAbleToInitiateFireSequence() && IsAbleToFire(m_projectileCounter))
            {
                Fire(m_projectileCounter);
                m_timeUntilBurst = 0;
                ++m_shotsFired;

                //Update projectileCounter
                ++m_projectileCounter;
                if (m_projectileCounter >= m_muzzlePoints.Count)
                {
                    m_projectileCounter = 0;
                    m_timeUntilFire = 0;
                }
            }
        }
    }

    bool IsAbleToInitiateFireSequence()
    {
        return m_timeUntilFire >= 1f / m_towerData.m_fireRate;
    }

    bool IsAbleToFire(int i)
    {
        if (m_loadedProjectiles[i] == null) return false;
        if (m_towerData.m_burstFireRate == 0) return true;

        return m_timeUntilBurst >= 1f / m_towerData.m_burstFireRate;
    }


    void Reload(int i)
    {
        //Make the projectile objects
        //GameObject projectileObj = Instantiate(m_towerData.m_projectilePrefab, m_muzzlePoints[i].transform);
        GameObject projectileObj = ObjectPoolManager.SpawnObject(m_towerData.m_projectilePrefab, m_muzzlePoints[i].transform.position, m_muzzlePoints[i].transform.rotation);

        if (projectileObj == null) Debug.Log($"Projectile Obj is null");
        //Position loaded projectile
        projectileObj.transform.SetParent(m_muzzlePoints[i].transform);

        Projectile projectileScript = projectileObj.GetComponent<Projectile>();

        //Assign any necessary effects to the projectile.
        if (m_statusEffectData)
        {
            StatusEffect statusEffect = new StatusEffect();
            statusEffect.SetSender(gameObject);
            statusEffect.m_data = m_statusEffectData;
            projectileScript.SetProjectileStatusEffect(statusEffect);
        }

        //Store the projectiles in a list to pull from later.
        m_loadedProjectiles[i] = projectileScript;
        if (projectileScript == null) Debug.Log($"Projectile Script is null");
    }

    private void Fire(int fireIndex)
    {
        //Pull projectile from the pool of loaded projectiles.
        Projectile projectileScript = m_loadedProjectiles[m_projectileCounter];

        //Unparent the projectile so it stops rotating around the tower.
        projectileScript.transform.parent = ObjectPoolManager.SetParentObject(ObjectPoolManager.PoolType.Projectile).transform;
        m_loadedProjectiles[fireIndex] = null;

        //Give the projectile purpose.
        projectileScript.SetProjectileData(m_curTarget, m_curTarget.m_targetPoint, m_towerData.m_baseDamage, projectileScript.transform.position);

        //Play fire Sound
        int i = Random.Range(0, m_towerData.m_audioFireClips.Count - 1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);

        //Play fire Animation
        //m_animator.SetTrigger("Fire");
        FireVFX();

        //Start a reload timer for this index.
        //Source https://github.com/Mr-sB/UnityTimer
        int reloadIndex = fireIndex;
        Timer.DelayAction(m_reloadDelay, () => { Reload(reloadIndex); }, null, Timer.UpdateMode.GameTime, autoDestroyOwner: this);
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