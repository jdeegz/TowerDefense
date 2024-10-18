using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyThrall : EnemyController
{
    private EnemyHost m_enemyHost;
    public int m_cellsPerOoze = 10;                     // Cells needed to travel before oozing.
    public int m_oozeCount = 10;                        // Number of oozes to spawn.
    public int m_oozeRange = 6;                         // How far away an ooze may travel.
    public GameObject m_oozeObj;                        // Obj to spawn.
    public StatusEffectData m_vulnerabilityEffectData;          // The effect applied while oozing.
    private List<Cell> m_newOozeCells;
    private float m_storedSpeedModifier;
    
    public void SetHost(EnemyHost enemyHost)
    {
        m_enemyHost = enemyHost;
        SetEnemyData(m_enemyData);
    }
    
    // HANDLING DAMAGE
    public override void OnTakeDamage(float dmg)
    {
        
        //Send damage to enemyHost
        m_enemyHost.OnTakeDamage(dmg);

        //Audio
        int i = Random.Range(0, m_enemyData.m_audioDamagedClips.Count);
        m_audioSource.PlayOneShot(m_enemyData.m_audioDamagedClips[i]);

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
        m_storedSpeedModifier = m_lastSpeedModifierSlower;
        m_lastSpeedModifierSlower = 0f;
        
        if (m_vulnerabilityEffectData)
        {
            StatusEffect statusEffect = new StatusEffect();
            statusEffect.SetSender(gameObject);
            statusEffect.m_data = m_vulnerabilityEffectData;
            ApplyEffect(statusEffect);
        }
        
        m_newOozeCells = GetOozeCells(m_oozeCount);

        StartCoroutine(SpawnOozeWithDelay());
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
        // Resume moving -- Refresh where we want to move to, in case the player has adjusted the path during the spawning of ooze.
        // This chunk of code is copied from the base HandleMovement function.
        float wiggleMagnitude = m_enemyData.m_movementWiggleValue * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
        Vector2 nextCellPosOffset = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f) * wiggleMagnitude);
        Vector3 m_curCell3dPos = new Vector3(m_curCell.m_cellPos.x, 0, m_curCell.m_cellPos.y);
        m_nextCellPosition = m_curCell3dPos + new Vector3(m_curCell.m_directionToNextCell.x + nextCellPosOffset.x, 0, m_curCell.m_directionToNextCell.z + nextCellPosOffset.y);
        m_lastSpeedModifierSlower = m_storedSpeedModifier;
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
            if (!GameplayManager.Instance.m_oozeManager.IsCellOozed(randomCell))
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
        //Hit Flash
        if (m_allRenderers == null || !gameObject.activeInHierarchy) return;
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }
        m_hitFlashCoroutine = StartCoroutine(HitFlash());
    }
    //
    
    //REDIRECT EFFECTS TO HOST.
    public override void RequestRemoveEffect(GameObject sender)
    {
        m_enemyHost.RequestRemoveEffect(sender);
    }
    
    public override void ApplyEffect(StatusEffect statusEffect)
    {
        m_enemyHost.ApplyEffect(statusEffect);
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
