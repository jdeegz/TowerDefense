using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public abstract class EnemyController : Dissolvable, IEffectable
{
    //Enemy Scriptable Data
    [Header("Enemy Controller")]
    public EnemyData m_enemyData;

    public Transform m_targetPoint;
    public GameObject m_enemyModelRoot;

    //Enemy Objective & Position
    protected Vector2Int m_curPos;
    protected Cell m_curCell;
    protected Transform m_goal;
    protected Cell m_goalCell;
    protected Vector3 m_nextCellPosition;
    protected float m_maxX;
    protected float m_minX;
    protected float m_maxZ;
    protected float m_minZ;
    protected int m_cellsTravelled = -1;
    protected int m_cellsToGoal;

    //Enemy Stats
    protected float m_curMaxHealth;
    protected float m_curHealth;
    protected float m_baseMoveSpeed;
    protected float m_baseLookSpeed; // to be removed.
    protected float m_lastSpeedModifierFaster = 1f;
    protected float m_lastSpeedModifierSlower = 1f;
    protected float m_lastDamageModifierLower = 1f;
    protected float m_lastDamageModifierHigher = 1f;
    protected float m_baseDamageMultiplier;

    //Hit Flash Info
    protected List<Renderer> m_allRenderers;
    protected List<Color> m_allOrigColors;
    protected Coroutine m_hitFlashCoroutine;

    //Animation
    public Animator m_animator;

    //VFX
    private GameObject m_decreaseHealthVFXOjb;
    private GameObject m_increaseHealthVFXOjb;
    private GameObject m_decreaseMoveSpeedVFXOjb;
    private GameObject m_increaseMoveSpeedVFXOjb;

    protected AudioSource m_audioSource;
    protected List<StatusEffect> m_statusEffects;
    protected List<StatusEffect> m_newStatusEffects = new List<StatusEffect>();
    protected List<StatusEffect> m_expiredStatusEffects = new List<StatusEffect>();

    //Obelisk
    private ObeliskData m_obeliskData;

    //Actions
    public event Action<float> UpdateHealth;
    public event Action<Vector3> DestroyEnemy;

    //Enemy State
    protected bool m_isComplete;
    protected bool m_isActive = true;
    protected Vector3 m_moveDirection;


    public void SetEnemyData(EnemyData data, bool active = true)
    {
        m_enemyData = data;
        SetupEnemy(active);
    }

    public virtual void SetupEnemy(bool active)
    {
        m_isComplete = false;
        
        m_cellsTravelled = -1;

        if (GameplayManager.Instance)
        {
            m_goal = GameplayManager.Instance.m_enemyGoal;
            m_goalCell = Util.GetCellFrom3DPos(m_goal.position);
            AddToGameplayList();
        }

        //Setup with GridManager
        //Debug.Log($"Setting up {gameObject.name} at {transform.position}.");
        m_curPos = Vector2Int.zero;

        //Get new cell from new position.
        //m_curCell = Util.GetCellFromPos(m_curPos);

        //Assign self to cell.
        //m_curCell.UpdateActorCount(1, gameObject.name);

        //Setup Data
        m_baseMoveSpeed = m_enemyData.m_moveSpeed;
        m_baseLookSpeed = m_enemyData.m_lookSpeed;
        m_curMaxHealth = GameplayManager.Instance.m_gameplayData.CalculateHealth(m_enemyData.m_health);
        m_curHealth = m_curMaxHealth;
        m_baseDamageMultiplier = m_enemyData.m_damageMultiplier;

        //Setup Hit Flash
        CollectMeshRenderers(m_enemyModelRoot.transform);

        //Setup Status Effects
        //Debug.Log($"Clearing status effect lists.");
        if (m_statusEffects != null) m_statusEffects.Clear();
        if (m_expiredStatusEffects != null) m_expiredStatusEffects.Clear();
        if (m_newStatusEffects != null) m_newStatusEffects.Clear();
        m_statusEffects = new List<StatusEffect>();

        //Define AudioSource
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.PlayOneShot(m_enemyData.m_audioSpawnClip);

        //Setup ObeliskData if the mission has obelisks
        if (GameplayManager.Instance && GameplayManager.Instance.m_obelisksInMission.Count > 0)
        {
            m_obeliskData = GameplayManager.Instance.m_obelisksInMission[0].m_obeliskData;
        }

        SetupUI();

        SetEnemyActive(active);
    }

    public virtual void AddToGameplayList()
    {
        GameplayManager.Instance.AddEnemyToList(this);
    }

    public virtual void RemoveFromGameplayList()
    {
        GameplayManager.Instance.RemoveEnemyFromList(this);
    }

    public virtual void SetupUI()
    {
        //Setup Life Meter
        UIHealthMeter lifeMeter = ObjectPoolManager.SpawnObject(IngameUIController.Instance.m_healthMeter.gameObject, IngameUIController.Instance.transform).GetComponent<UIHealthMeter>();
        lifeMeter.SetEnemy(this, m_curMaxHealth, m_enemyData.m_healthMeterOffset, m_enemyData.m_healthMeterScale);
    }

    void Update()
    {
        if (!m_isActive) return;

        if (m_curHealth > 0) UpdateStatusEffects();

        //Target Dummy
        if (!m_goal) return;

        //If this is the exit cell, we've made it! Deal some damage to the player.
        CheckAtGoal();
    }

    public virtual void CheckAtGoal()
    {
        if (Vector3.Distance(transform.position, m_goal.position) <= 1.5f)
        {
            ReachedCastle();
        }
    }

    public void ReachedCastle()
    {
        DamageCastle();
        OnEnemyDestroyed(transform.position);
        DestroyEnemy?.Invoke(transform.position);
    }

    public virtual void DamageCastle()
    {
        GameplayManager.Instance.m_castleController.TakeDamage(1);
    }

    void FixedUpdate()
    {
        if (!m_isActive) return;

        HandleMovement();
    }

    //Movement
    //Functions
    public virtual void HandleMovement()
    {
        //Update Cell occupancy
        Vector2Int newPos = Util.GetVector2IntFrom3DPos(transform.position);

        if (newPos != m_curPos)
        {
            //Cell prevCell = m_curCell; //Stash the previous cell incase we need to go back.
            Cell newCell = Util.GetCellFromPos(newPos);

            //Check new cells occupancy.
            if (newCell.m_isOccupied)
            {
                //If it is occupied, we do NOT want to continue entering it. Ask our previous cell for it's new direction (assuming we've placed a tower and updated the grid)
            }
            else
            {
                //Remove self from current cell.
                if (m_curCell != null)
                {
                    m_curCell.UpdateActorCount(-1, gameObject.name);
                }

                //Assign new position, we are now in a new cell.
                m_curPos = newPos;

                //Get new cell from new position.
                m_curCell = newCell;

                //Assign self to cell.
                m_curCell.UpdateActorCount(1, gameObject.name);

                ++m_cellsTravelled;
            }

            m_cellsToGoal = AStar.CalculateGridDistance(m_curCell.m_cellPos, m_goalCell.m_cellPos);
            
            float wiggleMagnitude = m_enemyData.m_movementWiggleValue * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
            Vector2 nextCellPosOffset = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f) * wiggleMagnitude);

            //Convert saved cell pos from Vector2 to Vector3
            Vector3 m_curCell3dPos = new Vector3(m_curCell.m_cellPos.x, 0, m_curCell.m_cellPos.y);

            //Get the position of the next cell.
            //If the current cell is occupied, we go in the reverse direction until we're in an unoccupied cell.
            //If the current cell has no direction, we go back to the previous cell.

            m_nextCellPosition = m_curCell3dPos + new Vector3(m_curCell.m_directionToNextCell.x + nextCellPosOffset.x, 0, m_curCell.m_directionToNextCell.z + nextCellPosOffset.y);

            //Clamp saftey net. This was .45, but changed to .49 when I saw units hitch forward after new cell subscriptions combined with low velocity.
            m_maxX = m_curCell.m_cellPos.x + .49f;
            m_minX = m_curCell.m_cellPos.x - .49f;

            m_maxZ = m_curCell.m_cellPos.y + .49f;
            m_minZ = m_curCell.m_cellPos.y - .49f;

            if (m_curCell.m_directionToNextCell.x < 0)
            {
                //We're going left.
                m_minX += -1;
            }
            else if (m_curCell.m_directionToNextCell.x > 0)
            {
                //we're going right.
                m_maxX += 1;
            }

            if (m_curCell.m_directionToNextCell.z < 0)
            {
                //We're going down.
                m_minZ += -1;
            }
            else if (m_curCell.m_directionToNextCell.z > 0)
            {
                //we're going up.
                m_maxZ += 1;
            }
        }

        m_moveDirection = (m_nextCellPosition - transform.position).normalized;

        //Send information to Animator
        float angle = Vector3.SignedAngle(transform.forward, m_moveDirection, Vector3.up);
        m_animator.SetFloat("LookRotation", angle);

        //Move forward.
        float speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        float cumulativeMoveSpeed = speed * Time.deltaTime;
        transform.position += transform.forward * cumulativeMoveSpeed;
        m_animator.SetFloat("Speed", speed);

        //Look towards the move direction.
        float baseLookSpeed = Mathf.Max(m_baseLookSpeed, Math.Abs(angle));
        float cumulativeLookSpeed = baseLookSpeed * speed * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(m_moveDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, cumulativeLookSpeed);

        /*if (angle >= 90 || angle <= -90)
        {
            Debug.Log($"Angle: {angle} at Speed: {speed}. Look Speed: {baseLookSpeed * speed}");
        }*/

        //Apply clamping
        float posX = Mathf.Clamp(transform.position.x, m_minX, m_maxX);
        float posZ = Mathf.Clamp(transform.position.z, m_minZ, m_maxZ);
        transform.position = new Vector3(posX, transform.position.y, posZ);
    }

    //Taking Damage
    //Functions
    public void CollectMeshRenderers(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        Renderer Renderer = parent.GetComponent<Renderer>();
        if (Renderer != null && !(Renderer is TrailRenderer) && !(Renderer is VFXRenderer))
        {
            if (m_allRenderers == null)
            {
                m_allRenderers = new List<Renderer>();
            }

            if (m_allOrigColors == null)
            {
                m_allOrigColors = new List<Color>();
            }


            m_allRenderers.Add(Renderer);
            m_allOrigColors.Add(Renderer.material.GetColor("_EmissionColor"));
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            //Recursivly add meshrenderers by calling this function again with the child as the transform.
            Transform child = parent.GetChild(i);
            CollectMeshRenderers(child);
        }
    }

    public virtual void OnTakeDamage(float dmg)
    {
        if (m_curHealth <= 0) return;

        //Audio
        int i = Random.Range(0, m_enemyData.m_audioDamagedClips.Count);
        m_audioSource.PlayOneShot(m_enemyData.m_audioDamagedClips[i]);

        //Calculate Damage
        float cumDamage = dmg * m_baseDamageMultiplier * m_lastDamageModifierHigher * m_lastDamageModifierLower;
        m_curHealth -= cumDamage;
        UpdateHealth?.Invoke(-cumDamage);
        
        //If we're dead, destroy.
        if (m_curHealth <= 0)
        {
            OnEnemyDestroyed(transform.position);
            return;
        }
        
        //VFX

        //Hit Flash
        if (m_allRenderers == null) return;
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        if(gameObject.activeSelf) m_hitFlashCoroutine = StartCoroutine(HitFlash());
    }

    public IEnumerator HitFlash()
    {
        //Set the color
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            foreach (Material material in m_allRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", Color.red);
            }
        }

        yield return new WaitForSeconds(.15f);

        //Return to original colors.
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            foreach (Material material in m_allRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", m_allOrigColors[i]);
            }
        }
    }

    public virtual void OnHealed(float heal, bool percentage)
    {
        if (m_curHealth >= m_curMaxHealth) return;

        //Audio
        m_audioSource.PlayOneShot(m_enemyData.m_audioHealedClip);

        //VFX

        //Hit Flash
        if (m_allRenderers == null) return;
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());

        //Calculate Damage
        if (percentage) heal = m_curMaxHealth * heal; //If the heal is sent as percent, calculate based on max HP
        float curMissingHealth = m_curMaxHealth - m_curHealth; //Don't allow the unit to go above max HP
        heal = Mathf.Min(heal, curMissingHealth);
        m_curHealth += heal;
        UpdateHealth?.Invoke(heal);
        //Debug.Log($"{gameObject.name} healed for {heal}.");
    }

    public virtual void OnEnemyDestroyed(Vector3 pos)
    {
        if (m_isComplete) return;

        m_isComplete = true;
        m_isActive = false;

        //Kind of hacky, but this prevents towers from continuing to hit units that reach the castle.
        m_curHealth = 0;

        if (m_curCell != null)
        {
            m_curCell.UpdateActorCount(-1, gameObject.name);
            m_curCell = null;
            m_nextCellPosition = Vector3.zero;
        }

        m_animator.SetTrigger("IsDead");

        DestroyEnemy?.Invoke(transform.position);

        //If dead, look for obelisks nearby. If there is one, spawn a soul and have it move to the obelisk.
        if (m_curHealth <= 0 && m_obeliskData != null)
        {
            Obelisk m_closestObelisk = FindObelisk();
            if (m_closestObelisk != null)
            {
                //Instantiate a soul, and set its properties.
                m_obeliskData = m_closestObelisk.m_obeliskData;
                //GameObject obeliskSoulObject = Instantiate(m_obeliskData.m_obeliskSoulObj, m_swarmMemberTarget.position, quaternion.identity);
                GameObject obeliskSoulObject = ObjectPoolManager.SpawnObject(m_obeliskData.m_obeliskSoulObj, m_targetPoint.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
                ObeliskSoul obeliskSoul = obeliskSoulObject.GetComponent<ObeliskSoul>();
                obeliskSoul.SetupSoul(m_closestObelisk.m_targetPoint.transform.position, m_closestObelisk, m_obeliskData.m_soulValue);
            }
            else if (m_deathVFX)
            {
                ObjectPoolManager.SpawnObject(m_deathVFX.gameObject, m_targetPoint.position, Quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
            }
        }

        RemoveFromGameplayList();

        //Return effects to pool.
        foreach (StatusEffect activeEffect in m_statusEffects)
        {
            RemoveEffect(activeEffect);
        }

        //End the running coroutine
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        //Reset the coroutine tinting
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            foreach (Material material in m_allRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", m_allOrigColors[i]);
            }
        }

        StartDissolve(RemoveObject);
    }

    public virtual void RemoveObject()
    {
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
    }

    public virtual float GetCurrentHP()
    {
        return m_curHealth;
    }

    public virtual float GetMaxHP()
    {
        return m_curMaxHealth;
    }
    
    public virtual int GetCellCountToGoal()
    {
        return m_cellsToGoal;
    }

    public (float, float) GetMoveSpeedModifiers()
    {
        return (m_lastSpeedModifierFaster, m_lastSpeedModifierSlower);
    }

    private Obelisk FindObelisk()
    {
        float closestDistance = Mathf.Infinity;
        Obelisk closestObelisk = null;
        foreach (Obelisk obelisk in GameplayManager.Instance.m_obelisksInMission)
        {
            float distance = Vector3.Distance(transform.position, obelisk.transform.position);
            if (distance > obelisk.m_obeliskData.m_obeliskRange)
            {
                continue; // Enemy is too far from this obelisk.
            }
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObelisk = obelisk;
            }
        }

        return closestObelisk;
    }

    public void HandleTrojanSpawn(Vector3 startPos, Vector3 endPos)
    {
        transform.position = startPos;
        float moveDuration = Vector3.Distance(transform.position, endPos) / 0.6f;
        gameObject.transform.DOJump(endPos, 2, 1, moveDuration).OnComplete(() => SetEnemyActive(true));
    }

    public void SetEnemyActive(bool active) //Used to halt this unit from functioning. (Trojan created enemies)
    {
        m_isActive = active;
    }

    //Status Effect
    //Functions
    public void UpdateStatusEffects()
    {
        //Remove Expired Effects
        if (m_expiredStatusEffects.Count > 0)
        {
            foreach (StatusEffect expiredStatusEffect in m_expiredStatusEffects)
            {
                m_statusEffects.Remove(expiredStatusEffect);
            }

            m_expiredStatusEffects.Clear();
        }

        //Update each Effect.
        foreach (StatusEffect activeEffect in m_statusEffects)
        {
            HandleEffect(activeEffect);
        }

        //Add New Effects if the sender is unique (Does not already have this effect)
        if (m_newStatusEffects.Count > 0)
        {
            //Check & Add new Effects to list if there are any.
            foreach (StatusEffect newStatusEffect in m_newStatusEffects)
            {
                bool senderFound = false;
                for (int i = 0; i < m_statusEffects.Count; i++)
                {
                    if (newStatusEffect.m_sender == m_statusEffects[i].m_sender)
                    {
                        //We found a sender match, update the existing effect.
                        //Debug.Log($"Sender found, not a new effect.");

                        m_statusEffects[i] = newStatusEffect;
                        senderFound = true;
                        break;
                    }
                }

                //If we didnt find a matching sender, this is a new effect. Add it to the list.
                //Debug.Log($"Added new effect: {newStatusEffect.m_data.name}");
                if (!senderFound) m_statusEffects.Add(newStatusEffect);
            }

            //Reset the new status effects so it is empty.
            m_newStatusEffects.Clear();
        }
    }

    public virtual void ApplyEffect(StatusEffect statusEffect)
    {
        //Add incoming status effects to a holding list. They will get added to the list then updated in UpdateStatusEffects.
        if (m_curHealth <= 0) return;
        m_newStatusEffects.Add(statusEffect);
    }

    public void HandleEffect(StatusEffect statusEffect)
    {
        statusEffect.m_elapsedTime += Time.deltaTime;
        
        if (statusEffect.m_elapsedTime > statusEffect.m_data.m_lifeTime)
        {
            //Debug.Log($"Removing effect from {statusEffect.m_sender}. Elapsed time {statusEffect.m_elapsedTime} is greater than Life Time {statusEffect.m_data.m_lifeTime}.");
            RemoveEffect(statusEffect);
            return;
        }

        if (m_expiredStatusEffects.Contains(statusEffect)) return;

        //If we need to, spawn a vfx for this effect.
        StatusEffectSource statusEffectSource;
        switch (statusEffect.m_data.m_effectType)
        {
            case StatusEffectData.EffectType.DecreaseMoveSpeed:
                if (!m_decreaseMoveSpeedVFXOjb && statusEffect.m_data.m_effectVFX)
                {
                    m_decreaseMoveSpeedVFXOjb = ObjectPoolManager.SpawnObject(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                    
                    statusEffectSource = m_decreaseMoveSpeedVFXOjb.GetComponent<StatusEffectSource>();
                    if (statusEffectSource)
                    {
                        statusEffectSource.SetStatusEffectSource(statusEffect.m_sender);
                    }
                }

                break;
            case StatusEffectData.EffectType.IncreaseMoveSpeed:
                if (!m_increaseMoveSpeedVFXOjb && statusEffect.m_data.m_effectVFX)
                {
                    m_increaseMoveSpeedVFXOjb = ObjectPoolManager.SpawnObject(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                    
                    statusEffectSource = m_increaseMoveSpeedVFXOjb.GetComponent<StatusEffectSource>();
                    if (statusEffectSource)
                    {
                        statusEffectSource.SetStatusEffectSource(statusEffect.m_sender);
                    }
                }

                break;
            case StatusEffectData.EffectType.DecreaseHealth:
                if (!m_decreaseHealthVFXOjb && statusEffect.m_data.m_effectVFX)
                {
                    m_decreaseHealthVFXOjb = ObjectPoolManager.SpawnObject(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                    
                    statusEffectSource = m_decreaseHealthVFXOjb.GetComponent<StatusEffectSource>();
                    if (statusEffectSource)
                    {
                        statusEffectSource.SetStatusEffectSource(statusEffect.m_sender);
                    }
                }

                break;
            case StatusEffectData.EffectType.IncreaseHealth:
                if (!m_increaseHealthVFXOjb && statusEffect.m_data.m_effectVFX)
                {
                    m_increaseHealthVFXOjb = ObjectPoolManager.SpawnObject(statusEffect.m_data.m_effectVFX, m_targetPoint.position, Quaternion.identity, transform);
                    
                    statusEffectSource = m_increaseHealthVFXOjb.GetComponent<StatusEffectSource>();
                    if (statusEffectSource)
                    {
                        statusEffectSource.SetStatusEffectSource(statusEffect.m_sender);
                    }
                }

                break;
            case StatusEffectData.EffectType.DecreaseArmor:
                break;
            case StatusEffectData.EffectType.IncreaseArmor:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        //Damage & Healing per tick.
        if (statusEffect.m_elapsedTime > statusEffect.m_nextTickTime)
        {
            if (statusEffect.m_data.m_damage != 0)
            {
                //Modify health
                OnTakeDamage(statusEffect.m_data.m_damage);
            }

            statusEffect.m_nextTickTime += statusEffect.m_data.m_tickSpeed;
        }

        //Move Speed multipliers.
        if (statusEffect.m_data.m_speedModifier != 1)
        {
            //Debug.Log($"Move Speed modifier found.");
            //Modify speed
            if (statusEffect.m_data.m_speedModifier < 1 && statusEffect.m_data.m_speedModifier < m_lastSpeedModifierSlower)
            {
                m_lastSpeedModifierSlower = statusEffect.m_data.m_speedModifier;
                //Debug.Log($"Set slower speed modifier to: {m_lastSpeedModifierSlower}");
            }

            if (statusEffect.m_data.m_speedModifier > 1 && statusEffect.m_data.m_speedModifier > m_lastSpeedModifierFaster)
            {
                m_lastSpeedModifierFaster = statusEffect.m_data.m_speedModifier;
                //Debug.Log($"Set slower speed modifier to: {m_lastSpeedModifierFaster}");
            }
        }

        if (statusEffect.m_data.m_damageModifier != 1)
        {
            //Modify Damage Taken
            if (statusEffect.m_data.m_damageModifier < 1 && statusEffect.m_data.m_damageModifier < m_lastDamageModifierLower)
            {
                m_lastDamageModifierLower = statusEffect.m_data.m_damageModifier;
                //Debug.Log($"Set Lower Damage Modifier to: {m_lastDamageModifierHigher} on {gameObject.name}");
            }

            if (statusEffect.m_data.m_damageModifier > 1 && statusEffect.m_data.m_damageModifier > m_lastDamageModifierHigher)
            {
                m_lastDamageModifierHigher = statusEffect.m_data.m_damageModifier;
                //Debug.Log($"Set Higher Damage Modifier to: {m_lastDamageModifierHigher} on {gameObject.name}");
            }
        }
    }

    public void RemoveEffect(StatusEffect statusEffect)
    {
        m_expiredStatusEffects.Add(statusEffect);

        VisualEffect visualEffect;
        switch (statusEffect.m_data.m_effectType)
        {
            case StatusEffectData.EffectType.DecreaseMoveSpeed:
                m_lastSpeedModifierSlower = 1;
                if (m_decreaseMoveSpeedVFXOjb)
                {
                    visualEffect = m_decreaseMoveSpeedVFXOjb.GetComponent<VisualEffect>();
                    if (visualEffect)
                    {
                        visualEffect.Stop();
                    }

                    ObjectPoolManager.ReturnObjectToPool(m_decreaseMoveSpeedVFXOjb, ObjectPoolManager.PoolType.ParticleSystem);

                    m_decreaseMoveSpeedVFXOjb = null;
                }

                break;
            case StatusEffectData.EffectType.IncreaseMoveSpeed:
                m_lastSpeedModifierFaster = 1;
                if (m_increaseMoveSpeedVFXOjb)
                {
                    visualEffect = m_increaseMoveSpeedVFXOjb.GetComponent<VisualEffect>();
                    if (visualEffect)
                    {
                        visualEffect.Stop();
                    }

                    ObjectPoolManager.ReturnObjectToPool(m_increaseMoveSpeedVFXOjb, ObjectPoolManager.PoolType.ParticleSystem);

                    m_increaseMoveSpeedVFXOjb = null;
                }

                break;
            case StatusEffectData.EffectType.DecreaseHealth:
                if (m_decreaseHealthVFXOjb)
                {
                    Debug.Log($"Trying to remove DoT Visual Effect from {gameObject.name}");
                    visualEffect = m_decreaseHealthVFXOjb.GetComponent<VisualEffect>();
                    if (visualEffect)
                    {
                        visualEffect.Stop();
                    }

                    ObjectPoolManager.ReturnObjectToPool(m_decreaseHealthVFXOjb, ObjectPoolManager.PoolType.ParticleSystem);

                    m_decreaseHealthVFXOjb = null;
                }

                break;
            case StatusEffectData.EffectType.IncreaseHealth:
                if (m_increaseHealthVFXOjb)
                {
                    visualEffect = m_increaseHealthVFXOjb.GetComponent<VisualEffect>();
                    if (visualEffect)
                    {
                        visualEffect.Stop();
                    }

                    ObjectPoolManager.ReturnObjectToPool(m_increaseHealthVFXOjb, ObjectPoolManager.PoolType.ParticleSystem);


                    m_increaseHealthVFXOjb = null;
                }

                break;
            case StatusEffectData.EffectType.DecreaseArmor:
                m_lastDamageModifierHigher = 1;
                //Debug.Log($"Set Higher Damage Modifier to: {m_lastDamageModifierHigher} on {gameObject.name}");
                break;
            case StatusEffectData.EffectType.IncreaseArmor:
                m_lastDamageModifierLower = 1;
                //Debug.Log($"Set Lower Damage Modifier to: {m_lastDamageModifierHigher} on {gameObject.name}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public virtual void RequestRemoveEffect(GameObject sender)
    {
        //For each status effect, see if the sender matches, if it does, remove the effect.
        for (int i = 0; i < m_statusEffects.Count; i++)
        {
            if (sender == m_statusEffects[i].m_sender)
            {
                //We found a sender match, update the existing effect.
                RemoveEffect(m_statusEffects[i]);
                break;
            }
        }
    }
}

[Serializable]
public class StatusEffect
{
    public GameObject m_sender;
    public StatusEffectData m_data;
    public float m_elapsedTime;
    public float m_nextTickTime;

    public StatusEffect(GameObject sender, StatusEffectData data)
    {
        m_sender = sender;
        m_data = data;
    }
    
    /*public void SetSender(GameObject obj)
    {
        m_sender = obj;
    }*/
}