using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class BossSequenceController : MonoBehaviour
{
    [Header("Boss Objects")] public GameObject m_bossIntroductionObj;
    public EnemyData m_bossEnemyData;

    [Header("Grid Info")] public List<Vector2Int> m_bossGridCellPositions = new List<Vector2Int>();
    public List<BossObeliskObj> m_bossObeliskPathPoints = new List<BossObeliskObj>();
    private int m_bossObeliskPathIndex = 0;

    [FormerlySerializedAs("m_positionRadius")]
    public int m_castlePathingRadius = 8;

    public enum SpawnDirection
    {
        North,
        East,
        South,
        West
    }

    public SpawnDirection m_spawnDirection;

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
        m_bossGridCellPositions = HexagonGenerator(centerPoint, m_castlePathingRadius);

        foreach (Obelisk obelisk in GameplayManager.Instance.m_obelisksInMission)
        {
            BossObeliskObj newObeliskObj = new BossObeliskObj();
            newObeliskObj.m_obeliskProgressPercent = obelisk.GetObeliskProgress();

            Vector2Int obeliskPosition = Util.GetVector2IntFrom3DPos(obelisk.transform.position);
            newObeliskObj.m_obeliskPointPositions = DiamondGenerator(obeliskPosition, (int)obelisk.m_obeliskData.m_obeliskRange);
            m_bossObeliskPathPoints.Add(newObeliskObj);
        }

        m_bossObeliskPathPoints.Sort(new BossObeliskObjComparer());

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
    
    private List<Vector2Int> DiamondGenerator(Vector2Int centerPoint, int radius)
    {
        List<Vector2Int> points = new List<Vector2Int>();
        float startAngle = Mathf.PI / 2;
        for (int i = 0; i < 4; ++i)
        {
            float angle = startAngle + 2 * Mathf.PI / 4 * i;
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
        float castleZPosition = castlePosition.z;
        switch (m_spawnDirection)
        {
            case SpawnDirection.North:
                m_bossSpawnPosition = new Vector3(castleXPosition, 0, GridManager.Instance.m_gridHeight + 5f);
                break;
            case SpawnDirection.East:
                m_bossSpawnPosition = new Vector3(GridManager.Instance.m_gridWidth + 5f, 0, castleZPosition);
                break;
            case SpawnDirection.South:
                m_bossSpawnPosition = new Vector3(castleXPosition, 0, -5f);
                break;
            case SpawnDirection.West:
                m_bossSpawnPosition = new Vector3(-5f, 0, castleZPosition);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

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

    public (List<Vector2Int>, Vector3, int) GetNextGoalList(Vector3 curPos)
    {
        //Our obelisks lists are sorted from Closest to completetion to lowest.
        //We need to know if we've started down this list. And also if we've reased the end of this list to start over.
        List<Vector2Int> newList = new List<Vector2Int>();

        //If we've looped all the way around, we want to go back to circling the castle and start if all over.
        if (m_bossObeliskPathIndex == m_bossObeliskPathPoints.Count)
        {
            m_bossObeliskPathIndex = 0; //Reset index for next loop
            newList = m_bossGridCellPositions; //List to send.
        }
        else
        {
            newList = m_bossObeliskPathPoints[m_bossObeliskPathIndex].m_obeliskPointPositions; //List to send 
            ++m_bossObeliskPathIndex; //Increment for next loop
        }
        
        //We now need to find the closts point in the list to move to.
        Vector3 closestGoalPos = new Vector3();
        int pathingStartIndex = 0;
        float shortestDistance = 9999f;
        for (int i = 0; i < newList.Count; ++i)
        {
            Vector3 pointPos = new Vector3(newList[i].x, 0, newList[i].y);
            float distance = Vector3.Distance(curPos, pointPos);
            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                closestGoalPos = pointPos;
                pathingStartIndex = i;
            }
        }

        closestGoalPos = GetNextGoalPosition(newList, pathingStartIndex);
        
        return (newList, closestGoalPos, pathingStartIndex);
    }

    public Vector3 GetNextGoalPosition(List<Vector2Int> curPositionList, int i)
    {
        //If the new index is equal to our starting index, we've completed a loop and need to pick a new list to operate from.

        Vector3 goalPos = Vector3.zero;

        Vector2Int cellPos = curPositionList[i];

        float offset = 1.5f;
        float x = Random.Range(-offset, offset);
        float y = Random.Range(-offset, offset);

        goalPos = new Vector3(cellPos.x + x, 0, cellPos.y + y);
        Debug.Log($"cell pos: {cellPos} / goal pos: {goalPos}");
        return goalPos;
    }

    public (int, Vector3) GetStartingGoalPosition()
    {
        Vector3 goalPos = Vector3.zero;
        Vector2Int cellPos = new Vector2Int();
        int goalIndex = 0;
        switch (m_spawnDirection)
        {
            case SpawnDirection.North:
                goalIndex = 0;
                break;
            case SpawnDirection.East:
                goalIndex = 1;
                break;
            case SpawnDirection.South:
                goalIndex = 3;
                break;
            case SpawnDirection.West:
                goalIndex = 4;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        cellPos = m_bossGridCellPositions[goalIndex];
        goalPos = new Vector3(cellPos.x, 0, cellPos.y);
        Debug.Log($"cell pos: {cellPos} / goal pos: {goalPos}");
        return (goalIndex, goalPos);
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

public class BossObeliskObj
{
    public List<Vector2Int> m_obeliskPointPositions;
    public float m_obeliskProgressPercent;
}

public class BossObeliskObjComparer : IComparer<BossObeliskObj>
{
    public int Compare(BossObeliskObj x, BossObeliskObj y)
    {
        // Compare based on the YourVariable property
        return y.m_obeliskProgressPercent.CompareTo(x.m_obeliskProgressPercent);
    }
}