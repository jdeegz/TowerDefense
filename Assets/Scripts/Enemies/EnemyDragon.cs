using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using Random = UnityEngine.Random;

public class EnemyDragon : EnemyController
{
    public GameObject m_muzzleObj;
    public GameObject m_colliderObj;
    public int m_castlePathingRadius = 8;
    public List<GameObject> m_dragonBoneObjs;
    public List<GameObject> m_dragonBoneVFXTransforms;
    public List<GameObject> m_dragonBones;
    public float m_dragonBoneSpacing = .5f;
    public VisualEffect m_dragonBreathVFX;
    [SerializeField]private AudioSource m_secondaryAudioSource;

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
    private Queue<BoneTransform> m_headTransformHistory = new Queue<BoneTransform>();
    private float m_cumulativeMoveSpeed;
    private float m_cumulativeLookSpeed;
    private float m_moveSpeed;
    private float m_attackingSpeedModifier = 1;
    private float m_rampingLookSpeed = 0;

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

        if (m_dragonBones.Count > 1) // Reset the list of bones to just the head bone (head bone is pre-assigned in the prefab)
        {
            m_dragonBones.RemoveRange(1, m_dragonBones.Count - 1);
        }

        m_headTransformHistory = new Queue<BoneTransform>();


        UpdateBossState(BossState.Idle);

        SetBossPathPoints();

        SetSpawnPosition();

        InitiateBossMovement();

        SetSequenceController(GameplayManager.Instance.GetActiveBossController());
        for (var i = 0; i < m_dragonBoneObjs.Count; i++)
        {
            var obj = m_dragonBoneObjs[i];
            GameObject newBone = ObjectPoolManager.SpawnObject(obj, m_enemyModelRoot.transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.Enemy);
            m_dragonBones.Add(newBone);
            EnemySwarmMember swarmMemberController = newBone.GetComponent<EnemySwarmMember>();
            swarmMemberController.SetEnemyData(m_enemyData);
            swarmMemberController.SetMother(this);
            swarmMemberController.m_returnToPool = true;
        }

        for (int x = 0; x < m_dragonBones.Count; x++)
        {
            m_dragonBoneVFXTransforms[x].transform.SetParent(m_dragonBones[x].transform);
            m_dragonBoneVFXTransforms[x].transform.localPosition = Vector3.zero;
        }


        m_colliderObj.SetActive(false);
        m_dragonBreathVFX.Stop();
    }

    public override void OnEnemyDestroyed(Vector3 pos)
    {
        if (m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }

        foreach (GameObject obj in m_dragonBoneVFXTransforms)
        {
            obj.transform.SetParent(gameObject.transform);
            obj.transform.position = Vector3.zero;
        }

        base.OnEnemyDestroyed(pos);
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

        Vector3 spawnPos = new Vector3(castleXPosition + ((xDelta + spawnOffset) * xMultiplier), 0, castleZPosition + ((zDelta + spawnOffset) * zMultiplier));
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

        /*//We now need to find the closts point in the list to move to.
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
        }*/

        //We now need to find the closts point in the list to move to.
        Vector3 nextGoalPos = new Vector3();
        int pathingStartIndex = 0;
        float savedDistance = -1;

        for (int i = 0; i < newList.Count; ++i)
        {
            Vector3 pointPos = new Vector3(newList[i].x, 0, newList[i].y);
            float distance = Vector3.Distance(curPos, pointPos);
            if (distance >= savedDistance)
            {
                savedDistance = distance;
                pathingStartIndex = i;
            }
        }

        nextGoalPos = GetNextGoalPosition(newList, pathingStartIndex);

        return (newList, nextGoalPos, pathingStartIndex);
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
                goalIndex = 3;
                break;
            case SpawnDirection.East:
                goalIndex = 2;
                break;
            case SpawnDirection.South:
                goalIndex = 1;
                break;
            case SpawnDirection.West:
                goalIndex = 0;
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
        //
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
                break;
            case BossState.Death:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator Attack()
    {
        while (m_attackingSpeedModifier > 0)
        {
            m_attackingSpeedModifier -= 1 * Time.deltaTime;
            yield return null;
        }

        m_attackingSpeedModifier = 0;
        RequestPlayAudio(((BossEnemyData)m_enemyData).m_audioAnticFireballClip);
        yield return new WaitForSeconds(((BossEnemyData)m_enemyData).m_attackDelay);
        HandleAttack();
        yield return new WaitForSeconds(((BossEnemyData)m_enemyData).m_attackCooldown);
        while (m_attackingSpeedModifier < 1)
        {
            m_attackingSpeedModifier += 1 * Time.deltaTime;
            yield return null;
        }

        m_attackingSpeedModifier = 1;

        UpdateMoveDestination();
        UpdateBossState(BossState.MoveToDestination);

        m_curCoroutine = null;
    }

    private IEnumerator UpdateStateAfterDelay(float i, BossState newState)
    {
        yield return new WaitForSeconds(i);
        UpdateBossState(newState);
    }

    private void HandleAttack()
    {
        RequestPlayAudio(((BossEnemyData)m_enemyData).m_audioShootFireballClip);
        ObjectPoolManager.SpawnObject(((BossEnemyData)m_enemyData).m_projectileObj, m_muzzleObj.transform.position, m_muzzleObj.transform.rotation, null, ObjectPoolManager.PoolType.Projectile);
    }

    private float m_previousAngleToTarget;

    public void Update()
    {
        if (!m_isActive) return;

        if (m_curHealth > 0) UpdateStatusEffects();

        m_moveSpeed = m_baseMoveSpeed * m_lastSpeedModifierFaster * Mathf.Sqrt(m_lastSpeedModifierSlower) * m_attackingSpeedModifier;
        m_cumulativeMoveSpeed = m_moveSpeed * Time.deltaTime;
        m_cumulativeLookSpeed = m_baseLookSpeed * m_attackingSpeedModifier * Time.deltaTime;

        switch (m_bossState)
        {
            case BossState.Idle:
                break;
            case BossState.RotateToDestination:
                break;
            case BossState.MoveToDestination:
                if (m_moveCounter % ((BossEnemyData)m_enemyData).m_strafeAttackRate != 0) HandleCone();

                // Move forward
                transform.position += transform.forward * m_cumulativeMoveSpeed;

                // Set the distance travelled
                m_distanceTravelled = m_moveDistance - Vector3.Distance(transform.position, m_curGoalPos);

                // Rotation
                Quaternion targetRotation = Quaternion.LookRotation((m_curGoalPos - transform.position).normalized);
                float lookSpeed = m_cumulativeLookSpeed + m_rampingLookSpeed; // Adding a ramping look speed to avoid circling target.
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, lookSpeed);

                if (transform.rotation != targetRotation)
                {
                    m_rampingLookSpeed += Time.deltaTime;
                }

                // Check if we're at our destination
                if (Vector3.Distance(transform.position, m_curGoalPos) <= 0.05f)
                {
                    //Get the next destination, even if we're attacking right now.
                    m_rampingLookSpeed = 0;
                    UpdateMoveDestination();
                }

                break;
            case BossState.RotateToTarget:
                // Move forward
                transform.position += transform.forward * m_cumulativeMoveSpeed;

                // Rotation
                targetRotation = Quaternion.LookRotation((m_curGoalPos - transform.position).normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_cumulativeLookSpeed);

                // Calculate the rotation to face the target
                Quaternion rotationToCastle = Quaternion.LookRotation(m_castlePos - transform.position);
                float rotationToCastledotProduct = Mathf.Abs(Quaternion.Dot(transform.rotation, rotationToCastle));

                // Check if we're at our destination
                if (rotationToCastledotProduct >= m_rotationThreadhold && m_curCoroutine == null)
                {
                    m_curCoroutine = StartCoroutine(Attack());
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

    void FixedUpdate()
    {
        HandleBonePositioning();
    }

    private Vector3 m_previousHeadPosition;
    private float m_movementThreshold = 0.001f;

    void HandleBonePositioning()
    {
        // Check if the head has moved more than the threshold distance from the previous frame
        Vector3 currentHeadPosition = m_dragonBones[0].transform.position;

        if (Vector3.Distance(currentHeadPosition, m_previousHeadPosition) > m_movementThreshold)
        {
            // Add our position this frame
            BoneTransform m_curBoneTransform = new BoneTransform(m_dragonBones[0].transform.position, m_dragonBones[0].transform.rotation, m_dragonBones[0].transform.localScale);
            m_headTransformHistory.Enqueue(m_curBoneTransform);

            // Remove the oldest item if the queue is too long.
            if (m_headTransformHistory.Count > m_dragonBones.Count * 80)
            {
                m_headTransformHistory.Dequeue();
            }

            // Update the previous head position for the next frame
            m_previousHeadPosition = currentHeadPosition;

            // Assign the position of each bone based on spacing
            for (int i = 1; i < m_dragonBones.Count; i++)
            {
                float transformHistoryFloat = Mathf.Clamp(m_headTransformHistory.Count - i * m_dragonBoneSpacing / (m_baseMoveSpeed * Time.fixedUnscaledDeltaTime), 0, m_headTransformHistory.Count - 1);
                int transformHistoryIndex = Mathf.FloorToInt(transformHistoryFloat);

                BoneTransform targetTransform = m_headTransformHistory.ElementAt(transformHistoryIndex);

                // Set transform of bone
                m_dragonBones[i].transform.position = targetTransform.position;
                m_dragonBones[i].transform.rotation = targetTransform.rotation;
                m_dragonBones[i].transform.localScale = targetTransform.scale;

                //If this ends up needing polish, try removing the assignment of p/r/s, and replacing it with a lerp-to-transform and use m_moveSpeed as the lerp factor.
            }
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

        SetConeDistances();

        //Do we need to Rotate to a new Destination, or Rotate to attack the castle?
        if (m_moveCounter % ((BossEnemyData)m_enemyData).m_castleAttackRate == 0)
        {
            UpdateBossState(BossState.RotateToTarget);
        }
        else
        {
            RequestPlayAudio(((BossEnemyData)m_enemyData).m_audioMovementClips, m_audioSource);
            UpdateBossState(BossState.MoveToDestination);
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

    private float m_curDissolve = 1;

    void HandleCone()
    {
        //If the cone is disabled, and we're after start, before end, turn on cone.
        if (!m_colliderObj.activeSelf && m_distanceTravelled > m_coneStartDelay && m_distanceTravelled < m_coneEndBuffer)
        {
            m_colliderObj.SetActive(true);
            RequestPlayAudio(((BossEnemyData)m_enemyData).m_audioStrafeIgniteClip);
            RequestPlayAudioLoop(((BossEnemyData)m_enemyData).m_audioStrafeLoop, m_secondaryAudioSource);
            m_dragonBreathVFX.Play();
            DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 0f, 1f)
                .OnUpdate(() => m_dragonBreathVFX.SetFloat("Dissolve", m_curDissolve));
        }
        else if (m_colliderObj.activeSelf && (m_distanceTravelled < m_coneStartDelay || m_distanceTravelled > m_coneEndBuffer))
        {
            m_colliderObj.SetActive(false);
            m_dragonBreathVFX.Stop();
            RequestStopAudioLoop(m_secondaryAudioSource);
            DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 1f, 1f)
                .OnUpdate(() => m_dragonBreathVFX.SetFloat("Dissolve", m_curDissolve));
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

[System.Serializable]
public struct BoneTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public BoneTransform(Vector3 pos, Quaternion rot, Vector3 scl)
    {
        position = pos;
        rotation = rot;
        scale = scl;
    }
}

public class BossObeliskObjComparer : IComparer<BossObeliskObj>
{
    public int Compare(BossObeliskObj x, BossObeliskObj y)
    {
        // Compare based on the YourVariable property
        return y.m_obeliskProgressPercent.CompareTo(x.m_obeliskProgressPercent);
    }
}