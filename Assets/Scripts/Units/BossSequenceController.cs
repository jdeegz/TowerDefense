using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class BossSequenceController : MonoBehaviour
{
    [Header("Boss Objects")] 
    public GameObject m_bossIntroductionObj;
    //public EnemyController m_bossEnemyController;
    public EnemyData m_bossEnemyData;
    
    [Header("Grid Info")]
    public List<Vector2Int> m_bossGridCellPositions = new List<Vector2Int>();
    public int m_bossGridBuffer = 4;
    public int m_bossGridCols = 4;
    public int m_bossGridRows = 3;
    private int m_bossGridCellWidth;
    private int m_bossGridCellHeight;

    private Vector3 m_cameraControllerPos;
    private BossSequenceTask m_bossTask;
    private GameObject m_activeBossIntroObj;
    private Vector3 m_bossSpawnPosition;

    public GameObject tempobj;
    
    public enum BossSequenceTask
    {
        Setup, // Create the assets in the scene and supply them with necessary info.
        BossIntroduction, //State for cool boss intro
        BossSpawn, //After the intro, spawn the boss somewhere.
        BossLifetime, //BossController takes over and does stuff.
        BossFlee, //Used if we want the boss to escape without dying.
        BossDeath, //Handle what may happen after the boss is removed, like spawn new things.
    }
    
    // Start is called before the first frame update
    void Start()
    {
        UpdateTask(BossSequenceTask.Setup);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateTask(BossSequenceTask newTask)
    {
        m_bossTask = newTask;
        Debug.Log($"Boss Sequence Task: {m_bossTask}");
        
        switch (newTask)
        {
            case BossSequenceTask.Setup:
                HandleSetup();
                break;
            case BossSequenceTask.BossIntroduction:
                HandleBossIntroduction();
                break;
            case BossSequenceTask.BossSpawn:
                HandleBossSpawn();
                break;
            case BossSequenceTask.BossLifetime:
                break;
            case BossSequenceTask.BossFlee:
                break;
            case BossSequenceTask.BossDeath:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newTask), newTask, null);
        }
    }

    void HandleSetup()
    {
        //Build the grid.
        m_bossGridCellWidth = (GridManager.Instance.m_gridWidth - m_bossGridBuffer * 2) / m_bossGridCols;
        m_bossGridCellHeight = (GridManager.Instance.m_gridHeight - m_bossGridBuffer * 2) / m_bossGridRows;

        //Start at the grid buffer, increment by gridCellWidth, until we surpass gridWith - buffer.
        for (int x = m_bossGridBuffer + m_bossGridCellWidth/2; x <= GridManager.Instance.m_gridWidth - m_bossGridBuffer; x += m_bossGridCellWidth)
        {
            for (int y = m_bossGridBuffer + m_bossGridCellHeight/2; y <= GridManager.Instance.m_gridHeight - m_bossGridBuffer; y += m_bossGridCellHeight)
            {
                m_bossGridCellPositions.Add(new Vector2Int(x, y));
                Instantiate(tempobj, new Vector3(x, 0, y), quaternion.identity);
            }
        }

        //We're done setting up. Time for boss Introduction.
        UpdateTask(BossSequenceTask.BossIntroduction);
    }

    void HandleBossIntroduction()
    {
        m_activeBossIntroObj = Instantiate(m_bossIntroductionObj, CameraController.Instance.transform.position, quaternion.identity, transform);
        BossIntroductionController bossIntroController = m_activeBossIntroObj.GetComponent<BossIntroductionController>();
        bossIntroController.OnIntroductionComplete += BossIntroductionCompleted;
    }

    //Triggered by the DOTween OnComplete on the boss shadow Quad.
    void BossIntroductionCompleted()
    {
        m_activeBossIntroObj.SetActive(false);
        UpdateTask(BossSequenceTask.BossSpawn);
    }

    void HandleBossSpawn()
    {
        //Find the position we want to spawn the boss.
        //Spawn it horizontally centered to the castle, and just off the height of the grid (off screen).
        float castleXPosition = GameplayManager.Instance.m_castleController.transform.position.x;
        float gridYposition = GridManager.Instance.m_gridHeight + 5f;
        m_bossSpawnPosition = new Vector3(castleXPosition, 0, gridYposition);
        
        GameObject activeBossObj = Instantiate(m_bossEnemyData.m_enemyPrefab, m_bossSpawnPosition, Quaternion.identity, GameplayManager.Instance.m_enemiesObjRoot);
        activeBossObj.GetComponent<EnemyController>().SetEnemyData(m_bossEnemyData);
        activeBossObj.GetComponent<EnemyFlierBoss>().m_bossSequenceController = this;
        
        UpdateTask(BossSequenceTask.BossLifetime);
    }

    public BossSequenceTask GetCurrentTask()
    {
        return m_bossTask;
    }

    public int GetNextGridCell(int lastGridCellIndex, int curGridCellIndex)
    {
        int nextGridCellIndex = 0;
        
        //Get the neighbor cell indexes.
        List<int> neighborCellIndexes = new List<int>();
        Vector2Int curPos = m_bossGridCellPositions[curGridCellIndex];
        for (var i = 0; i < m_bossGridCellPositions.Count; i++)
        {
            //If the difference between current and i's width and height is less than or equal to the gridcell size.
            var cellPos = m_bossGridCellPositions[i];
            if (Mathf.Abs(cellPos.x - curPos.x) <= m_bossGridCellWidth && Mathf.Abs(cellPos.y - curPos.y) <= m_bossGridCellHeight)
            {
                //Makesure the current index is not our last index or our current index.
                if (i != lastGridCellIndex || i != curGridCellIndex)
                {
                    neighborCellIndexes.Add(i);
                }
            }
        }

        //Get a random cell index, exclude current and last indexes.
        nextGridCellIndex = Random.Range(0, neighborCellIndexes.Count - 1);
        
        return nextGridCellIndex;
    }

    public Vector3 GetNextGoalPosition(int i)
    {
        Vector3 goalPos = Vector3.zero;

        Vector2Int cellPos = m_bossGridCellPositions[i];
        
        float x = Random.Range(cellPos.x - m_bossGridCellWidth / 2, cellPos.x + m_bossGridCellWidth / 2);
        float y = Random.Range(cellPos.y - m_bossGridCellHeight / 2, cellPos.y + m_bossGridCellHeight / 2);

        goalPos.x = x;
        goalPos.z = y;

        return goalPos;
    }

    public int GetCellIndexFromVector3(Vector3 pos)
    {
        //Convert to vector2 for calculating distances with our other list.
        Vector2Int curPos = Util.GetVector2IntFrom3DPos(pos);
        int closestIndex = -1;
        float closestDistance = 9999f;

        for (int i = 0; i < m_bossGridCellPositions.Count; ++i)
        {
            //sqrMagnitude is less expensive than Vector2Int.Distance
            float distance = (curPos - m_bossGridCellPositions[i]).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestIndex = i;
                closestDistance = distance;
            }
        }
        
        return closestIndex;
    }
}

