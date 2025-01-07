using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Tower : MonoBehaviour
{
    [Header("Tower Data")]
    [SerializeField] protected TowerData m_towerData;
    [SerializeField] protected GameObject m_modelRoot;

    [SerializeField] protected StatusEffectData m_statusEffectData;
    [SerializeField] protected LayerMask m_layerMask;
    [SerializeField] protected LayerMask m_raycastLayerMask;
    protected int m_shieldLayer;

    [Header("Attachment Points")]
    [SerializeField] protected Transform m_turretPivot;

    [SerializeField] protected Transform m_muzzlePoint;

    [Header("Range Circle")]
    [SerializeField] protected LineRenderer m_towerRangeCircle;

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
        SetupRangeCircle(48, m_towerData.m_fireRange);
        m_audioSource = GetComponent<AudioSource>();
        m_animator = GetComponent<Animator>();
        m_shieldLayer = LayerMask.NameToLayer("Shield"); //HARDCODED LAYER NAME
    }

    private void GameplayStatChanged(GameplayManager.GameplayState newState)
    {
    }

    public virtual void RequestTowerDisable()
    {
        RequestPlayAudio(m_towerData.m_audioTowerDeactivatedClips, m_audioSource);
        m_curTarget = null;
        enabled = false;
    }

    public virtual void RequestTowerEnable()
    {
        RequestPlayAudio(m_towerData.m_audioTowerActivatedClips, m_audioSource);
        enabled = true;
    }

    public abstract TowerTooltipData GetTooltipData();
    public abstract TowerUpgradeData GetUpgradeData();
    public abstract void SetUpgradeData(TowerUpgradeData data);

    public void FindTarget()
    {
        // Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_fireRange, m_layerMask.value);
        // Trying a Capsule for player-friendly flying unit target acquisition.
        Vector3 point1 = transform.position + Vector3.up * 10; // Top of the capsule
        Vector3 point2 = transform.position - Vector3.up * 1; // Bottom of the capsule
        Collider[] hits = Physics.OverlapCapsule(point1, point2, m_towerData.m_fireRange, m_layerMask.value);
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
            EnemyController target = hits[i].GetComponent<EnemyController>();
            if (target.GetCurrentHP() > 0)
            {
                targets.Add(target);
            }
        }

        if (targets.Count == 0)
        {
            m_curTarget = null;
            return;
        }
        
        if (targets.Count == 1)
        {
            m_curTarget = targets[0];
            return;
        }
        
        m_curTarget = GetPriorityTarget(targets);

        if (m_curTarget != null)
        {
            m_targetCollider = m_curTarget.GetComponent<Collider>(); // Used for Void towers to check hit unit or shield.
        }
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
        // Trying 2d range calculations to be more player friendly with the flying unit(s).
        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        Vector2 tarPos = new Vector2(targetPos.x, targetPos.z);
        return Vector3.Distance(pos, tarPos) <= m_towerData.m_fireRange;
    }

    protected bool IsTargetInTargetRange(Vector3 targetPos)
    {
        float distance = Vector3.Distance(transform.position, targetPos);
        bool isTargetInTargetRange = distance <= m_towerData.m_targetRange;
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

    public virtual void SetupTower()
    {
        //Grid
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GameplayManager.Instance.AddTowerToList(this);

        //Operational
        gameObject.GetComponent<Collider>().enabled = true;
        m_isBuilt = true;
        m_modelRoot.SetActive(true);

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

    public void RequestPlayAudio(AudioClip clip, AudioSource audioSource = null)
    {
        if (clip == null) return;
        
        if (audioSource == null) audioSource = m_audioSource;
        audioSource.PlayOneShot(clip);
    }

    public void RequestPlayAudio(List<AudioClip> clips, AudioSource audioSource = null)
    {
        if (clips[0] == null) return;
        
        if (audioSource == null) audioSource = m_audioSource;
        int i = Random.Range(0, clips.Count);
        audioSource.PlayOneShot(clips[i]);
    }

    public void RequestPlayAudioLoop(AudioClip clip, AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;
        audioSource.loop = true;
        audioSource.clip = clip;
        audioSource.Play();
    }
    
    public void RequestStopAudioLoop(AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;
        audioSource.Stop();
    }

    public virtual void OnDestroy()
    {
        //If this gameObject does not exist, exit this function. (Good for leaving play mode in editor and not getting asserts)
        if (!Application.isPlaying) return;

        /*if (m_audioSource.enabled)
        {
            RequestPlayAudio(m_towerData.m_audioDestroyClip);
        }*/

        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
        GameplayManager.OnGameObjectDeselected -= GameObjectDeselected;
    }

    public virtual void RemoveTower()
    {
        m_modelRoot.SetActive(false);
        ObjectPoolManager.OrphanObject(gameObject, .5f, ObjectPoolManager.PoolType.Tower);
    }

    void SetupRangeCircle(int segments, float radius)
    {
        m_towerRangeCircle.positionCount = segments;
        m_towerRangeCircle.startWidth = 0.06f;
        m_towerRangeCircle.endWidth = 0.06f;
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

    public TargetingSystem m_targetSystem = TargetingSystem.ClosestToGoal;

    public enum TargetingSystem
    {
        ClosestToGoal,
        ClosestToTower,
        HighestHealth,
        LowestHealth,
    }

    public EnemyController GetPriorityTarget(List<EnemyController> targets)
    {
        int highestPriority = int.MinValue;
        List<EnemyController> m_targetsOfThisPriorityValue = new List<EnemyController>();

        // Create a list of enemies that share the highest Priority that we can then operate upon based on the assigned Target System.
        foreach (EnemyController enemy in targets)
        {
            if (enemy.m_enemyData.m_targetPriority == highestPriority)
            {
                m_targetsOfThisPriorityValue.Add(enemy);
            }

            if (enemy.m_enemyData.m_targetPriority > highestPriority)
            {
                highestPriority = enemy.m_enemyData.m_targetPriority;
                m_targetsOfThisPriorityValue = new List<EnemyController> { enemy };
            }
        }

        EnemyController priorityTarget = null;
        int fewestCellsToGoal = int.MaxValue;
        float closestTargetDistance = float.PositiveInfinity;
        float lowestCurrentHP = float.PositiveInfinity;
        float highestCurrentHP = 0;

        foreach (EnemyController enemy in m_targetsOfThisPriorityValue)
        {
            switch (m_targetSystem)
            {
                case TargetingSystem.ClosestToGoal:
                    int cellCount = enemy.GetCellCountToGoal();
                    if (cellCount < fewestCellsToGoal)
                    {
                        priorityTarget = enemy;
                        fewestCellsToGoal = cellCount;
                    }

                    break;
                case TargetingSystem.ClosestToTower: // Find the target closest to the tower.
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < closestTargetDistance)
                    {
                        priorityTarget = enemy;
                        closestTargetDistance = distance;
                    }

                    break;
                case TargetingSystem.HighestHealth: // Find the target with the highest current health.
                    float highEnemyHealth = enemy.GetCurrentHP();
                    if (highEnemyHealth > highestCurrentHP)
                    {
                        priorityTarget = enemy;
                        highestCurrentHP = highEnemyHealth;
                    }

                    break;
                case TargetingSystem.LowestHealth: // Find the target with the lowest current health.
                    float lowEnemyHealth = enemy.GetCurrentHP();
                    if (lowEnemyHealth < lowestCurrentHP)
                    {
                        priorityTarget = enemy;
                        highestCurrentHP = lowEnemyHealth;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (m_curTarget == null) // Return early if the tower does not have a target.
        {
            return priorityTarget;
        }

        if (priorityTarget.m_enemyData.m_targetPriority > m_curTarget.m_enemyData.m_targetPriority)
        {
            return priorityTarget;
        }

        // Don't change targets if the priority target is within a threshold based on the Targeting System of the current target.
        switch (m_targetSystem)
        {
            case TargetingSystem.ClosestToGoal:
                int curCellCount = m_curTarget.GetCellCountToGoal();
                if (fewestCellsToGoal < curCellCount)
                {
                    return priorityTarget;
                }

                break;
            case TargetingSystem.ClosestToTower: // Only swap if the distance between current target and priority is past a threshold.
                float curTargetdistance = Vector3.Distance(transform.position, m_curTarget.transform.position);
                if (Mathf.Abs(curTargetdistance - closestTargetDistance) > .25f)
                {
                    return priorityTarget;
                }

                break;
            case TargetingSystem.HighestHealth: // Only swap if the MaxHP is greater than the current targets + % threshold.
                float curTargetMaxHP = priorityTarget.GetMaxHP();
                if (curTargetMaxHP * 0.95 > m_curTarget.GetMaxHP())
                {
                    return priorityTarget;
                }

                break;
            case TargetingSystem.LowestHealth: // Only swap if the target has less than current target - 1 tower shots worth of hp.
                float adjustedHP = lowestCurrentHP + m_towerData.m_baseDamage;
                if (adjustedHP < m_curTarget.GetCurrentHP())
                {
                    return priorityTarget;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return m_curTarget; // If we fail to meet the thresholds, return our current target.
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