using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class TowerRay : Tower
{
    [Header("Visual Attributes")]
    public VisualEffect m_projectileImpactVFX;

    public VisualEffect m_muzzleVFX;
    //public GameObject m_maxStackVFX;

    [Space(15)] [GradientUsage(true)]
    public Gradient m_beamGradient;

    public LineRenderer m_projectileLineRenderer;
    public LineRenderer m_projectileLineRenderer_Darken;
    public LineRenderer m_projectileLineRenderer_Lightning;

    [Space(15)] [GradientUsage(true)]
    public Gradient m_panelGradient;

    public List<MeshRenderer> m_panelMeshRenderers;

    [Header("Data")]
    public float m_stackDropDelayTime;

    public float m_curStackDropDelay;
    public float m_totalResetTime;
    public float m_damagePower;
    public float m_speedPower;
    public float m_damageCap;
    public float m_speedCap;
    public int m_maxStacks;
    public float m_maxBeamWidth;

    private float m_resetStep;
    private float m_resetTime;
    private int m_curStacks;
    private float m_curDamage;
    private float m_curFireRate;
    private Vector2 m_scrollOffset;
    private float m_timeUntilFire = 99f;
    private int m_lastStacks;
    private Tween m_curTween;
    private float m_curBeamWidth = 0;
    private bool m_beamActive;
    private Collider m_colliderHit;

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        RotateTowardsTarget();

        m_timeUntilFire += Time.deltaTime;

        //RAY STACK MANAGEMENT
        //If we have stacks, increment the time. Time is being set to 0 each fire.
        if (m_curStacks > 0 && m_curStackDropDelay <= m_stackDropDelayTime)
        {
            m_curStackDropDelay += Time.deltaTime;
        }

        //If we've passed delay time, start removing 1 stack at a time until we hit 0 stacks.
        if (m_curStackDropDelay >= m_stackDropDelayTime)
        {
            m_resetTime += Time.deltaTime;
            if (m_resetTime >= m_resetStep)
            {
                m_resetTime = 0;
                if (m_curStacks > 0) m_curStacks--;
                HandlePanelColor();
            }
        }

        m_targetDetectionTimer += Time.deltaTime;
        if (m_curTarget == null && m_targetDetectionTimer >= m_targetDetectionInterval)
        {
            m_targetDetectionTimer = 0f;
            FindTarget();
        }

        if (m_curTarget == null)
        {
            return;
        }

        if (m_curTarget.GetCurrentHP() <= 0)
        {
            if (m_beamActive) StopBeam();
            m_curTarget = null;
            return;
        }

        if (!IsTargetInFireRange(m_curTarget.transform.position))
        {
            if (m_beamActive) StopBeam();
            m_curTarget = null;
        }
        else
        {
            m_curFireRate = m_towerData.m_fireRate * Mathf.Pow(m_speedPower, m_curStacks);
            if (m_curFireRate > m_speedCap)
            {
                m_curFireRate = m_speedCap;
            }

            if (IsTargetInSight())
            {
                if (m_beamActive) HandleBeamVisual();
                
                if (m_timeUntilFire >= 1f / m_curFireRate)
                {
                    if (m_curStacks < m_maxStacks) m_curStacks++;
                    StartBeam();
                    Fire();
                    HandlePanelColor();

                    m_timeUntilFire = 0;
                }
            }
            else if (m_beamActive) StopBeam();
        }
    }

    void SetLineWidth()
    {
        m_projectileLineRenderer.startWidth = m_curBeamWidth;
        m_projectileLineRenderer.endWidth = m_curBeamWidth;

        m_projectileLineRenderer_Darken.startWidth = m_curBeamWidth * 2;
        m_projectileLineRenderer_Darken.endWidth = m_curBeamWidth * 2;

        if (m_projectileLineRenderer_Lightning)
        {
            m_projectileLineRenderer_Lightning.startWidth = m_curBeamWidth * 1.4f;
            m_projectileLineRenderer_Lightning.endWidth = m_curBeamWidth * 0.6f;
        }
    }

    void StopBeam()
    {
        m_curTween.Kill();
        m_curTween = DOTween.To(() => m_curBeamWidth, x => m_curBeamWidth = x, 0f, 0.05f)
            .OnUpdate(SetLineWidth)
            .OnComplete(DisableBeam);

        m_projectileImpactVFX.Stop();
        m_muzzleVFX.Stop();
    }

    void DisableBeam()
    {
        m_projectileLineRenderer.enabled = false;
        m_projectileLineRenderer_Darken.enabled = false;
        if (m_projectileLineRenderer_Lightning) m_projectileLineRenderer_Lightning.enabled = false;
        m_beamActive = false;
    }

    void StartBeam()
    {
        m_curTween.Kill();
        m_beamActive = true;
        m_projectileLineRenderer.enabled = true;
        m_projectileLineRenderer_Darken.enabled = true;
        if (m_projectileLineRenderer_Lightning) m_projectileLineRenderer_Lightning.enabled = true;

        m_curTween = DOTween.To(() => m_curBeamWidth, x => m_curBeamWidth = x, m_maxBeamWidth, 0.2f)
            .OnUpdate(SetLineWidth);

        m_projectileImpactVFX.Play();
        m_muzzleVFX.Play();
    }

    public override void RemoveTower()
    {
        StopBeam();
        base.RemoveTower();
    }

    private void HandleMaxStackVisuals()
    {
        /*if (m_curStacks == m_maxStacks)
        {
            m_maxStackVFX.SetActive(true);
        }
        else
        {
            m_maxStackVFX.SetActive(false);
        }*/
    }

    private void HandlePanelColor()
    {
        //if (m_curStacks == m_lastStacks) return;

        float normalizedTime = (float)m_curStacks / m_maxStacks;
        Color color = m_panelGradient.Evaluate(normalizedTime);
        foreach (MeshRenderer mesh in m_panelMeshRenderers)
        {
            mesh.material.SetColor("_EmissionColor", color);
        }

        //m_lastStacks = m_curStacks;
    }

    private void Fire()
    {
        //Calculate Damage.
        m_curDamage = m_towerData.m_baseDamage * Mathf.Pow(m_damagePower, m_curStacks);
        if (m_curDamage > m_damageCap)
        {
            m_curDamage = m_damageCap;
        }

        //Deal Damage if we hit our target (was Raycast was not interrupted)
        if (m_colliderHit == m_targetCollider)
        {
            m_curTarget.OnTakeDamage(m_curDamage);
            //Debug.Log($"Damage done by {gameObject.name}: {m_curDamage}");
            //Apply Shred
            if (m_statusEffectData)
            {
                StatusEffect statusEffect = new StatusEffect();
                statusEffect.SetSender(gameObject);
                statusEffect.m_data = m_statusEffectData;
                m_curTarget.ApplyEffect(statusEffect);
            }
        }


        //Play Audio.
        int i = Random.Range(0, m_towerData.m_audioFireClips.Count - 1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);

        //Reset Counters.
        m_curStackDropDelay = 0;
        m_resetStep = m_totalResetTime / m_curStacks;
    }


    private void HandleBeamVisual()
    {
        //Setup Target point and rotation
        Vector3 vfxTarget;
        Quaternion vfxRotation;
        GetPointOnColliderSurface(m_muzzlePoint.position, m_curTarget.m_targetPoint.position, m_targetCollider, out vfxTarget, out vfxRotation, out m_colliderHit);
        m_projectileImpactVFX.transform.position = vfxTarget;
        m_projectileImpactVFX.transform.rotation = vfxRotation;

        //Setup LineRenderer Data.
        m_projectileLineRenderer.SetPosition(0, m_muzzlePoint.position);
        m_projectileLineRenderer.SetPosition(1, vfxTarget);
        m_projectileLineRenderer_Darken.SetPosition(0, m_muzzlePoint.position);
        m_projectileLineRenderer_Darken.SetPosition(1, vfxTarget);
        if (m_projectileLineRenderer_Lightning)
        {
            m_projectileLineRenderer_Lightning.SetPosition(0, m_muzzlePoint.position);
            m_projectileLineRenderer_Lightning.SetPosition(1, vfxTarget);
        }

        //Line Width based on Stacks
        /*float width = 0.0f + (m_curStacks - 0f) * (0.5f - 0.0f) / (m_maxStacks - 0f);
        m_projectileLineRenderer.startWidth = width;
        m_projectileLineRenderer.endWidth = width;*/


        //Scroll the texture.
        m_scrollOffset = new Vector2(-m_curFireRate, 0);
        m_projectileLineRenderer.material.SetVector("_BaseScrollSpeed", m_scrollOffset);
        if (m_projectileLineRenderer_Lightning) m_projectileLineRenderer_Lightning.material.SetVector("_BaseScrollSpeed", m_scrollOffset);


        //Color the texture.
        float normalizedTime = (float)m_curStacks / m_maxStacks;
        Color color = m_beamGradient.Evaluate(normalizedTime);
        m_projectileLineRenderer.material.SetColor("_Color", color);
        if (m_projectileLineRenderer_Lightning) m_projectileLineRenderer_Lightning.material.SetColor("_Color", color);
        m_projectileImpactVFX.SetVector4("_Color", color);
        m_muzzleVFX.SetVector4("_Color", color);

        //Modify Speed & Rate.
        m_projectileImpactVFX.SetFloat("_Rate", m_curFireRate);
        m_muzzleVFX.SetFloat("_Speed", m_curStacks / 8);
        m_muzzleVFX.SetFloat("_Rate", m_curStacks);
        //Handle the Charge up VFX
    }

    private bool IsTargetInRange(Vector3 targetPos)
    {
        float distance = Vector3.Distance(transform.position, targetPos);
        return distance <= m_towerData.m_fireRange;
    }

    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        data.m_towerDescription = m_towerData.m_towerDescription;

        //Details string creation.
        string baseDamage;
        baseDamage = $"Base Damage: {m_towerData.m_baseDamage}{data.m_damageIconString} | Damage Cap: {m_damageCap}{data.m_damageIconString}<br>" +
                     $"Base Fire Rate: {m_towerData.m_fireRate}{data.m_timeIconString} | Fire Rate Cap: {m_speedCap}{data.m_timeIconString}";

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
        data.m_stacks = m_curStacks;

        return data;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        SetTurretRotation(data.m_turretRotation);
        m_curStacks = data.m_stacks;
    }
}