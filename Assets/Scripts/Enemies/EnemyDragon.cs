using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyDragon : EnemyController
{
    public GameObject m_muzzleObj;
    public int m_castlePathingRadius = 8;
    
    private BossSequenceController m_bossSequenceController;
    
    private int m_startGoalIndex;
    private int m_curGoalIndex;
    private List<Vector2Int> m_curPositionList;
    private Vector3 m_curGoalPos;
    private Vector3 m_castlePos;
    private bool m_isStrafing = false;
    private float m_coneStartDelay;
    private float m_coneEndBuffer;
    private float m_moveDistance;
    private float m_distanceTravelled;
    private int m_moveCounter;
    private float m_rotationThreadhold = 0.999f;
    private Coroutine m_curCoroutine;
    private BossState m_bossState;
    private List<Vector2Int> m_bossGridCellPositions = new List<Vector2Int>();
    private List<BossObeliskObj> m_bossObeliskPathPoints = new List<BossObeliskObj>();
    private int m_bossObeliskPathIndex = 0;

    private enum BossState
    {
        Idle,
        RotateToDestination,
        MoveToDestination,
        RotateToTarget,
        AttackTarget,
        Death,
    }

    public override void SetupEnemy(bool active)
    {
        base.SetupEnemy(active);
        
        SetBossPathPoints();
        
        SetSpawnPosition();

        InitiateBossMovement();
        
        SetSequenceController(GameplayManager.Instance.GetActiveBossController());
    }
    
    public void SetSequenceController(BossSequenceController controller)
    {
        m_bossSequenceController = controller;
    }

    void SetBossPathPoints()
    {
        //Build the grid.
        Vector2Int centerPoint = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_castleController.transform.position);
        m_bossGridCellPositions = DiamondGenerator(centerPoint, m_castlePathingRadius);
        
        m_curPositionList = m_bossGridCellPositions; // We start rotating around the bass first.

        foreach (Obelisk obelisk in GameplayManager.Instance.m_obelisksInMission) // Get the points around obelisks we want to move towards. Then sort them by obelisk completion %.
        {
            BossObeliskObj newObeliskObj = new BossObeliskObj();
            newObeliskObj.m_obeliskProgressPercent = obelisk.GetObeliskProgress();

            Vector2Int obeliskPosition = Util.GetVector2IntFrom3DPos(obelisk.transform.position);
            newObeliskObj.m_obeliskPointPositions = DiamondGenerator(obeliskPosition, (int)obelisk.m_obeliskData.m_obeliskRange);
            m_bossObeliskPathPoints.Add(newObeliskObj);
        }

        m_bossObeliskPathPoints.Sort(new BossObeliskObjComparer());
    }
    
    void SetSpawnPosition()
    {
        //Find the position we want to spawn the boss.
        //Spawn it horizontally centered to the castle, and just off the height of the grid (off screen).
        
        float spawnOffset = 5f;
        
        Vector3 castlePosition = GameplayManager.Instance.m_castleController.transform.position;
        float castleXPosition = castlePosition.x;
        float castleZPosition = castlePosition.z;

        float gridZHeight = GridManager.Instance.m_gridHeight;
        float gridXWidth = GridManager.Instance.m_gridWidth;

        float xDelta = castleXPosition; //Default to distance from left edge.
        float zDelta = castleZPosition; //Default to distance from bottom edge.
        float xMultiplier = -1;
        float zMultiplier = -1;
        
        if (castleXPosition < gridXWidth / 2) //If true, we're closer to the left edge.
        {
            xDelta = gridXWidth - castleXPosition; //Furthest horizontal edge is the right.
            xMultiplier = 1; //We are moving right from the castle position.

        }

        if (castleZPosition < gridZHeight / 2) //If true, we're closer to the bottom edge.
        {
            zDelta = gridZHeight - castleZPosition; //Furthest vertical edge is the top.
            zMultiplier = 1; //We are moving up from the castle position.
        }

        if (xDelta > zDelta) //Are we moving laterally or horizontally from the castle position?
        {
            zMultiplier = 0;
            if (xMultiplier > 0)
            {
                m_spawnDirection = SpawnDirection.East;
            }
            else
            {
                m_spawnDirection = SpawnDirection.West;
            }
        }
        else
        {
            xMultiplier = 0;
            if (zMultiplier > 0)
            {
                m_spawnDirection = SpawnDirection.North;
            }
            else
            {
                m_spawnDirection = SpawnDirection.South;
            }
        }

        Vector3 spawnPos = new Vector3(castleXPosition + ((xDelta +spawnOffset) * xMultiplier), 0, castleZPosition + ((zDelta + spawnOffset) * zMultiplier));
        transform.position = spawnPos;
    }
    
    void InitiateBossMovement()
    {
        //If we just spawned, travel towards N units away from the castle.
        (m_curGoalIndex, m_curGoalPos) = GetStartingGoalPosition();
        m_startGoalIndex = m_curGoalIndex;
        m_castlePos = GameplayManager.Instance.m_castleController.transform.position;
        transform.rotation = Quaternion.LookRotation(m_curGoalPos - transform.position);
        UpdateBossState(BossState.MoveToDestination);
    }
    
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
        Debug.Log($"cell pos: {cellPos} / goal pos: {goalPos} which is index {i} of {curPositionList.Count}.");
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

    
    public enum SpawnDirection
    {
        North,
        East,
        South,
        West
    }

    public SpawnDirection m_spawnDirection;

    public override void HandleMovement()
    {
        
    }

    void UpdateBossState(BossState newState)
    {
        m_bossState = newState;
        switch (m_bossState)
        {
            case BossState.Idle:
                break;
            case BossState.RotateToDestination:
                break;
            case BossState.MoveToDestination:
                break;
            case BossState.RotateToTarget:
                break;
            case BossState.AttackTarget:
                m_curCoroutine = StartCoroutine(Attack());
                break;
            case BossState.Death:
                if (m_curCoroutine != null) StopCoroutine(m_curCoroutine);
                //Do boss death stuff.
                //Spawn 4 seekers.
                //Have to keep gameplay state from switching due to not having alive enemies.
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator Attack()
    {
        yield return new WaitForSeconds(((BossEnemyData)m_enemyData).m_attackDelay);
        HandleAttack();
        yield return new WaitForSeconds(((BossEnemyData)m_enemyData).m_attackCooldown);
        UpdateBossState(BossState.RotateToDestination);
    }

    private IEnumerator UpdateStateAfterDelay(float i, BossState newState)
    {
        yield return new WaitForSeconds(i);
        UpdateBossState(newState);
    }

    private void HandleAttack()
    {
        ObjectPoolManager.SpawnObject(((BossEnemyData)m_enemyData).m_projectileObj, m_muzzleObj.transform.position, m_muzzleObj.transform.rotation, null, ObjectPoolManager.PoolType.Projectile);
    }

    public void Update()
    {
        switch (m_bossState)
        {
            case BossState.Idle:
                break;
            case BossState.RotateToDestination:
                // Calculate the rotation to face the target
                Quaternion rotationToDestination = Quaternion.LookRotation(m_curGoalPos - transform.position);
                float rotationToDestinationDotProduct = Mathf.Abs(Quaternion.Dot(transform.rotation, rotationToDestination));

                transform.rotation = Quaternion.Slerp(transform.rotation, rotationToDestination, m_baseLookSpeed * Time.deltaTime);

                if (rotationToDestinationDotProduct >= m_rotationThreadhold)
                {
                    m_curCoroutine = StartCoroutine(UpdateStateAfterDelay(1, BossState.MoveToDestination));
                }

                break;
            case BossState.MoveToDestination:
                if (m_moveCounter % ((BossEnemyData)m_enemyData).m_strafeAttackRate != 0) HandleCone();

                //Movement
                float speed = m_baseMoveSpeed * m_lastSpeedModifierFaster * m_lastSpeedModifierSlower;
                Vector3 direction = (m_curGoalPos - transform.position).normalized;
                transform.Translate(speed * Time.deltaTime * direction, Space.World);

                //Set the distance travelled
                m_distanceTravelled = m_moveDistance - Vector3.Distance(transform.position, m_curGoalPos);

                //Check if we're at our destination.
                if (Vector3.Distance(transform.position, m_curGoalPos) <= 0.05f)
                {
                    //Get the next destination, even if we're attacking right now.
                    UpdateMoveDestination();
                    SetConeDistances();
                }

                break;
            case BossState.RotateToTarget:
                // Calculate the rotation to face the target
                Quaternion rotationToCastle = Quaternion.LookRotation(m_castlePos - transform.position);
                float rotationToCastledotProduct = Mathf.Abs(Quaternion.Dot(transform.rotation, rotationToCastle));

                transform.rotation = Quaternion.Slerp(transform.rotation, rotationToCastle, m_baseLookSpeed * Time.deltaTime);

                if (rotationToCastledotProduct >= m_rotationThreadhold)
                {
                    UpdateBossState(BossState.AttackTarget);
                }

                break;
            case BossState.AttackTarget:
                break;
            case BossState.Death:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void UpdateMoveDestination()
    {
        m_isStrafing = true;
        
        ++m_curGoalIndex;
        
        if (m_curGoalIndex == m_curPositionList.Count)
        {
            m_curGoalIndex = 0;
        }

        ++m_moveCounter;

        //Check to see if we've completed a cycle around the current list.
        if (m_curGoalIndex == m_startGoalIndex)
        {
            //If we have, we need to pick a new list to operate on.
            (m_curPositionList, m_curGoalPos, m_startGoalIndex) = GetNextGoalList(transform.position);
            m_curGoalIndex = m_startGoalIndex;
        }
        else
        {
            m_curGoalPos = GetNextGoalPosition(m_curPositionList, m_curGoalIndex);
        }

        m_moveDistance = Vector3.Distance(transform.position, m_curGoalPos);
        
        //Do we need to Rotate to a new Destination, or Rotate to attack the castle?
        if (m_moveCounter % ((BossEnemyData)m_enemyData).m_castleAttackRate == 0)
        {
            UpdateBossState(BossState.RotateToTarget);
        }
        else
        {
            UpdateBossState(BossState.RotateToDestination);
        }
    }


    void SetConeDistances()
    {
        //Starting distance
        m_coneStartDelay = m_moveDistance * .2f; // Distance we need to travel before turning on cone.

        //Ending distance
        m_coneEndBuffer = m_moveDistance * .8f; //Distance we need to travel to turn the cone off.

        m_distanceTravelled = 0f;
    }

    void HandleCone()
    {
        //If the cone is disabled, and we're after start, before end, turn on cone.
        if (!m_muzzleObj.activeSelf && m_distanceTravelled > m_coneStartDelay && m_distanceTravelled < m_coneEndBuffer)
        {
            m_muzzleObj.SetActive(true);
        }
        else if (m_muzzleObj.activeSelf && (m_distanceTravelled < m_coneStartDelay || m_distanceTravelled > m_coneEndBuffer))
        {
            m_muzzleObj.SetActive(false);
        }
    }
    

    public override void RemoveObject()
    {
        //Disabling the Status Effect application to spawners. Movespeed after maze destruction too punishing.
        /*foreach (UnitSpawner spawner in GameplayManager.Instance.m_unitSpawners)
        {
            GameObject bossShardObj = Instantiate(((BossEnemyData)m_enemyData).m_bossShard, transform.position, quaternion.identity);

            bossShardObj.GetComponent<BossShard>().SetupBossShard(spawner.transform.position);
            spawner.SetSpawnerStatusEffect(((BossEnemyData)m_enemyData).m_spawnStatusEffect, ((BossEnemyData)m_enemyData).m_spawnStatusEffectWaveDuration);
        }*/
        
        base.RemoveObject();
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
    
    public override void AddToGameplayList()
    {
        GameplayManager.Instance.AddBossToList(this);
    }
    
    public override void RemoveFromGameplayList()
    {
        m_bossSequenceController.BossRemoved(m_curHealth);
        GameplayManager.Instance.RemoveBossFromList(this);
    }
    
    public override void SetupUI()
    {
        //Setup the Boss health meter.
        UIHealthMeter lifeMeter = ObjectPoolManager.SpawnObject(IngameUIController.Instance.m_healthMeterBoss.gameObject, IngameUIController.Instance.m_healthMeterBossRect).GetComponent<UIHealthMeter>();
        lifeMeter.SetBoss(this, m_curMaxHealth);
    }
}

[System.Serializable]
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