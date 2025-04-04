using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

public class TowerArc : Tower
{
    public VisualEffect m_flameTowerProjectile;
    private bool m_isShooting;
    private bool IsShooting
    {
        get { return m_isShooting; }
        set {
            if (value != m_isShooting)
            {
                m_isShooting = value;
                if (m_isShooting)
                {
                    //TURN ON FLAMES
                    m_flameTowerProjectile.Play();
                    RequestPlayAudio(m_towerData.m_audioFireClips);
                    RequestPlayAudioLoop(m_towerData.m_audioLoops[0]);
                    HandleFireAnims(true);
                }
                else
                {
                    //TURN OFF FLAMES
                    m_flameTowerProjectile.Stop();
                    RequestStopAudioLoop();
                    HandleFireAnims(false);
                }
            } 
        }
    }
    
    private void HandleFireAnims(bool isShooting)
    {
        m_animator.SetBool("HasTargets", isShooting);
    }
    
    private float m_timeUntilFire;

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        if (m_towerData.m_hasSecondaryAttack) HandleSecondaryAttack();

        RotateTowardsTarget();

        m_targetDetectionTimer += Time.deltaTime;
        if (m_targetDetectionTimer >= m_targetDetectionInterval)
        {
            m_targetDetectionTimer = 0f;
            FindTarget();
        }

        if (m_curTarget == null)
        {
            //If target is not in range, destroy the flame cone if there is one.
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
        else
        {
            m_timeUntilFire += Time.deltaTime;

            //If we have elapsed time, and are looking at the target, fire.
            if (m_timeUntilFire >= 1f / m_towerData.m_fireRate && IsTargetInSight())
            {
                IsShooting = true;
                
                //m_flameTowerProjectile.Play();
                Fire();
                m_timeUntilFire = 0;
            }
        }
    }

    public override void RequestTowerDisable()
    {
        IsShooting = false;
        //m_flameTowerProjectile.Stop();

        base.RequestTowerDisable();
    }

    private float m_timeUntilSecondaryFire;

    private void HandleSecondaryAttack()
    {
        m_timeUntilSecondaryFire += Time.deltaTime;

        if (m_timeUntilSecondaryFire >= 1f / m_towerData.m_secondaryfireRate)
        {
            //Reset Counter
            m_timeUntilSecondaryFire = 0f;

            // VISUAL
            ObjectPoolManager.SpawnObject(m_towerData.m_secondaryProjectilePrefab, transform.position, Quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
            
            // AUDIO
            RequestPlayAudio(m_towerData.m_audioSecondaryFireClips);
            
            // COLLISION
            Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_secondaryfireRange, m_layerMask.value);

            //Find enemies and deal damage
            if (hits.Length <= 0) return;
            foreach (Collider col in hits)
            {
                EnemyController enemyHit = col.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_towerData.m_secondaryDamage);
            }
        }
    }

    private Vector3 m_hitPos;
    private Vector3 m_direction;
    private float m_coneAngleCosine;
    
    private void Fire()
    {
        //Get all the enemies in the cone and deal damage to them. VFX is visual layer ontop of that.
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_targetRange, m_layerMask.value);
        if (hits.Length <= 0) return;

        for (int i = 0; i < hits.Length; ++i)
        {
            m_hitPos = hits[i].transform.position;
            m_hitPos.y = m_muzzlePoint.transform.position.y;

            m_direction = m_hitPos - m_muzzlePoint.transform.position;
            m_coneAngleCosine = Mathf.Cos(Mathf.Deg2Rad * (m_towerData.m_fireConeAngle / 2f));

            if (Vector3.Dot(m_direction.normalized, m_muzzlePoint.forward.normalized) > m_coneAngleCosine && IsTargetInFireRange(m_hitPos))
            {
                // Target is within the cone.
                EnemyController enemyHit = hits[i].transform.GetComponent<EnemyController>();
                enemyHit.OnTakeDamage(m_towerData.m_baseDamage);

                if (m_statusEffectData)
                {
                    StatusEffect statusEffect = new StatusEffect(gameObject, m_statusEffectData);
                    enemyHit.ApplyEffect(statusEffect);
                }
            }
        }
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