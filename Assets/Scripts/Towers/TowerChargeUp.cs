using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class TowerChargeUp : Tower
{
    [Header("Audio")]
    [SerializeField] private AudioSource m_secondaryAudioSource;
    
    [Header("Visual Attributes")]
    public VisualEffect m_projectileImpactVFX;

    public VisualEffect m_muzzleChargeVFX;
    public VisualEffect m_muzzleBlastVFX;

    [Space(15)] [GradientUsage(true)]
    public Gradient m_beamGradient;

    public LineRenderer m_projectileLineRenderer;
    public LineRenderer m_projectileLineRenderer_Darken;

    [Space(15)] [GradientUsage(true)]
    public Gradient m_panelGradient;

    public List<MeshRenderer> m_panelMeshRenderers;

    [Header("Data")]
    public float m_stackDropDelayTime;

    public int m_maxStacks;
    public float m_maxBeamWidth = 0.5f;
    public float m_beamDuration = .25f;

    private float m_curStackDropDelay;
    private float m_resetStep;
    private float m_resetTime;
    private float m_curStacks;

    private float CurStacks
    {
        get { return m_curStacks; }
        set
        {
            if (value != m_curStacks)
            {
                m_curStacks = value;
                HandlePanelColor();
            }
        }
    }

    private Vector2 m_scrollOffset;
    private float m_lastStacks;
    private float m_timeUntilBeamOff;
    private Vector3 m_lastTargetPos;
    private bool m_isCharging;
    private Tween m_curTween;
    private float m_curBeamWidth = 0;
    private bool m_beamActive;
    private Collider m_colliderHit;

    void Start()
    {
        DisableBeam();
    }

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        if (m_beamActive)
        {
            return;
        }

        RotateTowardsTarget();

        m_targetDetectionTimer += Time.deltaTime;
        if (m_targetDetectionTimer >= m_targetDetectionInterval)
        {
            m_targetDetectionTimer = 0f;
            FindTarget();
        }

        if (m_curTarget == null)
        {
            ChargeDown();
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
            ChargeUp();

            HandleBeamVisual();

            //If we we are fully charged, and target is in cone of view, fire.
            if (CurStacks >= m_maxStacks && IsTargetInSight())
            {
                //Enable the visual effect.
                StartBeam();
                Fire();
                RequestPlayAudio(m_towerData.m_audioFireClips, m_secondaryAudioSource);
            }
        }
    }

    private void ChargeDown()
    {
        //Dont charge down if we're at 0.
        if (CurStacks <= 0) return;

        //If we were charging, we're no longer charging.
        if (m_isCharging)
        {
            //Restart Stack Removal Countdown
            m_curStackDropDelay = 0f;

            m_isCharging = false;
            m_muzzleChargeVFX.Stop();
        }

        //Delay clock, then remove stacks when met.
        m_curStackDropDelay += Time.deltaTime;

        if (m_curStackDropDelay >= m_stackDropDelayTime && CurStacks > 0)
        {
            CurStacks -= (m_towerData.m_fireRate * m_maxStacks) * Time.deltaTime;
            //Debug.Log($"Charging Down: {CurStacks} / {m_maxStacks}");
        }
    }

    private Color m_chargeColor;
    private float m_chargeNormalizedTime;
    private void ChargeUp()
    {
        //Dont charge up if we're at max stacks.
        if (CurStacks >= m_maxStacks) return;

        if (m_isCharging == false)
        {
            m_isCharging = true;
            m_muzzleChargeVFX.Play();
        }

        //Increase curStacks based on max stacks and fire rate.
        CurStacks += (m_towerData.m_fireRate * m_maxStacks) * Time.deltaTime;

        //Handle the Charge up VFX
        m_chargeNormalizedTime = (float)CurStacks / m_maxStacks;
        m_chargeColor = m_beamGradient.Evaluate(m_chargeNormalizedTime);
        m_muzzleChargeVFX.SetVector4("_Color", m_chargeColor);
        m_muzzleChargeVFX.SetFloat("_Speed", CurStacks / 8);
        m_muzzleChargeVFX.SetFloat("_Rate", CurStacks);
    }

    public override void RequestTowerDisable()
    {
        StopBeam();
        m_muzzleChargeVFX.Stop();
        CurStacks = 0;

        base.RequestTowerDisable();
    }

    private float m_audioPitchMin = 0.5f;
    private float m_audioPitchMax = 1.5f;
    private float m_panelNormalizedTime;
    private Color m_panelColor;
    private void HandlePanelColor()
    {
        if (CurStacks == m_lastStacks) return;

        m_panelNormalizedTime = CurStacks / m_maxStacks;
        m_panelColor = m_panelGradient.Evaluate(m_panelNormalizedTime);
        foreach (MeshRenderer mesh in m_panelMeshRenderers)
        {
            mesh.material.SetColor("_EmissionColor", m_panelColor);
        }

        m_lastStacks = CurStacks;
        
        // AUDIO - TOWER AMBIENT 
        m_audioSource.pitch = Mathf.Lerp(m_audioPitchMin, m_audioPitchMax, m_panelNormalizedTime);
        //m_audioSource.volume = Mathf.Lerp(0, 1, normalizedTime);
        
        if (CurStacks > 0 && !m_audioSource.isPlaying)
        {
            RequestPlayAudioLoop(m_towerData.m_audioLoops[0], m_audioSource);
        }
        else if (CurStacks == 0 && m_audioSource.isPlaying)
        {
            RequestStopAudioLoop(m_audioSource);
        }
    }

    private void Fire()
    {
        //Remove Stacks
        CurStacks *= 0.5f;

        //Deal Damage if we hit the target's collider.
        if (m_colliderHit == m_targetCollider)
        {
            m_curTarget.OnTakeDamage(m_towerData.m_baseDamage);
            //Debug.Log($"{name} fired its beam for {m_towerData.m_baseDamage}.");

            //Apply Shred
            if (m_statusEffectData)
            {
                StatusEffect statusEffect = new StatusEffect(gameObject, m_statusEffectData);
                m_curTarget.ApplyEffect(statusEffect);
            }
        }

        //Play Audio.
        int i = Random.Range(0, m_towerData.m_audioFireClips.Count - 1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);
    }

    void StartBeam()
    {
        if (!m_beamActive)
        {
            m_beamActive = true;
            m_curTween.Kill();
            m_projectileLineRenderer.enabled = true;
            m_projectileLineRenderer_Darken.enabled = true;

            //Debug.Log($"Starting Beam");
            m_curTween = DOTween.To(() => m_curBeamWidth, x => m_curBeamWidth = x, m_maxBeamWidth, m_beamDuration)
                .SetEase(Ease.OutBack)
                .OnUpdate(SetLineWidth)
                .OnComplete(StopBeam);

            //.From(0f)
            //.SetEase(m_startBeamCurve)
            m_muzzleBlastVFX.SetFloat("_BoxLength", Vector3.Distance(m_muzzlePoint.position, m_curTarget.m_targetPoint.position));
            m_muzzleBlastVFX.Play();
            m_projectileImpactVFX.Play();
        }
    }

    void StopBeam()
    {
        m_curTween.Kill();
        //Debug.Log($"Stopping Beam");
        m_curTween = DOTween.To(() => m_curBeamWidth, x => m_curBeamWidth = x, 0f, m_beamDuration)
            .SetEase(Ease.InCirc)
            .OnUpdate(SetLineWidth)
            .OnComplete(DisableBeam);

        //.From(m_maxBeamWidth)
        //.SetEase(m_stopBeamCurve)
        m_projectileImpactVFX.Stop();
    }

    public override void RemoveTower()
    {
        StopBeam();
        base.RemoveTower();
    }

    void DisableBeam()
    {
        m_projectileLineRenderer.enabled = false;
        m_projectileLineRenderer_Darken.enabled = false;
        m_beamActive = false;
        //Debug.Log($"Beam Disabled");
    }

    void SetLineWidth()
    {
        m_projectileLineRenderer.startWidth = m_curBeamWidth;
        m_projectileLineRenderer.endWidth = m_curBeamWidth;

        m_projectileLineRenderer_Darken.startWidth = m_curBeamWidth * 2;
        m_projectileLineRenderer_Darken.endWidth = m_curBeamWidth * 2;

        //Debug.Log($"Current Beam Width: {m_curBeamWidth}");
    }

    private Vector3 m_vfxTarget;
    private Quaternion m_vfxRotation;
    private float m_beamNormalizedTime;
    private Color m_beamColor;
    private void HandleBeamVisual()
    {
        //Setup Target point and rotation
        
        GetPointOnColliderSurface(m_muzzlePoint.position, m_curTarget.m_targetPoint.position, m_targetCollider, out m_vfxTarget, out m_vfxRotation, out m_colliderHit);
        m_projectileImpactVFX.transform.position = m_vfxTarget;
        m_projectileImpactVFX.transform.rotation = m_vfxRotation;


        //Setup LineRenderer Data.
        m_projectileLineRenderer.SetPosition(0, m_muzzlePoint.position);
        m_projectileLineRenderer.SetPosition(1, m_vfxTarget);
        m_projectileLineRenderer_Darken.SetPosition(0, m_muzzlePoint.position);
        m_projectileLineRenderer_Darken.SetPosition(1, m_vfxTarget);

        //Scroll the texture.
        m_scrollOffset.x = -m_maxStacks / 8;
        m_projectileLineRenderer.material.SetVector("_BaseScrollSpeed", m_scrollOffset);

        //Color the texture.
        m_beamNormalizedTime = (float)CurStacks / m_maxStacks;
        m_beamColor = m_beamGradient.Evaluate(m_beamNormalizedTime);
        m_projectileLineRenderer.material.SetColor("_Color", m_beamColor);
        m_projectileImpactVFX.SetVector4("_Color", m_beamColor);

        //Modify Speed & Rate.
        m_projectileImpactVFX.SetFloat("_Rate", CurStacks);
    }

    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        data.m_towerDescription = m_towerData.m_towerDescription;

        //Details string creation.
        string baseDamage;
        baseDamage = $"Damage: {m_towerData.m_baseDamage}{data.m_damageIconString}<br>" +
                     $"Full Charge: {m_towerData.m_fireRate}{data.m_timeIconString}";

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
        data.m_stacks = (int)CurStacks;

        return data;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        SetTurretRotation(data.m_turretRotation);
        CurStacks = data.m_stacks;
    }
}