using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoidRayTowerController : MonoBehaviour
{
    [SerializeField] private Transform m_turretPivot;
    [SerializeField] private Transform m_muzzlePoint;
    [SerializeField] private ScriptableTowerDataObject m_towerData;
    [SerializeField] private LayerMask m_layerMask;


    public bool m_isBuilt;
    private UnitEnemy m_curTarget;
    private float m_timeUntilFire;
    private float m_facingThreshold = 10f;
    private AudioSource m_audioSource;

    [Header("Edit")]
    public LineRenderer m_projectileLineRenderer;
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

    [Header("Dont Edit")]
    public int m_curStacks;
    public float m_curDamage;
    public float m_curFireRate;
    private Vector2 m_scrollOffset;

    //When the gun starts firing.
    //Increment stacks(x) each time it fires.
    //Attack Speed and Attack Damage is multiplied by X.
    //When the tower has not fired for m_stackDropDelay(2s) remove 1 stack every m_totalResetTime(2s) / m_curStacks(x)
    void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        //Dont do anything until we're built.
        if (!m_isBuilt)
        {
            return;
        }

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
            }
        }

        //FINDING TARGET
        if (m_curTarget == null)
        {
            FindTarget();
            return;
        }

        RotateTowardsTarget();

        if (!IsTargetInRange(m_curTarget.transform.position))
        {
            m_projectileLineRenderer.enabled = false;
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
                
                m_timeUntilFire = 0;
            }
        }
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
        return Vector3.Distance(transform.position, targetPos) < m_towerData.m_fireRange;
    }

    private void RotateTowardsTarget()
    {
        float angle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));

        m_turretPivot.rotation = Quaternion.RotateTowards(m_turretPivot.transform.rotation, targetRotation,
            m_towerData.m_rotationSpeed * Time.deltaTime);
    }

    private void FindTarget()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, m_towerData.m_targetRange, transform.forward, m_layerMask);
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

            m_curTarget = hits[closestIndex].transform.GetComponent<UnitEnemy>();
        }
    }
}