using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyThrall : EnemyController
{
    private EnemyHost m_enemyHost;
    public int m_cellsPerOoze = 10;                     // Cells needed to travel before oozing.
    public int m_oozeCount = 10;                        // Number of oozes to spawn.
    public int m_oozeRange = 6;                         // How far away an ooze may travel.
    public GameObject m_oozeObj;                        // Obj to spawn.
    public StatusEffectData m_vulnerabilityEffectData;  // The effect applied while oozing.
    public Renderer m_renderer;
    public Vector2 m_dissolveRange;
    public float m_dissolveShieldDuration = 0.3f;
    
    private List<Cell> m_newOozeCells;
    private float m_storedSpeedModifier;
    private Coroutine m_oozeCoroutine;
    private Material m_shieldMaterial;
    private float m_curDissolve;
    
    public void SetHost(EnemyHost enemyHost)
    {
        m_enemyHost = enemyHost;
        SetEnemyData(m_enemyData);

        if (m_shieldMaterial == null)
        {
            m_shieldMaterial = m_renderer.materials[1];
        }
        m_shieldMaterial.SetFloat("_DissolveValue", m_dissolveRange.x);
    }
    
    // HANDLING DAMAGE
    public override void OnTakeDamage(float dmg)
    {
        //Send damage to enemyHost
        float cumDamage = dmg * m_baseDamageMultiplier * m_lastDamageModifierHigher * m_lastDamageModifierLower;
        m_enemyHost.OnTakeDamage(cumDamage);

        //Audio

        //VFX
    }
    
    // OOZING
    public override void HandleMovement()
    {
        base.HandleMovement();

        // Have we moved far enough?
        if (m_cellsTravelled == m_cellsPerOoze)
        {
            Debug.Log($"Cells Travelled: {m_cellsTravelled}, Cells per Ooze {m_cellsPerOoze}.");
            
            m_cellsTravelled = 0;
            
            SpawnOoze();
        }
    }
    
    void SpawnOoze()
    {
        // Stop moving
        m_baseMoveSpeed = 0f;
        
        RequestStopAudioLoop(m_audioSource);
        
        m_shieldMaterial.DOFloat(m_dissolveRange.y, "_DissolveValue", m_dissolveShieldDuration);
        
        if (m_vulnerabilityEffectData)
        {
            m_storedSpeedModifier = m_lastSpeedModifierSlower;
            StatusEffect statusEffect = new StatusEffect(gameObject, m_vulnerabilityEffectData);
            ApplyEffect(statusEffect);
        }
        
        m_newOozeCells = new List<Cell>(GetOozeCells(m_oozeCount));

        m_oozeCoroutine = StartCoroutine(SpawnOozeWithDelay());
    }
    
    IEnumerator SpawnOozeWithDelay()
    {
        m_animator.SetTrigger("IsVulnerable");
        
        foreach (Cell cell in m_newOozeCells)
        {
            GameObject ooze = ObjectPoolManager.SpawnObject(m_oozeObj, transform.position, Quaternion.identity, null, ObjectPoolManager.PoolType.Enemy);
            ooze.GetComponent<CellOoze>().SetOozeCell(cell);

            // Wait for 0.3 seconds before spawning the next ooze
            yield return new WaitForSeconds(m_vulnerabilityEffectData.m_lifeTime / m_newOozeCells.Count);
        }

        ResumeMoving();
        m_oozeCoroutine = null;
    }

    void ResumeMoving()
    {
        m_shieldMaterial.DOFloat(m_dissolveRange.x, "_DissolveValue", m_dissolveShieldDuration / 2);
        RequestPlayAudioLoop(m_enemyData.m_audioLifeLoop, m_audioSource);
        
        // Resume moving -- Refresh where we want to move to, in case the player has adjusted the path during the spawning of ooze.
        // This chunk of code is copied from the base HandleMovement function.
        float wiggleMagnitude = m_enemyData.m_movementWiggleValue * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        Vector2 nextCellPosOffset = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f) * wiggleMagnitude);
        Vector3 m_curCell3dPos = new Vector3(m_curCell.m_cellPos.x, 0, m_curCell.m_cellPos.y);
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
        m_baseMoveSpeed = m_enemyData.m_moveSpeed;
        m_animator.SetTrigger("IsWalking");
    }

    List<Cell> GetOozeCells(int oozeCount)
    {
        List<Cell> cells = new List<Cell>();
        Vector2Int curPos = Util.GetCellFrom3DPos(transform.position).m_cellPos;
        if (GameplayManager.Instance.m_oozeManager == null) GameplayManager.Instance.m_oozeManager = new OozeManager();
        
        // Calculate how many possible cells can be chosen, and track how many we've checked. (So we don't check forever if we dont have enough valid cells)
        int totalCellsInRange = m_oozeRange * m_oozeRange;
        int cellsChecked = 0;
        
        int i = 0;
        while (i < oozeCount && cellsChecked <= totalCellsInRange)
        {
            int randomX = Random.Range(-m_oozeRange, m_oozeRange + 1);
            int randomY = Random.Range(-m_oozeRange, m_oozeRange + 1);
            Vector2Int randomPos = new Vector2Int(curPos.x + randomX, curPos.y + randomY);
            Cell randomCell = Util.GetCellFromPos(randomPos);

            // Check if the cell is not already oozed
            if (randomCell != null && !GameplayManager.Instance.m_oozeManager.IsCellOozed(randomCell))
            {
                cells.Add(randomCell);
                GameplayManager.Instance.m_oozeManager.AddCell(randomCell);
                ++i;  // Increment only if a valid cell was found and added
            }
            ++cellsChecked;
        }
        
        return cells;
    }
    
    public override void DamageCastle()
    {
        GameplayManager.Instance.m_castleController.TakeBossDamage(1);
        m_enemyHost.ThrallReachedCastle(this);
    }
    
    public override void OnHealed(float heal, bool percentage)
    {
        m_enemyHost.OnHealed(heal, percentage);
    }

    public void HostHit()
    {
        HitFlash();
    }

    public override void OnEnemyDestroyed(Vector3 pos)
    {
        if (m_oozeCoroutine != null)
        {
            StopCoroutine(m_oozeCoroutine);
            m_lastSpeedModifierSlower = m_storedSpeedModifier;
            m_animator.SetTrigger("IsWalking");
            m_oozeCoroutine = null;
        }
        
        base.OnEnemyDestroyed(pos);
    }
    //
    
    //REMOVING BASE FUNCTIONALITY
    public override void SetupUI()
    {
        return;
    }
    
    public override void AddToGameplayList()
    {
        return;
    }
    
    public override void RemoveFromGameplayList()
    {
        return;
    }
    //

}
