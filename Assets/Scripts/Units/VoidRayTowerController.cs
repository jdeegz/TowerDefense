using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class VoidRayTowerController : Tower
{
    public LineRenderer m_projectileLineRenderer;
    public GameObject m_maxStackVFX;
    public float m_stackDropDelayTime;
    public float m_curStackDropDelay;
    public float m_totalResetTime;
    private float m_resetStep;
    private float m_resetTime;
    public float m_damagePower;
    public float m_speedPower;
    public float m_damageCap;
    public float m_speedCap;
    public int m_maxStacks;
    public Gradient m_beamGradient;
    [GradientUsage(true)]
    public Gradient m_panelGradient;
    public List<MeshRenderer> m_panelMeshRenderers;


    private int m_curStacks;
    private float m_curDamage;
    private float m_curFireRate;
    private Vector2 m_scrollOffset;
    private float m_timeUntilFire = 99f;
    private float m_facingThreshold = 10f;
    private int m_lastStacks;

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        if (m_maxStackVFX) HandleMaxStackVisuals();

        RotateTowardsTarget();
        
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

        //FINDING TARGET
        if (m_curTarget == null)
        {
            m_projectileLineRenderer.enabled = false;
            FindTarget();
            return;
        }

        if (!IsTargetInRange(m_curTarget.transform.position))
        {
            m_curTarget = null;
        }
        else
        {
            m_timeUntilFire += Time.deltaTime;

            HandleBeamVisual();

            //If we have elapsed time, and are looking at the target, fire.
            Vector3 directionOfTarget = m_curTarget.transform.position - transform.position;

            m_curFireRate = m_towerData.m_fireRate * Mathf.Pow(m_speedPower, m_curStacks);
            if (m_curFireRate > m_speedCap)
            {
                m_curFireRate = m_speedCap;
            }

            if (m_timeUntilFire >= 1f / m_curFireRate && Vector3.Angle(m_turretPivot.transform.forward, directionOfTarget) <= m_facingThreshold)
            {
                if (m_curStacks < m_maxStacks) m_curStacks++;

                m_projectileLineRenderer.enabled = true;

                Fire();
                HandlePanelColor();

                m_timeUntilFire = 0;
            }
        }
    }

    private void HandleMaxStackVisuals()
    {
        if (m_curStacks == m_maxStacks)
        {
            m_maxStackVFX.SetActive(true);
        }
        else
        {
            m_maxStackVFX.SetActive(false);
        }
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

        //Deal Damage.
        m_curTarget.OnTakeDamage(m_curDamage);

        //Apply Shred
        if (m_statusEffectData)
        {
            StatusEffect statusEffect = new StatusEffect();
            statusEffect.SetTowerSender(this);
            statusEffect.m_data = m_statusEffectData;
            m_curTarget.ApplyEffect(statusEffect);
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
        //Setup LineRenderer Data.
        m_projectileLineRenderer.SetPosition(0, m_muzzlePoint.position);
        m_projectileLineRenderer.SetPosition(1, m_curTarget.m_targetPoint.position);

        //Scroll the texture.
        m_scrollOffset -= new Vector2(m_curFireRate * Time.deltaTime, 0);
        m_projectileLineRenderer.material.SetTextureOffset("_BaseMap", m_scrollOffset);

        //Color the texture.
        float normalizedTime = (float)m_curStacks / m_maxStacks;
        Color color = m_beamGradient.Evaluate(normalizedTime);
        m_projectileLineRenderer.material.SetColor("_BaseColor", color);
    }

    private bool IsTargetInRange(Vector3 targetPos)
    {
        float distance = Vector3.Distance(transform.position, targetPos);
        return distance <= m_towerData.m_fireRange;
    }

    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_targetRange, m_layerMask.value);
        float closestDistance = 999;
        int closestIndex = -1;
        if (hits.Length > 0)
        {
            //Debug.Log($"Hits: {hits.Length} and Layers: {m_layerMask.value}");
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
}