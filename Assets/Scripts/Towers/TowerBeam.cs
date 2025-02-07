using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class TowerBeam : Tower

{
    private ProjectileBeam m_activeBeam;
    private Tween m_curTween;
    private float m_curDissolve = 1f;
    private float m_timeUntilFire;
    private bool m_isShooting;
    private EnemyController m_lastTarget;

    private bool IsShooting
    {
        get { return m_isShooting; }
        set
        {
            if (value != m_isShooting)
            {
                m_isShooting = value;
                if (m_isShooting)
                {
                    //TURN ON BEAM
                    StartBeam();
                    RequestPlayAudio(m_towerData.m_audioFireClips);
                    RequestPlayAudioLoop(m_towerData.m_audioLoops[0]);
                }
                else
                {
                    //TURN OFF BEAM
                    StopBeam();
                    RequestStopAudioLoop();
                }
            }
        }
    }


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

        RotateTowardsTarget();

        Reload();

        //Find a new target only if our current target is out of range.
        m_targetDetectionTimer += Time.deltaTime;
        if (m_targetDetectionTimer >= m_targetDetectionInterval)
        {
            m_targetDetectionTimer = 0f;
            FindTarget();
        }
        
        //We found a new target, or no target. Stop shooting.
        if (m_lastTarget != m_curTarget)
        {
            IsShooting = false;
            m_lastTarget = m_curTarget;
        }

        if (m_curTarget == null)
        {
            //StopBeam();
            IsShooting = false;
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
        else if (IsReadyToFire() && IsTargetInSight())
        {
            Fire();
        }
    }

    public override void RequestTowerDisable()
    {
        IsShooting = false;

        base.RequestTowerDisable();
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

        //Turn on/update beam if the new target is different.
        IsShooting = true;
    }

    void StartBeam()
    {
        StopBeam(); //Will stop any active beams.
        m_activeBeam = ObjectPoolManager.SpawnObject(m_towerData.m_projectilePrefab, m_muzzlePoint.position, Quaternion.identity, null, ObjectPoolManager.PoolType.Projectile).GetComponent<ProjectileBeam>();
        m_activeBeam.StartBeam(m_curTarget.m_targetPoint, m_muzzlePoint);
    }

    void StopBeam()
    {
        if (m_activeBeam == null) return;
        m_activeBeam.StopBeam();
        m_activeBeam = null;
    }

    public override void RemoveTower()
    {
        IsShooting = false;
        base.RemoveTower();
    }

    private void ApplyStatusEffect()
    {
        if (m_statusEffectData == null) return;

        StatusEffect statusEffect = new StatusEffect(gameObject, m_statusEffectData);
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