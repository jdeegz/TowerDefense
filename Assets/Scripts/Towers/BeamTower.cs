using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class BeamTower : Tower

{
    private BeamProjectile m_activeBeam;
    private Tween m_curTween;
    private float m_curDissolve = 1f;
    private float m_timeUntilFire;


    void Start()
    {
        m_timeUntilFire = 1f / m_towerData.m_fireRate;
    }


    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        //Rotate towards target. This also rotates to exits during Build Phase.
        RotateTowardsTarget();

        Reload();

        m_targetDetectionTimer += Time.deltaTime;
        if (m_targetDetectionTimer >= m_targetDetectionInterval)
        {
            m_targetDetectionTimer = 0f;
            FindTarget();
        }

        if (m_curTarget.GetCurrentHP() <= 0)
        {
            m_curTarget = null;
        }
        
        if (m_curTarget == null)
        {
            StopBeam();
            return;
        }
        
        if (IsTargetInRange(m_curTarget.transform.position) && IsReadyToFire() && IsTargetInSight())
        {
            Fire();
        }
    }

    private bool IsReadyToFire()
    {
        return m_timeUntilFire >= 1f / m_towerData.m_fireRate;
    }

    private void Reload()
    {
        m_timeUntilFire += Time.deltaTime;
    }

    private void Fire()
    {
        //Restart Reload Timer
        m_timeUntilFire = 0;

        //Apply Status Effect
        ApplyStatusEffect();

        //Turn on beam
        StartBeam();
    }

    void StartBeam()
    {
        if (m_activeBeam) return;
        m_activeBeam = ObjectPoolManager.SpawnObject(m_towerData.m_projectilePrefab, m_muzzlePoint.position, Quaternion.identity, ObjectPoolManager.PoolType.Projectile).GetComponent<BeamProjectile>();
        m_activeBeam.StartBeam(m_curTarget.m_targetPoint, m_muzzlePoint);
    }

    void StopBeam()
    {
        if (!m_activeBeam) return;
        m_activeBeam.StopBeam();
        m_activeBeam = null;
    }

    public override void RemoveTower()
    {
        StopBeam();
        base.RemoveTower();
    }

    private void ApplyStatusEffect()
    {
        if (m_statusEffectData == null) return;

        StatusEffect statusEffect = new StatusEffect();
        statusEffect.SetSender(gameObject);
        statusEffect.m_data = m_statusEffectData;
        m_curTarget.ApplyEffect(statusEffect);
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