using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeUpTowerController : Tower
{
    public LineRenderer m_projectileLineRenderer;
    public float m_stackDropDelayTime;
    public int m_maxStacks;
    public Gradient m_beamGradient;
    public Gradient m_panelGradient;
    public MeshRenderer m_panelMeshRenderer;


    private float m_curStackDropDelay;
    private float m_resetStep;
    private float m_resetTime;
    private float m_curStacks;
    private Vector2 m_scrollOffset;
    private float m_lastStacks;
    private float m_beamDuration = .25f;
    private float m_timeUntilBeamOff;
    private float m_facingThreshold = 10f;
    private Vector3 m_lastTargetPos;

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

        
        //If FIRE turns the renderer on, turn it off.
        if (m_projectileLineRenderer.enabled && m_curTarget != null)
        {
            m_timeUntilBeamOff += Time.deltaTime;
            
            HandleBeamVisual();
            
            if (m_timeUntilBeamOff >= m_beamDuration)
            {
                Fire();
                
                //Lower total stacks.
                m_curStacks /= 2;
                
                //Reset Stack drop delay.
                m_curStackDropDelay = 0;
                
                m_projectileLineRenderer.enabled = false;
                m_timeUntilBeamOff = 0f;
            }
            
            //Dont let the tower do anything until the beam is off.
            return;
        }

        RotateTowardsTarget();
        
        HandlePanelColor();
        
        //FINDING TARGET
        if (m_curTarget == null)
        {
            ChargeDown();
            FindTarget();
            return;
        }

        ChargeUp();

        if (IsTargetInRange())
        {
            //If we we are fully charged, and target is in cone of view, fire.
            Vector3 directionOfTarget = m_curTarget.transform.position - transform.position;
            if (m_curStacks >= m_maxStacks && Vector3.Angle(m_turretPivot.transform.forward, directionOfTarget) <= m_facingThreshold)
            {
                //Enable the visual effect.
                m_projectileLineRenderer.enabled = true;
            }
        }
        else
        {
            m_curTarget = null;
        }
    }

    private void ChargeDown()
    {
        //Dont charge down if we're at 0.
        if (m_curStacks <= 0) return;
        
        //Delay clock, then remove stacks when met.
        m_curStackDropDelay += Time.deltaTime;

        if (m_curStackDropDelay >= m_stackDropDelayTime && m_curStacks > 0)
        {
            m_curStacks -= (m_towerData.m_fireRate * m_maxStacks) * Time.deltaTime;
        }
        Debug.Log($"Charging Down: {m_curStacks} / {m_maxStacks}");
    }

    private void ChargeUp()
    {
        //Dont charge up if we're at max stacks.
        if (m_curStacks >= m_maxStacks) return;
        
        //Increase curStacks based on max stacks and fire rate.
        m_curStacks += (m_towerData.m_fireRate * m_maxStacks) * Time.deltaTime;
        Debug.Log($"Charging Up: {m_curStacks} / {m_maxStacks}");
    }

    private void HandlePanelColor()
    {
        if (m_curStacks == m_lastStacks) return;

        float normalizedTime = m_curStacks / m_maxStacks;
        Color color = m_panelGradient.Evaluate(normalizedTime);
        m_panelMeshRenderer.materials[0].SetColor("_BaseColor", color);
        m_panelMeshRenderer.materials[1].SetColor("_BaseColor", color);

        m_lastStacks = m_curStacks;
    }

    private void Fire()
    {
        //Deal Damage.
        m_curTarget.OnTakeDamage(m_towerData.m_baseDamage);

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
    }

    private void HandleBeamVisual()
    {
        //Setup LineRenderer Data.
        m_projectileLineRenderer.SetPosition(0, m_muzzlePoint.position);
        m_projectileLineRenderer.SetPosition(1, m_curTarget.transform.position);

        //Scroll the texture.
        m_scrollOffset -= new Vector2(10 * Time.deltaTime, 0);
        m_projectileLineRenderer.material.SetTextureOffset("_BaseMap", m_scrollOffset);

        //Color the texture.
        Color color = m_beamGradient.Evaluate(1);
        m_projectileLineRenderer.material.SetColor("_BaseColor", color);
    }

    private bool IsTargetInRange()
    {
        float distance = Vector3.Distance(transform.position, m_curTarget.transform.position);
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
}