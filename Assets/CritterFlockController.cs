using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Collider))]
public class CritterFlockController : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float m_critterMoveSpeed = 0.2f;
    [SerializeField] private float m_critterRotationSpeed = 90f;

    [SerializeField] private float m_critterIdleDuration = 2f;
    [SerializeField] private float m_critterIdleChance = 0.1f;

    [SerializeField] private float m_critterEatDuration = 5f;
    [SerializeField] private float m_critterEatChance = 0.33f;

    [SerializeField] private float m_critterNewCellChance = 0.33f;

    [Header("Alerted Stats")]
    [SerializeField] private bool m_isAlerted;
    [SerializeField] private float m_critterAlertRange = 2f;
    [SerializeField] private float m_critterAlertMoveSpeed = 0.5f;
    [SerializeField] private float m_critterAlertRotationSpeed = 270f;

    [Header("Debug")]
    [SerializeField] private Renderer m_renderer;
    [SerializeField] private Color m_idleColor;
    [SerializeField] private Color m_eatingColor;
    [SerializeField] private Color m_rotatingColor;
    [SerializeField] private Color m_movingColor;
    [SerializeField] private Color m_alertColor;
    [SerializeField] private GameObject m_targetCellObj;
    [SerializeField] private GameObject m_currentCellObj;

    private Collider m_collider;
    private Material m_material;

    private Vector3 m_targetPos;

    private Vector2Int m_newPos;
    private Vector2Int m_curPos;

    private Cell m_prevCell;
    private Cell m_newCell;
    private Cell m_curCell;

    private Coroutine m_stateMachineCoroutine;
    private Coroutine m_movementCoroutine;
    private Coroutine m_rotationCoroutine;

    private CritterState m_curState = CritterState.Idle;

    public enum CritterState
    {
        Idle,
        Eating,
        ChoosingTarget,
        Moving,
        Rotating,
        Fleeing
    }


    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        GameplayManager.OnTowerBuild += TowerBuilt;
    }

    void Start()
    {
        // Get components to edit later
        m_material = m_renderer.material;
        m_collider = GetComponent<Collider>();

        // Assign random starting look direction
        float randomYRotation = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, randomYRotation, 0f);
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        GameplayManager.OnTowerBuild -= TowerBuilt;
    }

    private void TowerBuilt(TowerData towerData, GameObject towerObject)
    {
        // Is it within the flee range?
        float towerDistance = Vector3.Distance(towerObject.transform.position, new Vector3(m_curPos.x, 0, m_curPos.y));
        Debug.Log($"Critter Tower Built -- Distance from Critter: {towerDistance}");
        if (towerDistance > m_critterAlertRange) return;

        Vector3 directionToTower = transform.position - towerObject.transform.position;
        FleePosition(directionToTower);
    }

    private void FleePosition(Vector3 directionToTower)
    {
        Vector2Int preferredFleeDirection;
        Vector2Int secondaryFleeDirection;
        Cell fleeCell = m_curCell;
        float absX = Mathf.Abs(directionToTower.x);
        float absY = Mathf.Abs(directionToTower.z);

        if (absX > absY)
        {
            preferredFleeDirection = new Vector2Int((directionToTower.x > 0) ? 1 : -1, 0);
            secondaryFleeDirection = new Vector2Int(0, (directionToTower.y > 0) ? 1 : -1);
        }
        else
        {
            preferredFleeDirection = new Vector2Int(0, (directionToTower.z > 0) ? 1 : -1);
            secondaryFleeDirection = new Vector2Int((directionToTower.z > 0) ? 1 : -1, 0);
        }

        Cell preferredFleeCell = Util.GetCellFromPos(preferredFleeDirection + m_curCell.m_cellPos);
        Cell secondaryFleeCell = Util.GetCellFromPos(secondaryFleeDirection + m_curCell.m_cellPos);

        List<Cell> emptyNeighbors = Util.GetPathableAdjacentEmptyCells(m_curCell);

        if (emptyNeighbors.Count != 0)
        {
            if (emptyNeighbors.Contains(preferredFleeCell))
            {
                fleeCell = preferredFleeCell;
            }
            else if (emptyNeighbors.Contains(secondaryFleeCell))
            {
                fleeCell = secondaryFleeCell;
            }
            else // Fall back to a random choice.
            {
                fleeCell = emptyNeighbors[Random.Range(0, emptyNeighbors.Count)];
            }
        }

        m_isAlerted = true;
        m_targetPos = GetSpotInCell(fleeCell);
        InterruptStateMachine(CritterState.Rotating);
    }


    private void InterruptStateMachine(CritterState desiredState)
    {
        if (m_stateMachineCoroutine != null)
        {
            StopCoroutine(m_stateMachineCoroutine); 
        }
        
        if (m_movementCoroutine != null)
        {
            StopCoroutine(m_movementCoroutine); 
        }
        
        if (m_rotationCoroutine != null)
        {
            StopCoroutine(m_rotationCoroutine); 
        }

        m_curState = desiredState;
        m_stateMachineCoroutine = StartCoroutine(CritterStateMachine());
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            m_curCell = Util.GetCellFrom3DPos(transform.position);
            m_curPos = Util.GetVector2IntFrom3DPos(transform.position);
            m_prevCell = m_curCell;
            m_curCell.UpdateCritterCount(1, gameObject.name);
            m_stateMachineCoroutine = StartCoroutine(CritterStateMachine());
        }
    }

    private IEnumerator CritterStateMachine()
    {
        while (true)
        {
            switch (m_curState)
            {
                case CritterState.Idle:
                    m_material.color = m_idleColor;
                    yield return new WaitForSeconds(GetRandomDuration(m_critterIdleDuration));
                    ChooseNextAction();
                    break;
                case CritterState.Eating:
                    m_material.color = m_eatingColor;
                    yield return new WaitForSeconds(GetRandomDuration(m_critterEatDuration));
                    ChooseNextAction();
                    break;
                case CritterState.ChoosingTarget:
                    ChooseNewTarget();
                    break;
                case CritterState.Moving:
                    m_material.color = m_isAlerted ? m_alertColor : m_movingColor;
                    yield return m_movementCoroutine = StartCoroutine(MoveToTarget());
                    m_isAlerted = false;
                    m_curState = CritterState.Eating;
                    break;
                case CritterState.Rotating:
                    m_material.color = m_rotatingColor;
                    yield return m_rotationCoroutine = StartCoroutine(RotateToTarget());
                    m_curState = CritterState.Moving;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private Vector3 m_targetDirection;
    private Vector3 m_blendedDirection;
    private float m_movementStoppingDistance = 0.1f;
    private float m_baseStoppingDistance = 0.1f;

    private IEnumerator MoveToTarget()
    {
        float moveSpeed = m_isAlerted ? m_critterAlertMoveSpeed : m_critterMoveSpeed;

        while (Vector3.Distance(transform.position, m_targetPos) > m_movementStoppingDistance)
        {
            m_newPos = Util.GetVector2IntFrom3DPos(transform.position);

            if (m_newPos != m_curPos)
            {
                m_newCell = Util.GetCellFromPos(m_newPos);

                //Remove from the cell we're leaving.
                m_curCell.UpdateCritterCount(-1, gameObject.name);

                m_prevCell = m_curCell;
                m_curCell = m_newCell;
                m_curPos = m_newPos;

                //Assign self to cell we're entering.
                m_curCell.UpdateCritterCount(1, gameObject.name);
            }

            m_targetDirection = (m_targetPos - transform.position).normalized;
            m_blendedDirection = (m_targetDirection + m_avoidanceDirection).normalized;
            transform.position += m_blendedDirection * (moveSpeed * Time.deltaTime);

            if (m_blendedDirection.sqrMagnitude > 0.01f)
            {
                transform.forward = Vector3.Lerp(transform.forward, m_blendedDirection, 0.1f);
            }

            yield return null;
        }
    }

    private Vector3 m_avoidanceDirection;
    private int m_critterAvoidanceCount;
    private float m_avoidanceDecayRate = 2.0f;
    private List<Collider> m_blockingCritters = new List<Collider>();

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Critter") && other.gameObject != gameObject)
        {
            m_critterAvoidanceCount++;
            //Debug.Log($"{name} Trigger Enter ++ Critter Count {m_critterAvoidanceCount}");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Critter") && other.gameObject != gameObject)
        {
            Vector3 directionToOther = (transform.position - other.transform.position).normalized;

            if (other.bounds.Contains(m_targetPos) && !m_blockingCritters.Contains(other))
            {
                //Debug.Log($"{name} Trigger Stay: Collider occupying our targetPos, expanding Stopping Distance. Collider: {other.name}.");
                m_movementStoppingDistance = 1f;
                m_blockingCritters.Add(other);
            }

            // Avoidance strength smoothly decreases as they move apart
            float distance = directionToOther.magnitude;
            float avoidanceStrength = Mathf.Clamp(2.0f / (distance * distance), 0, 1);

            // Apply a smoothing effect to avoid sudden jumps
            m_avoidanceDirection = Vector3.Lerp(m_avoidanceDirection, directionToOther.normalized * avoidanceStrength, 0.2f);

            //Debug.Log($"{name} Trigger Stay: Avoidance Direction: {m_avoidanceDirection} at Strength: {avoidanceStrength}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Critter") && other.gameObject != gameObject)
        {
            m_critterAvoidanceCount = Mathf.Max(0, m_critterAvoidanceCount - 1);
            //Debug.Log($"{name} Trigger Exit -- Critter Count {m_critterAvoidanceCount}");

            if (m_blockingCritters.Contains(other))
            {
                m_blockingCritters.Remove(other);
                if (m_blockingCritters.Count == 0 && m_movementStoppingDistance != m_baseStoppingDistance)
                {
                    m_movementStoppingDistance = m_baseStoppingDistance;
                }
                //Debug.Log($"{name} Trigger Exit -- Collider leaving our targetPos, resetting Stopping Distance. Collider: {other.name}.");
            }
        }
    }

    void Update()
    {
        if (m_critterAvoidanceCount == 0 && m_avoidanceDirection != Vector3.zero)
        {
            m_avoidanceDirection = Vector3.Lerp(m_avoidanceDirection, Vector3.zero, m_avoidanceDecayRate * Time.deltaTime);

            if (m_avoidanceDirection.magnitude < 0.01f)
            {
                m_avoidanceDirection = Vector3.zero;
            }
        }
    }

    private IEnumerator RotateToTarget()
    {
        Quaternion targetRotation = Quaternion.LookRotation(m_targetPos - transform.position);
        float rotationSpeed = m_isAlerted ? m_critterAlertRotationSpeed : m_critterRotationSpeed;
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void ChooseNextAction()
    {
        float randomRoll = Random.Range(0f, 1f);

        if (randomRoll < m_critterIdleChance)
        {
            m_curState = CritterState.Idle;
        }
        else if (randomRoll < m_critterIdleChance + m_critterEatChance)
        {
            m_curState = CritterState.Eating;
        }
        else
        {
            m_curState = CritterState.ChoosingTarget;
        }
    }

    private void ChooseNewTarget()
    {
        float randomRoll = Random.Range(0f, 1f);

        if (randomRoll < m_critterNewCellChance)
        {
            PickNewCell();
        }
        else
        {
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        m_targetPos = GetSpotInCell(m_curCell);
        Debug.Log($"Pick New Target -- CurCell: {m_curCell.m_cellPos}, Target Pos: {m_targetPos}");
        m_curState = CritterState.Rotating;
    }

    private void PickNewCell()
    {
        List<Cell> emptyNeighbors = Util.GetPathableAdjacentEmptyCells(m_curCell);

        if (emptyNeighbors.Count == 0)
        {
            // No valid neighbors, we stay in the current cell.
            Debug.Log($"Pick New Cell -- No empty or pathable neighbors found.");
            m_targetPos = GetSpotInCell(m_curCell);
        }

        if (emptyNeighbors.Count >= 2)
        {
            // Deprioritize our previous cell only if we have other options.
            Debug.Log($"Pick New Cell -- More than two valid neighbors found, removing Prev Cell: {m_prevCell.m_cellPos}.");
            if (emptyNeighbors.Contains(m_prevCell))
            {
                emptyNeighbors.Remove(m_prevCell);
            }
        }

        int index = Random.Range(0, emptyNeighbors.Count);

        m_targetPos = GetSpotInCell(emptyNeighbors[index]);
        Debug.Log($"Pick New Cell -- Cell: {emptyNeighbors[index].m_cellPos}, Target Pos: {m_targetPos}");
        m_curState = CritterState.Rotating;
    }

    private Vector3 GetSpotInCell(Cell cell)
    {
        float xOffset = Random.Range(-.45f, .45f);
        float zOffset = Random.Range(-.45f, .45f);
        Vector3 newPos = new Vector3(cell.m_cellPos.x + xOffset, 0, cell.m_cellPos.y + zOffset);
        return newPos;
    }

    private float GetRandomDuration(float value)
    {
        float min = value / 2; // Given 10, min is 5
        float max = value + min; // Given 10, max is 15
        return Random.Range(min, max);
    }
}