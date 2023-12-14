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
    public EnemyData m_bossEnemyData;
    
    [Header("Grid Info")]
    public List<Vector2Int> m_bossGridCellPositions = new List<Vector2Int>();
    public int m_positionRadius = 8;

    private Vector3 m_cameraControllerPos;
    private BossSequenceTask m_bossTask;
    private GameObject m_activeBossIntroObj;
    private Vector3 m_bossSpawnPosition;
    //public GameObject m_debugShape;
    
    public enum BossSequenceTask
    {
        Idle,
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
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        UpdateTask(BossSequenceTask.Idle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        switch (newState)
        {
            case GameplayManager.GameplayState.BuildGrid:
                break;
            case GameplayManager.GameplayState.PlaceObstacles:
                break;
            case GameplayManager.GameplayState.FloodFillGrid:
                break;
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                break;
            case GameplayManager.GameplayState.SpawnBoss:
                UpdateTask(BossSequenceTask.Setup);
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                break;
            case GameplayManager.GameplayState.Paused:
                break;
            case GameplayManager.GameplayState.Victory:
                break;
            case GameplayManager.GameplayState.Defeat:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }
    
    
    public void UpdateTask(BossSequenceTask newTask)
    {
        m_bossTask = newTask;
        Debug.Log($"Boss Sequence Task: {m_bossTask}");
        
        switch (newTask)
        {
            case BossSequenceTask.Idle:
                break;
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

        Vector2Int centerPoint = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_castleController.transform.position);
        m_bossGridCellPositions = HexagonGenerator(centerPoint, m_positionRadius);
        //We're done setting up. Time for boss Introduction.
        UpdateTask(BossSequenceTask.BossIntroduction);
    }
    
    private List<Vector2Int> HexagonGenerator(Vector2Int centerPoint, int radius)
    {
        List<Vector2Int> points = new List<Vector2Int>();
        float startAngle = Mathf.PI / 2;
        for (int i = 0; i < 6; ++i)
        {
            float angle = startAngle + 2 * Mathf.PI / 6 * i;
            int x = Mathf.RoundToInt(centerPoint.x + radius * MathF.Cos(angle));
            int y = Mathf.RoundToInt(centerPoint.y + radius * MathF.Sin(angle));

            Vector2Int point = new Vector2Int(x, y);
            points.Add(point);
            //Instantiate(m_debugShape, new Vector3(point.x, 0, point.y), quaternion.identity, transform);
        }

        return points;
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
        Vector3 castlePosition = GameplayManager.Instance.m_castleController.transform.position;
        float castleXPosition = castlePosition.x;
        float gridYposition = GridManager.Instance.m_gridHeight + 5f;
        m_bossSpawnPosition = new Vector3(castleXPosition, 0, gridYposition);
        
        GameObject activeBossObj = Instantiate(m_bossEnemyData.m_enemyPrefab, m_bossSpawnPosition, Quaternion.identity, GameplayManager.Instance.m_enemiesObjRoot);
        activeBossObj.GetComponent<EnemyFlierBoss>().m_bossSequenceController = this;
        activeBossObj.GetComponent<EnemyController>().SetEnemyData(m_bossEnemyData);
        
        UpdateTask(BossSequenceTask.BossLifetime);
    }

    public BossSequenceTask GetCurrentTask()
    {
        return m_bossTask;
    }

    /*public int GetNextGridCell(int lastGridCellIndex, int curGridCellIndex)
    {
        int nextGridCellIndex = 0;
        float maxDistance = Vector2Int.Distance(Vector2Int.zero, new Vector2Int(m_bossGridCellWidth, m_bossGridCellHeight));
        
        //Get the neighbor cell indexes.
        List<int> neighborCellIndexes = new List<int>();
        Vector2Int curPos = m_bossGridCellPositions[curGridCellIndex];
        for (var i = 0; i < m_bossGridCellPositions.Count; i++)
        {
            //If the difference between current and i's width and height is less than or equal to the gridcell size.
            var cellPos = m_bossGridCellPositions[i];
            if (Vector2Int.Distance(cellPos, curPos) <= maxDistance)
            {
                //Makesure the current index is not our last index or our current index.
                if (i != lastGridCellIndex && i != curGridCellIndex)
                {
                    neighborCellIndexes.Add(i);
                }
            }
        }

        Debug.Log($"{neighborCellIndexes.Count} neighbors found from cell {curGridCellIndex}, excluding {lastGridCellIndex}.");
        //Get a random cell index, exclude current and last indexes.
        nextGridCellIndex = neighborCellIndexes[Random.Range(0, neighborCellIndexes.Count - 1)];
        
        return nextGridCellIndex;
    }*/

    public Vector3 GetNextGoalPosition(int i)
    {
        Vector3 goalPos = Vector3.zero;

        Vector2Int cellPos = m_bossGridCellPositions[i];

        float offset = 3;
        float x = Random.Range(-offset, offset);
        float y = Random.Range(-offset, offset);

        goalPos = new Vector3(cellPos.x + x, 0, cellPos.y + y);
        Debug.Log($"cell pos: {cellPos} / goal pos: {goalPos}");
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
    
    
    /*public int m_bossGridBuffer = 4;
    public int m_bossGridCols = 4;
    public int m_bossGridRows = 3;
    private int m_bossGridCellWidth;
    private int m_bossGridCellHeight;
    private void BuildGrid()
    {
        //Older code to build a sparse grid.
        m_bossGridCellWidth = (GridManager.Instance.m_gridWidth - m_bossGridBuffer * 2) / m_bossGridCols;
        m_bossGridCellHeight = (GridManager.Instance.m_gridHeight - m_bossGridBuffer * 2) / m_bossGridRows;

        //Start at the grid buffer, increment by gridCellWidth, until we surpass gridWith - buffer.
        for (int x = m_bossGridBuffer + m_bossGridCellWidth/2; x <= GridManager.Instance.m_gridWidth - m_bossGridBuffer; x += m_bossGridCellWidth)
        {
            for (int y = m_bossGridBuffer + m_bossGridCellHeight/2; y <= GridManager.Instance.m_gridHeight - m_bossGridBuffer; y += m_bossGridCellHeight)
            {
                m_bossGridCellPositions.Add(new Vector2Int(x, y));
            }
        }
    }*/
}

