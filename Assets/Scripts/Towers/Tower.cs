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

    [Header("Renderers")]
    [SerializeField] protected List<Renderer> m_renderers;

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
    protected Vector3 m_overlapCapsulePoint1;
    protected Vector3 m_overlapCapsulePoint2;
    protected float m_targetAngle;
    protected quaternion m_targetRotation;
    protected Vector3 m_directionAwayFromSpire;
    protected List<Material> m_materials;

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        m_towerRangeCircle.enabled = false;
        SetupRangeCircle(48, m_towerData.m_fireRange);
        m_audioSource = GetComponent<AudioSource>();
        m_animator = GetComponent<Animator>();
        m_towerCollider = gameObject.GetComponent<Collider>();
        m_shieldLayer = LayerMask.NameToLayer("Shield"); //HARDCODED LAYER NAME

        m_materials = new List<Material>();
        foreach (Renderer renderer in m_renderers)
        {
            m_materials.Add(renderer.material);
        }
    }

    public virtual void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.Defeat)
        {
            m_audioSource.enabled = false;
            RequestTowerDisable();
        }
    }

    public virtual void RequestTowerDisable()
    {
        foreach (Material material in m_materials)
        {
            material.color = Color.gray;
        }

        RequestPlayAudio(m_towerData.m_audioTowerDeactivatedClips, m_audioSource);
        m_curTarget = null;
        enabled = false;
    }

    public virtual void RequestTowerEnable()
    {
        enabled = true;
        m_curTarget = null;
        RequestPlayAudio(m_towerData.m_audioTowerActivatedClips, m_audioSource);

        foreach (Material material in m_materials)
        {
            material.color = Color.white;
        }
    }

    public abstract TowerTooltipData GetTooltipData();
    public abstract TowerUpgradeData GetUpgradeData();
    public abstract void SetUpgradeData(TowerUpgradeData data);

    private int m_overlapCapsuleHitCount = 0;

    public void FindTarget()
    {
        // Collider[] hits = Physics.OverlapSphere(transform.position, m_towerData.m_fireRange, m_layerMask.value);
        // Trying a Capsule for player-friendly flying unit target acquisition.
        Collider[] hits = Physics.OverlapCapsule(m_overlapCapsulePoint1, m_overlapCapsulePoint2, m_towerData.m_fireRange, m_layerMask.value);
        m_overlapCapsuleHitCount = hits.Length;

        if (m_overlapCapsuleHitCount == 0)
        {
            m_curTarget = null;
            m_targetCollider = null;
            return;
        }

        if (m_overlapCapsuleHitCount == 1)
        {
            EnemyController target = hits[0].GetComponent<EnemyController>();
            if (target.GetCurrentHP() > 0)
            {
                m_curTarget = target;
                m_targetCollider = m_curTarget.GetComponent<Collider>();
                return;
            }
        }

        List<EnemyController> targets = ListPool<EnemyController>.Get();
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
            ListPool<EnemyController>.Release(targets);
            return;
        }

        if (targets.Count == 1)
        {
            m_curTarget = targets[0];
            m_targetCollider = m_curTarget.GetComponent<Collider>();
            ListPool<EnemyController>.Release(targets);
            return;
        }

        m_curTarget = GetPriorityTarget(targets);

        if (m_curTarget != null)
        {
            m_targetCollider = m_curTarget.GetComponent<Collider>(); // Used for Void towers to check hit unit or shield.
        }

        ListPool<EnemyController>.Release(targets);
    }

    public void RotateTowardsTarget()
    {
        if (m_curTarget)
        {
            m_targetAngle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;
            m_targetRotation = Quaternion.Euler(new Vector3(0f, m_targetAngle, 0f));
        }

        //If we have no target, rotate away from the base during the Build phase. The isBuilt flag will stop this from happening when precon.
        //Adding a instance check for the target dummy scene.
        if (GameplayManager.Instance && GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Build)
        {
            //Use enemy Goal as the 'target'.
            m_directionAwayFromSpire = GameplayManager.Instance.m_enemyGoal.position - transform.position;

            // Calculate the rotation angle to make the new object face away from the target.
            float angle = Mathf.Atan2(m_directionAwayFromSpire.x, m_directionAwayFromSpire.z) * Mathf.Rad2Deg + 180f;
            m_targetRotation = Quaternion.Euler(0, angle, 0);
        }

        m_turretPivot.rotation = Quaternion.RotateTowards(m_turretPivot.transform.rotation, m_targetRotation,
            m_towerData.m_rotationSpeed * Time.deltaTime);
    }

    private Vector3 m_targetPos;
    private Vector3 m_muzzlePos;
    private Vector3 m_directionOfTarget;

    protected bool IsTargetInSight()
    {
        m_targetPos = m_curTarget.transform.position;
        m_targetPos.y = 0;

        m_muzzlePos = m_muzzlePoint.transform.position;
        m_muzzlePos.y = 0;

        m_directionOfTarget = m_targetPos - m_muzzlePos;
        return Vector3.Angle(m_muzzlePoint.transform.forward, m_directionOfTarget) <= m_towerData.m_facingThreshold;
    }

    private Vector2 m_tower2dPos;
    private Vector2 m_target2dPos;

    protected bool IsTargetInFireRange(Vector3 targetPos)
    {
        // Trying 2d range calculations to be more player friendly with the flying unit(s).
        m_tower2dPos = new Vector2(transform.position.x, transform.position.z);
        m_target2dPos = new Vector2(targetPos.x, targetPos.z);
        return Vector2.Distance(m_tower2dPos, m_target2dPos) <= m_towerData.m_fireRange;
    }

    protected bool IsTargetInTargetRange(Vector3 targetPos)
    {
        float distance = Vector3.Distance(transform.position, targetPos);
        bool isTargetInTargetRange = distance <= m_towerData.m_targetRange;
        return isTargetInTargetRange;
    }

    private void ToggleTowerRangeCircle(bool value)
    {
        if (m_towerData.m_fireRange == -1) return;
        if (value == m_towerRangeCircle.enabled) return;
        m_towerRangeCircle.enabled = value;
    }

    public virtual void GameObjectSelected(GameObject obj)
    {
        if (obj == gameObject)
        {
            ToggleTowerRangeCircle(true);
            m_animator.SetTrigger("Selected");
        }
        else
        {
            ToggleTowerRangeCircle(false);
        }
    }

    public virtual void GameObjectDeselected(GameObject obj)
    {
        if (obj == gameObject)
        {
            ToggleTowerRangeCircle(false);
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
        Debug.Log($"Build Tower: Setting up {gameObject.name} at {transform.position}.");
        //Grid
        GridCellOccupantUtil.SetOccupant(gameObject, true, m_towerData.m_buildingSize.x, m_towerData.m_buildingSize.y);
        GameplayManager.Instance.AddTowerToList(this);

        //Operational
        m_towerCollider.enabled = true;
        m_isBuilt = true;
        m_modelRoot.SetActive(true);
        Debug.Log($"Build Tower: {gameObject.name}'s collider enabled: {m_towerCollider.enabled}, is built: {m_isBuilt}, model root is active: {m_modelRoot.activeSelf}.");
        m_overlapCapsulePoint1 = transform.position + Vector3.up * 10; // Top of the capsule
        m_overlapCapsulePoint2 = transform.position - Vector3.up * 1; // Bottom of the capsule

        //Animation
        //m_animator.SetTrigger("Construct");

        //Audio
        if (GameplayManager.Instance.m_gameplayState != GameplayManager.GameplayState.PlaceObstacles) m_audioSource.PlayOneShot(m_towerData.m_audioBuildClip);

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
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
    }

    protected Collider m_towerCollider;

    public virtual void RemoveTower()
    {
        Debug.Log($"Remove Tower: Setting up {gameObject.name} at {transform.position}.");
        m_isBuilt = false;
        m_towerCollider.enabled = false;
        m_modelRoot.SetActive(false);
        Debug.Log($"Remove Tower: {gameObject.name}'s collider enabled: {m_towerCollider.enabled}, is built: {m_isBuilt}, model root is active: {m_modelRoot.activeSelf}.");
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Tower);
        //ObjectPoolManager.OrphanObject(gameObject, .5f, ObjectPoolManager.PoolType.Tower);
    }

    void SetupRangeCircle(int segments, float radius)
    {
        if (radius == -1) return;

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

    public void SetRangeCircleColor(Color color)
    {
        m_towerRangeCircle.material.color = color;
    }

    public TargetingSystem m_targetSystem = TargetingSystem.ClosestToGoal;

    public enum TargetingSystem
    {
        ClosestToGoal,
        ClosestToTower,
        HighestHealth,
        LowestHealth,
    }

    private int m_highestPriority;
    private List<EnemyController> m_targetsOfThisPriorityValue;
    int m_fewestCellsToGoal = int.MaxValue;
    float m_closestTargetDistance = float.PositiveInfinity;
    float m_lowestCurrentHP = float.PositiveInfinity;
    float m_highestCurrentHP = 0;
    int m_enemyCellCountToGoal = 0;
    float m_enemyDitanceFromTower = 0;
    float m_enemyHighestHealth = 0;
    float m_enemyLowestHealth = 0;
    int m_curCellCount;
    float m_curTargetdistance;
    float m_curTargetMaxHP;
    float m_adjustedHP;

    public EnemyController GetPriorityTarget(List<EnemyController> targets)
    {
        m_highestPriority = int.MinValue;
        if (m_targetsOfThisPriorityValue == null)
        {
            m_targetsOfThisPriorityValue = new List<EnemyController>();
        }
        else
        {
            m_targetsOfThisPriorityValue.Clear();
        }

        // Create a list of enemies that share the highest Priority that we can then operate upon based on the assigned Target System.
        foreach (EnemyController enemy in targets)
        {
            if (enemy.m_enemyData.m_targetPriority == m_highestPriority)
            {
                m_targetsOfThisPriorityValue.Add(enemy);
            }

            if (enemy.m_enemyData.m_targetPriority > m_highestPriority)
            {
                m_highestPriority = enemy.m_enemyData.m_targetPriority;
                m_targetsOfThisPriorityValue = new List<EnemyController> { enemy };
            }
        }

        EnemyController priorityTarget = null;
        m_fewestCellsToGoal = int.MaxValue;
        m_closestTargetDistance = float.PositiveInfinity;
        m_lowestCurrentHP = float.PositiveInfinity;
        m_highestCurrentHP = 0;

        foreach (EnemyController enemy in m_targetsOfThisPriorityValue)
        {
            switch (m_targetSystem)
            {
                case TargetingSystem.ClosestToGoal:
                    m_enemyCellCountToGoal = enemy.GetCellCountToGoal();
                    if (m_enemyCellCountToGoal < m_fewestCellsToGoal)
                    {
                        priorityTarget = enemy;
                        m_fewestCellsToGoal = m_enemyCellCountToGoal;
                    }

                    break;
                case TargetingSystem.ClosestToTower: // Find the target closest to the tower.
                    m_enemyDitanceFromTower = Vector3.Distance(transform.position, enemy.transform.position);
                    if (m_enemyDitanceFromTower < m_closestTargetDistance)
                    {
                        priorityTarget = enemy;
                        m_closestTargetDistance = m_enemyDitanceFromTower;
                    }

                    break;
                case TargetingSystem.HighestHealth: // Find the target with the highest current health.
                    m_enemyHighestHealth = enemy.GetCurrentHP();
                    if (m_enemyHighestHealth > m_highestCurrentHP)
                    {
                        priorityTarget = enemy;
                        m_highestCurrentHP = m_enemyHighestHealth;
                    }

                    break;
                case TargetingSystem.LowestHealth: // Find the target with the lowest current health.
                    m_enemyLowestHealth = enemy.GetCurrentHP();
                    if (m_enemyLowestHealth < m_lowestCurrentHP)
                    {
                        priorityTarget = enemy;
                        m_highestCurrentHP = m_enemyLowestHealth;
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
                m_curCellCount = m_curTarget.GetCellCountToGoal();
                if (m_fewestCellsToGoal < m_curCellCount)
                {
                    return priorityTarget;
                }

                break;
            case TargetingSystem.ClosestToTower: // Only swap if the distance between current target and priority is past a threshold.
                m_curTargetdistance = Vector3.Distance(transform.position, m_curTarget.transform.position);
                if (Mathf.Abs(m_curTargetdistance - m_closestTargetDistance) > .25f)
                {
                    return priorityTarget;
                }

                break;
            case TargetingSystem.HighestHealth: // Only swap if the MaxHP is greater than the current targets + % threshold.
                m_curTargetMaxHP = priorityTarget.GetMaxHP();
                if (m_curTargetMaxHP * 0.95 > m_curTarget.GetMaxHP())
                {
                    return priorityTarget;
                }

                break;
            case TargetingSystem.LowestHealth: // Only swap if the target has less than current target - 1 tower shots worth of hp.
                m_adjustedHP = m_lowestCurrentHP + m_towerData.m_baseDamage;
                if (m_adjustedHP < m_curTarget.GetCurrentHP())
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
        Debug.Log($"Build Tower: Getting Turret Transform. Transform is null: {m_turretPivot == null}");
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