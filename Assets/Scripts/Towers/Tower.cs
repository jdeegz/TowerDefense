using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public abstract class Tower : MonoBehaviour
{
    [Header("Tower Data")]
    [SerializeField] protected TowerData m_towerData;
    [SerializeField] protected StatusEffectData m_statusEffectData;
    [SerializeField] protected LayerMask m_layerMask;
    [SerializeField] protected LayerMask m_raycastLayerMask;
    protected int m_shieldLayer;
    
    [Header("Attachment Points")]
    [SerializeField] protected Transform m_turretPivot;
    [SerializeField] protected Transform m_muzzlePoint;
    
    [Header("Range Circle")]
    [SerializeField] protected LineRenderer m_towerRangeCircle;
    [SerializeField] protected int m_towerRangeCircleSegments;
    
    [Space(15)]
    [SerializeField] protected bool m_isBuilt;
    protected bool m_hasTargets;
    protected Animator m_animator;
    protected AudioSource m_audioSource;
    protected EnemyController m_curTarget;
    protected float m_targetDetectionInterval = 0.33f;
    protected float m_targetDetectionTimer = 0f;
    protected Collider m_targetCollider;

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStatChanged;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        m_towerRangeCircle.enabled = false;
        SetupRangeCircle(m_towerRangeCircleSegments, m_towerData.m_fireRange);
        m_audioSource = GetComponent<AudioSource>();
        m_animator = GetComponent<Animator>();
        m_shieldLayer = LayerMask.NameToLayer("Shield"); //HARDCODED LAYER NAME

        //If we have a status effect on this tower, create an instance of it to use when applying.
    }

    private void GameplayStatChanged(GameplayManager.GameplayState newState)
    {
    }

    public abstract TowerTooltipData GetTooltipData();
    public abstract TowerUpgradeData GetUpgradeData();
    public abstract void SetUpgradeData(TowerUpgradeData data);

    public void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_fireRange, m_layerMask.value);
        if (hits.Length <= 0)
        {
            m_curTarget = null;
            m_targetCollider = null;
            return;
        }

        //Maybe escape early if hits.Length is only 1?
        
        List<EnemyController> targets = new List<EnemyController>();
        for (int i = 0; i < hits.Length; ++i)
        {
            targets.Add(hits[i].GetComponent<EnemyController>());
        }

        m_curTarget = GetPriorityTarget(targets);
        m_targetCollider = m_curTarget.GetComponent<Collider>();
    }
    
    public void RotateTowardsTarget()
    {
        Quaternion targetRotation = new Quaternion();

        if (m_curTarget)
        {
            float angle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));
        }

        //If we have no target, rotate away from the base during the Build phase. The isBuilt flag will stop this from happening when precon.
        //Adding a instance check for the target dummy scene.
        if (GameplayManager.Instance && GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Build)
        {
            //Use enemy Goal as the 'target'.
            Vector3 direction = GameplayManager.Instance.m_enemyGoal.position - transform.position;

            // Calculate the rotation angle to make the new object face away from the target.
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;
            targetRotation = Quaternion.Euler(0, angle, 0);
        }

        m_turretPivot.rotation = Quaternion.RotateTowards(m_turretPivot.transform.rotation, targetRotation,
            m_towerData.m_rotationSpeed * Time.deltaTime);
    }

    protected bool IsTargetInSight()
    {
        Vector3 targetPos = m_curTarget.transform.position;
        targetPos.y = 0;

        Vector3 muzzlePos = m_muzzlePoint.transform.position;
        muzzlePos.y = 0;

        Vector3 directionOfTarget = targetPos - muzzlePos;
        return Vector3.Angle(m_muzzlePoint.transform.forward, directionOfTarget) <= m_towerData.m_facingThreshold;
    }
    
    protected bool IsTargetInFireRange(Vector3 targetPos)
    {
        return Vector3.Distance(transform.position, targetPos) <= m_towerData.m_fireRange;
    }
    
    protected bool IsTargetInTargetRange(Vector3 targetPos)
    {
        float distance = Vector3.Distance(transform.position, targetPos);
        bool isTargetInTargetRange =  distance <= m_towerData.m_targetRange;
        return isTargetInTargetRange;
    }

    private void GameObjectSelected(GameObject obj)
    {
        if (obj == gameObject)
        {
            m_towerRangeCircle.enabled = true;
            m_animator.SetTrigger("Selected");
        }
        else
        {
            m_towerRangeCircle.enabled = false;
        }
    }

    private void GameObjectDeselected(GameObject obj)
    {
        if (obj == gameObject)
        {
            m_towerRangeCircle.enabled = false;
        }
    }

    public (int, int) GetTowercost()
    {
        return (m_towerData.m_stoneCost, m_towerData.m_woodCost);
    }

    public (int, int) GetTowerSellCost()
    {
        return (m_towerData.m_stoneSellCost, m_towerData.m_woodSellCost);
    }

    public TowerData GetTowerData()
    {
        return m_towerData;
    }

    public Vector3 GetTowerMuzzlePoint()
    {
        return m_muzzlePoint.position;
    }

    public void SetupTower()
    {
        //Grid
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GameplayManager.Instance.AddTowerToList(this);

        //Operational
        gameObject.GetComponent<Collider>().enabled = true;        
        m_isBuilt = true;

        //Animation
        m_animator.SetTrigger("Construct");
        
        //Audio
        m_audioSource.PlayOneShot(m_towerData.m_audioBuildClip);

        //VFX
        ObjectPoolManager.SpawnObject(m_towerData.m_towerConstructionPrefab, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
    }

    //Fired via a keyframe in Animation.
    public void FireVFX()
    {
        if (!m_towerData.m_muzzleFlashPrefab) return;

        ObjectPoolManager.SpawnObject(m_towerData.m_muzzleFlashPrefab, m_muzzlePoint.position, m_muzzlePoint.rotation, null, ObjectPoolManager.PoolType.ParticleSystem);
    }
    
    public void OnDestroy()
    {
        //If this gameObject does not exist, exit this function. (Good for leaving play mode in editor and not getting asserts)
        if (!Application.isPlaying) return;

        if (m_audioSource.enabled)
        {
            m_audioSource.PlayOneShot(m_towerData.m_audioDestroyClip);
        }

        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
    }

    public virtual void RemoveTower()
    {
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Tower);
    }

    void SetupRangeCircle(int segments, float radius)
    {
        m_towerRangeCircle.positionCount = segments;
        m_towerRangeCircle.startWidth = 0.15f;
        m_towerRangeCircle.endWidth = 0.15f;
        for (int i = 0; i < segments; ++i)
        {
            float circumferenceProgress = (float)i / segments;
            float currentRadian = circumferenceProgress * 2 * Mathf.PI;
            float xScaled = Mathf.Cos(currentRadian);
            float yScaled = Mathf.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            Vector3 currentPosition = new Vector3(x, 0.25f, y);

            m_towerRangeCircle.SetPosition(i, currentPosition);
        }
    }

    public EnemyController GetPriorityTarget(List<EnemyController> targets)
    {
        //Dont stop firing at targets in range with lower priority
        //Fire Range is shorter than Target Range.
        
        float closestTargetDistance = float.PositiveInfinity;
        int highestPriority = int.MinValue;
        EnemyController priorityTarget = null;

        foreach (EnemyController enemy in targets)
        {
            if (enemy.m_enemyData.m_targetPriority == highestPriority)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestTargetDistance)
                {
                    priorityTarget = enemy;
                    closestTargetDistance = distance;
                    continue;
                }
            }
            
            if (enemy.m_enemyData.m_targetPriority > highestPriority)
            {
                highestPriority = enemy.m_enemyData.m_targetPriority;
                priorityTarget = enemy;
                closestTargetDistance = Vector3.Distance(transform.position, enemy.transform.position);
            }
        }

        // Don't change targets if the discovered priority target is of the same value as current target.
        if (m_curTarget != null && priorityTarget.m_enemyData.m_targetPriority == m_curTarget.m_enemyData.m_targetPriority)
        {
            priorityTarget = m_curTarget;
        }

        return priorityTarget;
    }

    public Quaternion GetTurretRotation()
    {
        return m_turretPivot.rotation;
    }

    public void SetTurretRotation(Quaternion rot)
    {
        m_turretPivot.rotation = rot;
    }

    public Transform GetTurretTransform()
    {
        return m_turretPivot;
    }

    public void GetPointOnColliderSurface(Vector3 start, Vector3 target, Collider collider, out Vector3 point, out Quaternion rotation, out Collider colliderHit)
    {
        // Direction vector from start to target
        Vector3 direction = (target - start).normalized;

        // Setup a Ray
        Ray ray = new Ray(start, direction);
        
        // Calculate the maximum possible distance the ray could travel
        float rayLength = Vector3.Distance(start, target);
        
        RaycastHit[] raycastHits = Physics.RaycastAll(ray, rayLength, m_raycastLayerMask);

        if (raycastHits.Length == 0)
        {
            //Debug.Log($"Something broke with the void tower Get Point on Collider Surface function.");
        }
        
        raycastHits = raycastHits.OrderBy(raycastHits => raycastHits.distance).ToArray();
        //Check each hit's layer, if we hit a shield before we hit our target (ideally the last item in our list) escape.
        for (int i = 0; i < raycastHits.Length; i++)
        {
            //We hit the shield OR the target collider
            if (raycastHits[i].collider.gameObject.layer == m_shieldLayer || raycastHits[i].collider == collider)
            {
                point = raycastHits[i].point;
                rotation = Quaternion.LookRotation(raycastHits[i].normal);
                colliderHit = raycastHits[i].collider;
                return;
            }
        }
        
        // If no intersection, return the target point and identity rotation as fallback
        point = target;
        rotation = Quaternion.identity;
        colliderHit = collider;
    }
}

public class TowerTooltipData
{
    public string m_towerName;
    public string m_towerDescription;
    public string m_towerDetails;

    public string m_timeIconString = "<sprite name=\"Time\">";
    public string m_damageIconString = "<sprite name=\"Damage\">";
    
    public string BuildStatusEffectString(StatusEffectData m_statusEffectData)
    {
        string statusEffect = null;
        float statusEffectDamageRate = (m_statusEffectData.m_damage / m_statusEffectData.m_tickSpeed) * m_statusEffectData.m_lifeTime;
        string statusEffectDamageString = statusEffectDamageRate.ToString("F1");
        string moveModifierPercentage = Util.FormatAsPercentageString(1 - m_statusEffectData.m_speedModifier);
        string armorModifierPercentage = Util.FormatAsPercentageString(1 - m_statusEffectData.m_damageModifier);
        switch (m_statusEffectData.m_effectType)
        {
            case StatusEffectData.EffectType.DecreaseMoveSpeed:
                statusEffect = $"<br>Slows by: {moveModifierPercentage} for {m_statusEffectData.m_lifeTime}{m_timeIconString}";
                break;
            case StatusEffectData.EffectType.IncreaseMoveSpeed:
                statusEffect = $"<br>Faster by: {moveModifierPercentage} for {m_statusEffectData.m_lifeTime}{m_timeIconString}";
                break;
            case StatusEffectData.EffectType.DecreaseHealth:
                statusEffect = $"<br>Burns for: {statusEffectDamageString}{m_damageIconString} over {m_statusEffectData.m_lifeTime}{m_timeIconString}";
                break;
            case StatusEffectData.EffectType.IncreaseHealth:
                statusEffect = $"<br>Heals for: {statusEffectDamageString}{m_damageIconString} over {m_statusEffectData.m_lifeTime}{m_timeIconString}";
                break;
            case StatusEffectData.EffectType.DecreaseArmor:
                statusEffect = $"<br>Lowers Armor by: {armorModifierPercentage} for {m_statusEffectData.m_lifeTime}{m_timeIconString}";
                break;
            case StatusEffectData.EffectType.IncreaseArmor:
                statusEffect = $"<br>Boosts Armor by: {armorModifierPercentage} for {m_statusEffectData.m_lifeTime}{m_timeIconString}";
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return statusEffect;
    }

    
}

public class TowerUpgradeData
{
    public Quaternion m_turretRotation;
    public int m_stacks;
        
    //Add more data her as needed.
}