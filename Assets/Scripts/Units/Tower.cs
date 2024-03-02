using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Tower : MonoBehaviour
{
    [SerializeField] protected Transform m_turretPivot;
    [SerializeField] protected Transform m_muzzlePoint;
    [SerializeField] protected TowerData m_towerData;
    [SerializeField] protected StatusEffectData m_statusEffectData;
    [SerializeField] protected LayerMask m_layerMask;
    [SerializeField] protected LineRenderer m_towerRangeCircle;
    [SerializeField] protected int m_towerRangeCircleSegments;
    [SerializeField] protected bool m_isBuilt;

    protected Animator m_animator;
    protected AudioSource m_audioSource;
    protected EnemyController m_curTarget;

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStatChanged;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
        GameplayManager.OnGameObjectDeselected += GameObjectDeselected;
        m_towerRangeCircle.enabled = false;
        SetupRangeCircle(m_towerRangeCircleSegments, m_towerData.m_fireRange);
        m_audioSource = GetComponent<AudioSource>();
        m_animator = GetComponent<Animator>();

        //If we have a status effect on this tower, create an instance of it to use when applying.
    }

    private void GameplayStatChanged(GameplayManager.GameplayState newState)
    {
    }

    public abstract TowerTooltipData GetTooltipData();

    public void RotateTowardsTarget()
    {
        Quaternion targetRotation = new Quaternion();

        if (m_curTarget)
        {
            float angle = Mathf.Atan2(m_curTarget.transform.position.x - transform.position.x, m_curTarget.transform.position.z - transform.position.z) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));
        }

        //If we have no target, rotate away from the base during the Build phase. The isBuilt flag will stop this from happening when precon.
        if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Build)
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