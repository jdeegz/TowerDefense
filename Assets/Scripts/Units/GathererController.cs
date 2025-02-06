using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class GathererController : MonoBehaviour
{
    public GathererData m_gathererData;
    public Vector3 m_targetObjPosition;

    [SerializeField] private GameObject m_resourceAnchor;
    [SerializeField] private GameObject m_boostedVFX;
    [SerializeField] private GameObject m_gathererBlockedIndicator;
    [SerializeField] private GameObject m_gathererIdleIndicator;
    [SerializeField] private GathererTask m_gathererTask;
    [SerializeField] private Animator m_animator;
    [SerializeField] private AudioSource m_audioSource;

    public enum GathererTask
    {
        NotInitialized,
        TravelingToHarvest,
        TravelingToDeposit,
        TravelingToRuin,
        TravelingToIdle,
        Harvesting,
        Storing,
        Idling,
    }

    private Cell m_curGoalCell;
    private Vector3 m_curGoalCellPos;

    private Cell CurrentGoalCell
    {
        get { return m_curGoalCell; }
        set
        {
            m_curGoalCell = value;
            if (m_curGoalCell != null)
            {
                Vector3 newGoalPosition = new Vector3(m_curGoalCell.m_cellPos.x, 0, m_curGoalCell.m_cellPos.y);
                m_curGoalCellPos = newGoalPosition;

                if (m_targetObjPosition != newGoalPosition)
                {
                    m_targetObjPosition = newGoalPosition;
                }

                //Debug.Log($"New Current Goal Cell assigned. Now finding path to goal cell. {m_curGoalCell} cur {m_curCell}.");

                GathererPath = AStar.FindPathToGoal(m_curGoalCell, m_curCell);
            }
        }
    }

    private int m_gathererLevel;

    public int GathererLevel
    {
        get { return m_gathererLevel; }
        set
        {
            if (m_gathererLevel != value) // Only trigger the event if the value actually changes
            {
                m_gathererLevel = value;
                GathererLevelChange?.Invoke(m_gathererLevel);
            }
        }
    }

    private List<Vector2Int> m_gathererPath;

    public List<Vector2Int> GathererPath
    {
        get { return m_gathererPath; }
        set
        {
            m_gathererPath = value;

            Debug.Log($"GathererPath is null: {m_gathererPath == null}, it is size: {m_gathererPath.Count}");
            if (m_gathererPath != null)
            {
                if (m_gathererPath.Count > 1)
                {
                    m_nextCellInPath = Util.GetCellFromPos(GathererPath[1]);
                    m_nextCellPosition = new Vector3(m_nextCellInPath.m_cellPos.x, 0, m_nextCellInPath.m_cellPos.y);
                }

                if (m_gathererPath.Count == 1)
                {
                    m_nextCellInPath = Util.GetCellFromPos(GathererPath[0]);
                    m_nextCellPosition = new Vector3(m_nextCellInPath.m_cellPos.x, 0, m_nextCellInPath.m_cellPos.y);
                }

                CheckPathToGoal();

                OnGathererPathChanged?.Invoke(m_gathererPath);
            }
        }
    }

    // RESOURCE NODE
    private ResourceNode m_curHarvestNode;

    private ResourceNode CurrentHarvestNode
    {
        get { return m_curHarvestNode; }
        set
        {
            if (value != CurrentHarvestNode)
            {
                if (m_curHarvestNode)
                {
                    m_lastHarvestNodeCell = Util.GetCellFrom3DPos(m_curHarvestNode.transform.position);
                }
                else
                {
                    m_lastHarvestNodeCell = Util.GetCellFrom3DPos(value.transform.position);
                }

                m_curHarvestNode = value;

                //Debug.Log($"Gatherer {m_gathererData.m_gathererName}'s current harvest node is now {m_curHarvestNode}.");

                UpdateHarvestNodeIndicator();

                if (m_curHarvestNode) m_curHarvestNode.OnResourceNodeDepletion += OnNodeDepleted;
            }
        }
    }

    private Cell m_lastHarvestNodeCell;
    private GameObject m_harvestNodeIndicatorObj;
    private GameObject m_curHarvestNodeIndicator;

    // RESOURCE NODE QUEUE
    private List<GameObject> m_curNodeQueueIndicators;
    private List<ResourceNode> m_resourceNodeHarvestQueue;
    private GameObject m_queuedHarvestNodeIndicatorObj;

    private List<ResourceNode> ResourceNodeHarvestQueue
    {
        get { return m_resourceNodeHarvestQueue; }
        set { m_resourceNodeHarvestQueue = value; }
    }

    private List<ResourceNode> m_resourceNodesToRemoveFromHarvestQueue;

    // RUIN
    private Ruin m_curRuin;

    // DEPOSIT & IDLE
    private List<Vector2Int> m_depositLocations;
    private Cell m_idleCell;

    // MOVEMENT
    private bool m_isMoving;

    private bool IsMoving
    {
        get { return m_isMoving; }
        set
        {
            if (value != m_isMoving)
            {
                m_isMoving = value;
                ToggleIdleIndicator();
                ToggleBlockedIndicator();
            }
        }
    }

    private Vector2Int m_curPos;
    private Cell m_curCell;
    private float m_lookSpeed = 270f;

    private Vector3 m_nextCellPosition;
    private Cell m_nextCellInPath;

    private Vector3 m_moveDirection;
    private Quaternion m_previousRotation;

    // STATUS
    private bool m_isSelected;
    private bool m_isBlockedFromGoal;
    private int m_resourceCarried;

    private int ResourceCarried
    {
        get { return m_resourceCarried; }
        set
        {
            if (value != m_resourceCarried)
            {
                m_resourceCarried = value;
                ToggleResourceDisplay(m_resourceCarried);
            }
        }
    }

    // ATTRIBUTES
    private float m_harvestDuration;
    private int m_carryCapacity;
    private float m_storingDuration;

    private Coroutine m_curCoroutine;
    private int m_debugIndex;


    public event Action<List<Vector2Int>> OnGathererPathChanged;
    public event Action<int> GathererLevelChange;

    private void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        GameplayManager.OnGameObjectSelected += GathererSelected;
        GameplayManager.OnCommandRequested += CommandRequested;

        m_harvestDuration = m_gathererData.m_harvestDuration;
        m_carryCapacity = m_gathererData.m_carryCapacity;
        m_storingDuration = m_gathererData.m_storingDuration;
        //m_gathererRenderer.material.SetColor("_SkinTint", m_gathererData.m_gathererModelColor);

        m_harvestNodeIndicatorObj = m_gathererData.m_harvestNodeIndicatorObj;
        m_queuedHarvestNodeIndicatorObj = m_gathererData.m_queuedHarvestNodeIndicatorObj;
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
        GameplayManager.OnGameObjectSelected -= GathererSelected;
        GameplayManager.OnCommandRequested -= CommandRequested;


        GameplayManager.Instance.RemoveGathererFromList(this, m_gathererData.m_type);

        if (ResourceNodeHarvestQueue == null) return;

        foreach (ResourceNode node in ResourceNodeHarvestQueue)
        {
            node.OnResourceNodeDepletion -= OnQueuedNodeDepleted;
        }
    }

    private void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            m_curPos = new Vector2Int((int)Mathf.Floor(transform.position.x + 0.5f), (int)Mathf.Floor(transform.position.z + 0.5f));
            m_curCell = Util.GetCellFromPos(m_curPos);
            m_curCell.UpdateActorCount(1, gameObject.name);
            UpdateTask(GathererTask.Idling);
            m_idleCell = m_curCell;

            ToggleIdleIndicator();
        }

        if (newState == GameplayManager.GameplayState.CreatePaths) // Moved this code out of Place Obstacles, because that is also when Obelisks are added GameplayManager.
        {
            m_depositLocations = new List<Vector2Int>();

            foreach (Obelisk obelisk in GameplayManager.Instance.m_obelisksInMission)
            {
                m_depositLocations.Add(Util.GetVector2IntFrom3DPos(obelisk.gameObject.transform.position));
            }

            foreach (GameObject entrancePoint in GameplayManager.Instance.m_castleController.m_castleEntrancePoints)
            {
                m_depositLocations.Add(Util.GetVector2IntFrom3DPos(entrancePoint.transform.position));
            }
        }
    }

    public void RequestIncrementGathererLevel(int i)
    {
        IngameUIController.Instance.SpawnLevelUpAlert(gameObject, transform.position);
        GathererLevel += i;
        RequestPlayAudio(m_gathererData.m_levelUpClip);
    }

    void Start()
    {
        switch (m_gathererData.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                GameplayManager.Instance.AddGathererToList(this, m_gathererData.m_type);
                GathererLevel += 1;
                break;
            case ResourceManager.ResourceType.Stone:
                GameplayManager.Instance.AddGathererToList(this, m_gathererData.m_type);
                GathererLevel += 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // ResourceManager.Instance.AddGathererLineRenderer(this);
        m_resourceNodesToRemoveFromHarvestQueue = new List<ResourceNode>();
        m_curNodeQueueIndicators = new List<GameObject>();
    }


    void Update()
    {
        UpdateStatusEffects();

        HandleBoostedEffect();

        CleanUpHarvestQueue();

        FindUnblockedPath();
    }

    private void HandleBoostedEffect()
    {
        if (m_activeSpeedBoostCount > 0)
        {
            if (!m_boostedVFX.activeSelf)
            {
                m_boostedVFX.SetActive(true);
            }
        }
        else if (m_activeSpeedBoostCount == 0)
        {
            if (m_boostedVFX.activeSelf)
            {
                m_boostedVFX.SetActive(false);
            }
        }
    }

    void FixedUpdate()
    {
        // Each frame, check to see if we can enter the next cell, if not, find a new path to our goal.
        if (GathererPath != null && m_nextCellInPath != null && m_nextCellInPath.m_isOccupied)
        {
            GathererPath = AStar.FindPathToGoal(CurrentGoalCell, m_curCell);
        }

        HandleMovement();
    }

    // QUEUEING FUNCTIONS
    public void AddNodeToHarvestQueue(ResourceNode node) // Triggers from gameplay manager (Shift + Click while gatherer is selected)
    {
        // Add the node, if node already in queue, remove the node from the queue.
        if (ResourceNodeHarvestQueue == null) ResourceNodeHarvestQueue = new List<ResourceNode>();

        if (node == CurrentHarvestNode) return;

        if (ResourceNodeHarvestQueue.Contains(node))
        {
            RemoveNodeFromHarvestQueue(node);
        }
        else
        {
            node.RequestPlayAudio(m_gathererData.m_queueingClips);

            if (CurrentHarvestNode == null && m_resourceCarried == 0)
            {
                RequestedHarvest(node);
                return;
            }

            ResourceNodeHarvestQueue.Add(node);
            UpdateHarvestQueueIndicators();
            node.OnResourceNodeDepletion += OnQueuedNodeDepleted;
        }
    }

    private void CleanUpHarvestQueue()
    {
        if (m_resourceNodesToRemoveFromHarvestQueue.Count == 0) return;

        ResourceNodeHarvestQueue = ResourceNodeHarvestQueue
            .Where(node => !m_resourceNodesToRemoveFromHarvestQueue.Contains(node))
            .ToList();

        UpdateHarvestQueueIndicators();
        m_resourceNodesToRemoveFromHarvestQueue.Clear();
    }

    private void RemoveNodeFromHarvestQueue(ResourceNode node)
    {
        m_resourceNodesToRemoveFromHarvestQueue.Add(node);
        node.OnResourceNodeDepletion -= OnQueuedNodeDepleted;
    }

    private void OnQueuedNodeDepleted(ResourceNode depletedNode)
    {
        //Debug.Log($"{m_gathererData.m_gathererName}'s QUEUED node DEPLETED.");

        m_resourceNodesToRemoveFromHarvestQueue.Add(depletedNode);
        depletedNode.OnResourceNodeDepletion -= OnQueuedNodeDepleted;
    }

    private void ClearHarvestingQueue()
    {
        if (ResourceNodeHarvestQueue == null) return;

        foreach (ResourceNode node in ResourceNodeHarvestQueue)
        {
            RemoveNodeFromHarvestQueue(node);
        }
    }

    void UpdateHarvestNodeIndicator()
    {
        // Return the current one, if there is one.
        //Debug.Log($"{m_gathererData.m_gathererName} updating Harvest Node indicator; Current indicator: {m_curHarvestNodeIndicator}. Current Node: {CurrentHarvestNode}");

        if (m_curHarvestNodeIndicator)
        {
            //Debug.Log($"Disabling {m_gathererData.m_gathererName}'s Current Harvest Node Indicator.");
            GameObject oldIndicator = m_curHarvestNodeIndicator;
            m_curHarvestNodeIndicator = null;
            oldIndicator.transform.DOScale(0, 0.15f).SetEase(Ease.InBack).OnComplete(() => ObjectPoolManager.ReturnObjectToPool(oldIndicator, ObjectPoolManager.PoolType.GameObject));
        }

        // Then if we have a node, spawn a new indicator at it.
        if (CurrentHarvestNode)
        {
            //Debug.Log($"Enabling a new indicator at {m_gathererData.m_gathererName}'s Harvest Node.");
            m_curHarvestNodeIndicator = ObjectPoolManager.SpawnObject(m_harvestNodeIndicatorObj, CurrentHarvestNode.transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.GameObject);
            m_curHarvestNodeIndicator.transform.localScale = Vector3.zero;
            m_curHarvestNodeIndicator.transform.DOScale(1, .15f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    void UpdateHarvestQueueIndicators()
    {
        HashSet<Vector3> queuePositions = new HashSet<Vector3>();
        foreach (ResourceNode node in ResourceNodeHarvestQueue)
        {
            queuePositions.Add(node.transform.position);
        }

        for (int i = m_curNodeQueueIndicators.Count - 1; i >= 0; --i)
        {
            if (!queuePositions.Contains(m_curNodeQueueIndicators[i].transform.position))
            {
                GameObject oldIndicator = m_curNodeQueueIndicators[i];
                m_curNodeQueueIndicators.RemoveAt(i);
                oldIndicator.transform.DOScale(0, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                    ObjectPoolManager.ReturnObjectToPool(oldIndicator, ObjectPoolManager.PoolType.GameObject));
            }
        }

        foreach (ResourceNode node in ResourceNodeHarvestQueue)
        {
            Vector3 nodePos = node.transform.position;
            bool hasIndicator = m_curNodeQueueIndicators.Exists(indicator => indicator.transform.position == nodePos);

            if (!hasIndicator)
            {
                GameObject newIndicator = ObjectPoolManager.SpawnObject(m_queuedHarvestNodeIndicatorObj, node.transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.GameObject);
                m_curNodeQueueIndicators.Add(newIndicator);
                newIndicator.transform.localScale = Vector3.zero;
                newIndicator.transform.DOScale(1, .15f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }
    }

    private void CheckPathToGoal()
    {
        //Does the path reach the goal?
        //Debug.Log($"Checking Path to Goal.");
        Vector2Int lastPathPos = GathererPath.Last();
        int distance = Math.Max(Math.Abs(lastPathPos.x - CurrentGoalCell.m_cellPos.x), Math.Abs(lastPathPos.y - CurrentGoalCell.m_cellPos.y));
        if (distance > 1)
        {
            //Debug.Log($"The last cell {lastPathPos} is not reachable from {CurrentGoalCell}.");
            m_isBlockedFromGoal = true;
        }
        else
        {
            if (m_isBlockedFromGoal)
            {
                //If we were blocked, and now we're not. Let's move.
                //Debug.Log($"The last cell {lastPathPos} is reachable from {CurrentGoalCell}!");
                IsMoving = true;
            }

            m_isBlockedFromGoal = false;
        }
    }


    private float m_unblockSearchTimer = .5f;
    private float m_unblockSearchTimeElapsed = 0f;

    private void FindUnblockedPath()
    {
        //if (!m_isBlockedFromGoal) return;
        m_unblockSearchTimeElapsed += Time.deltaTime;
        if (m_unblockSearchTimeElapsed > m_unblockSearchTimer && m_isBlockedFromGoal)
        {
            //Debug.Log($"Trying to find an unblocked path.");
            GathererPath = AStar.FindPathToGoal(CurrentGoalCell, m_curCell);
            m_unblockSearchTimeElapsed = 0;
        }
    }

    private Vector3 m_avoidanceDirection;

    void OnTriggerStay(Collider other)
    {
        // Ensure we're only detecting other gatherers
        if (other.CompareTag("Gatherer") && other.gameObject != this.gameObject)

        {
            //Debug.Log($"avoiding {other.gameObject.name}");
            // Logic to adjust movement for avoidance
            Vector3 directionToOther = transform.position - other.transform.position;
            m_avoidanceDirection = directionToOther.normalized / directionToOther.magnitude;
        }
    }

    private void HandleMovement()
    {
        if (!IsMoving) return;

        // Get the cell-distance between our current cell and our goal cell.
        float remainingCellDistance = Math.Max(Math.Abs(m_curPos.x - CurrentGoalCell.m_cellPos.x), Math.Abs(m_curPos.y - CurrentGoalCell.m_cellPos.y));

        // Get the float distance of our current position and the halfway point.
        float distanceToGoalPoint = Vector3.Distance(transform.position, m_curGoalCellPos);
        //float distanceToNextCellCenter = Vector3.Distance(transform.position, m_nextCellPosition);
        float distanceBetweenCells = Vector3.Distance(m_nextCellPosition, m_curGoalCellPos);

        if (GathererPath == null || m_curPos == GathererPath.Last()) // We've arrived in the last cell of the path, are we next to the goal cell?
        {
            if (!m_isBlockedFromGoal && remainingCellDistance <= 1) // We're adjacent to the goal cell.
            {
                float stoppingDistance = Vector2Int.Distance(m_curCell.m_cellPos, CurrentGoalCell.m_cellPos) / 2 + 0.15f;
                //Debug.Log($"We're in a cell adjacent the current goal. Distance to border: {distanceToGoalPoint}, Stopping Distance: {stoppingDistance}");
                if (distanceToGoalPoint <= stoppingDistance && IsMoving) // We're on the border between the two cells.
                {
                    //Debug.Log($"We've reached the border between the two cells.");
                    DestinationReached();
                    //GathererPath = null;
                    return;
                }
            }
            else // We're at the last cell, but it's not adjacent, meaning we cannot access the Goal Cell to do our duty. Chill in the center of the cell
            {
                //Debug.Log($"We're in a cell that is NOT adjacent the current goal. Distance Between Cells: {distanceBetweenCells}, Distance from Goal: {distanceToGoalPoint}");
                if (distanceToGoalPoint <= distanceBetweenCells && IsMoving)
                {
                    //Debug.Log($"We've reached the center point of our last cell.");
                    SetAnimatorTrigger("Idle");
                    IsMoving = false;
                    return;
                }
            }
        }

        //Check to see if we're in a new cell.
        Vector2Int newPos = Util.GetVector2IntFrom3DPos(transform.position);
        if (newPos != m_curPos)
        {
            // Remove self from current cell.
            if (m_curCell != null)
            {
                m_curCell.UpdateActorCount(-1, gameObject.name);
            }

            // Assign new position
            m_curPos = newPos;

            // Get new cell from new position.
            m_curCell = Util.GetCellFromPos(m_curPos);

            // Assign self to cell.
            m_curCell.UpdateActorCount(1, gameObject.name);

            // Update my path towards my goal.
            GathererPath = AStar.FindPathToGoal(CurrentGoalCell, m_curCell);
        }

        if (GathererPath == null || m_curPos == GathererPath.Last()) // If we have no path, or are at the last cell, AND we're not blocked, keep moving.
        {
            Vector2 stoppingPoint2d = Vector2.Lerp(m_curCell.m_cellPos, CurrentGoalCell.m_cellPos, 0.45f);
            Vector3 stoppingPoint = new Vector3(stoppingPoint2d.x, 0, stoppingPoint2d.y);
            m_moveDirection = (stoppingPoint - transform.position).normalized;
        }
        else
        {
            m_moveDirection = (m_nextCellPosition - transform.position).normalized;
        }


        // Add avoidance direction to movement
        float avoidanceStrength = 0.5f;
        Vector3 finalMovementDirection = m_moveDirection + m_avoidanceDirection * avoidanceStrength;
        finalMovementDirection = finalMovementDirection.normalized;

        // Calculate final movement with speed and move the gatherer
        float cumulativeMoveSpeed = 1f * m_totalSpeedBoost * Time.deltaTime;
        transform.position += finalMovementDirection * cumulativeMoveSpeed;

        // Check if the avoidance direction is significant enough to influence look direction
        if (m_avoidanceDirection.magnitude < 0.5f) // Adjust threshold as needed
        {
            // Only change look direction if avoidance is minimal
            float cumulativeLookSpeed = m_lookSpeed * m_totalSpeedBoost * Time.deltaTime;
            Quaternion targetRotation = Quaternion.LookRotation(m_moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, cumulativeLookSpeed);
        }

        m_avoidanceDirection = Vector3.zero;
    }

    private void DestinationReached()
    {
        switch (m_gathererTask)
        {
            case GathererTask.TravelingToHarvest:
                UpdateTask(GathererTask.Harvesting);
                break;
            case GathererTask.TravelingToDeposit:
                UpdateTask(GathererTask.Storing);
                break;
            case GathererTask.TravelingToRuin:
                ClearHarvestVars();

                m_curRuin.GathererArrivedAtRuin(this);

                // If we're carrying resources, resume delivering them.
                if (ResourceCarried > 0)
                {
                    SetAnimatorTrigger("Idle");
                    IsMoving = false;
                    PathToDepositCell();
                    return;
                }

                // If we're not carrying resources, go idle.
                m_curCoroutine = StartCoroutine(IdleRotate());
                UpdateTask(GathererTask.Idling);

                // If we're not carrying resources, look for a node to go harvest.
                //RequestNextHarvestNode();
                break;
            case GathererTask.TravelingToIdle:
                m_curCoroutine = StartCoroutine(IdleRotate());
                UpdateTask(GathererTask.Idling);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void GathererSelected(GameObject selectedObj)
    {
        m_isSelected = selectedObj == gameObject;
        if (m_isSelected) RequestPlayAudio(m_gathererData.m_selectedGathererClips);
    }

    public void Contact()
    {
        if (m_curHarvestNode != null)
        {
            m_curHarvestNode.RequestContactRotation(transform);
            RequestPlayAudio(m_gathererData.m_harvestingClips);
            ++swingCount;
        }
    }

    public void RequestAudioLoop(AudioClip clip)
    {
        //source.Stop();
        m_audioSource.loop = true;
        m_audioSource.clip = clip;
        m_audioSource.Play();
        //Debug.Log($"playing audio loop: {clip.name}");
    }

    public void RequestPlayAudio(AudioClip clip)
    {
        //source.Stop();
        m_audioSource.PlayOneShot(clip);
        //Debug.Log($"playing clip {clip.name}");
    }

    public void RequestPlayAudio(List<AudioClip> clips)
    {
        int i = Random.Range(0, clips.Count);
        m_audioSource.PlayOneShot(clips[i]);
        //Debug.Log($"playing clip {clips[i].name}");
    }

    private void CommandRequested(GameObject requestObj, Selectable.SelectedObjectType type)
    {
        if (!m_isSelected)
        {
            return;
        }

        switch (type)
        {
            case Selectable.SelectedObjectType.ResourceWood:
                if (m_gathererData.m_type != ResourceManager.ResourceType.Wood) return;

                ResourceNode node = requestObj.GetComponent<ResourceNode>();
                node.RequestPlayAudio(m_gathererData.m_queueingClips);
                if (ResourceCarried == 0)
                {
                    RequestedHarvest(node);
                }
                else // If we're carrying a node, we're on our way to a ruin or to deposit, in both instances, we want to travel to this node next.
                {
                    ClearHarvestingQueue();

                    CurrentHarvestNode = node;
                    PathToDepositCell();
                }

                break;
            case Selectable.SelectedObjectType.Tower:
                break;
            case Selectable.SelectedObjectType.Building:
                break;
            case Selectable.SelectedObjectType.Gatherer:
                break;
            case Selectable.SelectedObjectType.Castle:
                if (ResourceCarried == 0) RequestedIdle();
                if (ResourceCarried > 0) PathToDepositCell();
                break;
            case Selectable.SelectedObjectType.Ruin:
                if (m_gathererTask != GathererTask.Storing) RequestTravelToRuin(requestObj);
                break;
            case Selectable.SelectedObjectType.Obelisk:
                if (ResourceCarried == 0) RequestedIdle();
                if (ResourceCarried > 0) PathToDepositCell();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        RequestPlayAudio(m_gathererData.m_commandRequestClips);
    }

    private void RequestedIdle()
    {
        ClearHarvestVars();

        ClearHarvestingQueue();

        RequestStopCoroutine();

        CurrentGoalCell = m_idleCell;
        UpdateTask(GathererTask.TravelingToIdle);
    }

    private void RequestTravelToRuin(GameObject requestObj)
    {
        m_curRuin = requestObj.GetComponent<Ruin>();

        InterruptHarvesting();

        RequestStopCoroutine();

        CurrentGoalCell = Util.GetCellFrom3DPos(m_curRuin.transform.position);
        UpdateTask(GathererTask.TravelingToRuin);
    }

    private void RequestedHarvest(ResourceNode node)
    {
        //If this is the node we're already harvesting ignore the command.
        if (node == CurrentHarvestNode) return;

        ClearHarvestingQueue();

        ClearHarvestVars();

        RequestStopCoroutine();

        CurrentHarvestNode = node;
        CurrentGoalCell = Util.GetCellFrom3DPos(node.transform.position);
        UpdateTask(GathererTask.TravelingToHarvest);
    }

    private Coroutine m_currentHarvestNodeCoroutine;

    public void RequestNextHarvestNode()
    {
        if (m_currentHarvestNodeCoroutine != null)
        {
            StopCoroutine(m_currentHarvestNodeCoroutine); // Stop the previous coroutine
        }

        m_currentHarvestNodeCoroutine = StartCoroutine(GetNextHarvestNode());
    }

    private IEnumerator GetNextHarvestNode()
    {
        if (CurrentHarvestNode != null)
        {
            SetNewCurrentHarvestNode(CurrentHarvestNode);
            yield break;
        }

        // Wait until there are no nodes to remove
        while (m_resourceNodesToRemoveFromHarvestQueue.Count > 0)
        {
            yield return null;
        }

        ResourceNode node = GetNextNodeFromQueue();
        if (node != null)
        {
            SetNewCurrentHarvestNode(node);
            yield break;
        }

        // If there are no nodes in the queue, search for new nodes
        node = FindNewNearbyHarvestNode();
        if (node != null)
        {
            SetNewCurrentHarvestNode(node);
        }
        else
        {
            RequestedIdle();
        }
    }

    private ResourceNode GetNextNodeFromQueue()
    {
        if (ResourceNodeHarvestQueue != null && ResourceNodeHarvestQueue.Count > 0)
        {
            ResourceNode node = ResourceNodeHarvestQueue[0];
            RemoveNodeFromHarvestQueue(node);
            return node;
        }

        return null; // No node available
    }

    private ResourceNode FindNewNearbyHarvestNode()
    {
        int searchRange = 1;
        while (searchRange <= 3 && CurrentHarvestNode == null)
        {
            Vector2Int searchFromPos = m_lastHarvestNodeCell != null ? m_lastHarvestNodeCell.m_cellPos : m_curCell.m_cellPos;
            List<ResourceNode> resourceNodes = GetHarvestNodesAtRange(searchFromPos, searchRange);
            if (resourceNodes == null || resourceNodes.Count == 0)
            {
                searchRange++;
                continue; // No hits, expand search range
            }

            Cell closestGoalCell = FindClosestPathableCell(resourceNodes);
            if (closestGoalCell != null)
            {
                foreach (ResourceNode node in resourceNodes)
                {
                    Cell nodeCell = Util.GetCellFrom3DPos(node.transform.position);
                    if (nodeCell == closestGoalCell)
                    {
                        return node; // Return the found node
                    }
                }
            }

            searchRange++;
        }

        return null;
    }

    private Cell FindClosestPathableCell(List<ResourceNode> resourceNodes)
    {
        List<Vector2Int> shortestPath = null;
        Cell closestGoalCell = null;

        foreach (ResourceNode node in resourceNodes)
        {
            Cell nodeCell = Util.GetCellFrom3DPos(node.transform.position);
            List<Vector2Int> path = AStar.FindPathToGoal(nodeCell, m_curCell);

            if (path == null)
            {
                //Debug.Log($"Path from {m_curCell.m_cellPos} to {nodeCell.m_cellPos} is NULL.");
                continue;
            }

            //Debug.Log($"Path from {m_curCell.m_cellPos} to {nodeCell.m_cellPos} is {path.Count} long.");
            //Debug.Log($"The last cell in the path is {path.Last()}.");
            float remainingCellDistance = Math.Max(Math.Abs(nodeCell.m_cellPos.x - path.Last().x), Math.Abs(nodeCell.m_cellPos.y - path.Last().y));

            if (remainingCellDistance > 1) continue;

            //Debug.Log($"The last cell in the path is {remainingCellDistance} away from the Resource Node.");

            if (shortestPath == null || path.Count < shortestPath.Count)
            {
                shortestPath = path;
                closestGoalCell = nodeCell;
            }
        }

        return closestGoalCell; // Return the closest goal cell or null if not found
    }

    private void SetNewCurrentHarvestNode(ResourceNode node)
    {
        CurrentHarvestNode = node;
        CurrentGoalCell = Util.GetCellFrom3DPos(CurrentHarvestNode.transform.position);
        UpdateTask(GathererTask.TravelingToHarvest);
    }

    void PathToDepositCell()
    {
        List<Vector2Int> shortestPath = null;
        Cell closestGoalCell = null;
        foreach (Vector2Int depositLocation in m_depositLocations)
        {
            Cell depositCell = Util.GetCellFromPos(depositLocation);
            List<Vector2Int> path = AStar.FindPathToGoal(depositCell, m_curCell);
            if (path == null) continue;

            if (shortestPath == null)
            {
                shortestPath = path;
                closestGoalCell = depositCell;
            }

            if (path.Count < shortestPath.Count)
            {
                shortestPath = path;
                closestGoalCell = depositCell;
            }
        }

        CurrentGoalCell = closestGoalCell;
        UpdateTask(GathererTask.TravelingToDeposit);
    }

    private List<String> m_triggerNames = new List<string>()
    {
        "Run", "Idle", "Harvest"
    };

    private void SetAnimatorTrigger(string triggerName)
    {
        //Debug.Log($"Setting trigger {triggerName}");
        foreach (String trigger in m_triggerNames)
        {
            m_animator.ResetTrigger(trigger);
        }

        m_animator.SetTrigger(triggerName);
    }

    private void SetAnimatorFloat(string floatName, float value)
    {
        m_animator.SetFloat(floatName, value);
    }

    private void SetAnimatorBool(string boolName, bool value)
    {
        m_animator.SetBool(boolName, value);
    }

    private void UpdateTask(GathererTask newTask)
    {
        m_gathererTask = newTask;

        //Debug.Log($"{m_gathererData.m_gathererName}'s new task is {m_gathererTask}.");

        switch (m_gathererTask)
        {
            case GathererTask.TravelingToHarvest:
                IsMoving = true;
                SetAnimatorTrigger("Run");
                break;
            case GathererTask.TravelingToDeposit:
                SetAnimatorTrigger("Run");
                IsMoving = true;
                break;
            case GathererTask.TravelingToRuin:
                SetAnimatorTrigger("Run");
                IsMoving = true;
                break;
            case GathererTask.TravelingToIdle:
                SetAnimatorTrigger("Run");
                IsMoving = true;
                break;
            case GathererTask.Harvesting:
                SetAnimatorTrigger("Harvest");
                IsMoving = false;
                if (CurrentHarvestNode)
                {
                    m_curCoroutine = StartCoroutine(Harvesting());
                }
                else
                {
                    RequestNextHarvestNode();
                }

                break;
            case GathererTask.Storing:
                SetAnimatorTrigger("Idle");
                IsMoving = false;
                m_curCoroutine = StartCoroutine(Storing());
                break;
            case GathererTask.Idling:
                SetAnimatorTrigger("Idle");
                IsMoving = false;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void RequestStopCoroutine()
    {
        if (m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }
    }

    void ToggleResourceDisplay(int i)
    {
        m_resourceAnchor.SetActive(i > 0);
    }

    private void ToggleBlockedIndicator()
    {
        // Toggle the Blocked Indicator on if the unit has stopped moving and their path is Blocked from Goal.
        m_gathererBlockedIndicator.SetActive(m_isBlockedFromGoal && !IsMoving);
    }

    private void ToggleIdleIndicator()
    {
        // Toggle the Idle indicator
        bool isIdle = m_gathererTask == GathererTask.Idling && !IsMoving;

        m_gathererIdleIndicator.gameObject.SetActive(isIdle);

        if (isIdle)
        {
            RequestAudioLoop(m_gathererData.m_idleClip);
        }
        else if (m_audioSource.isPlaying && m_audioSource.clip == m_gathererData.m_idleClip)
        {
            m_audioSource.Stop();
            m_audioSource.clip = null;
        }
    }

    private void ClearHarvestVars()
    {
        // Unassign from the node (used for Player interupting the Harvesting state)
        if (CurrentHarvestNode)
        {
            if (m_gathererTask == GathererTask.Harvesting)
            {
                //Debug.Log($"setting tree harvesting value -1");
                CurrentHarvestNode.SetIsHarvesting(-1); // We only want to do this if we've started harvesting, which increments the value.
            }

            CurrentHarvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
        }

        // Stop gatherer Harvest Animation.
        //m_animator.SetBool(m_isHarvestingHash, false);

        CurrentHarvestNode = null;
    }

    private void InterruptHarvesting()
    {
        if (CurrentHarvestNode && m_gathererTask == GathererTask.Harvesting)
        {
            CurrentHarvestNode.SetIsHarvesting(-1); // We only want to do this if we've started harvesting, which increments the value.
        }

        // Stop gatherer Harvest Animation.
        //m_animator.SetBool(m_isHarvestingHash, false);
    }

    private void OnNodeDepleted(ResourceNode node)
    {
        if (node != CurrentHarvestNode)
        {
            return;
        }

        //Debug.Log($"{m_gathererData.m_gathererName}'s CURRENT node DEPLETED.");

        ClearHarvestVars();

        if (m_gathererTask == GathererTask.Harvesting)
        {
            RequestStopCoroutine();

            RequestNextHarvestNode();
        }

        if (m_gathererTask == GathererTask.TravelingToHarvest)
        {
            RequestNextHarvestNode();
        }
    }

    private List<ResourceNode> GetHarvestNodesAtRange(Vector2Int center, int distance)
    {
        List<ResourceNode> resourceNodes = null;

        // Loop through the grid centered around the target
        for (int x = -distance; x <= distance; x++)
        {
            for (int z = -distance; z <= distance; z++)
            {
                // Get the current grid cell
                Vector2Int currentPos = new Vector2Int(center.x + x, center.y + z);

                // Skip the inner grid
                if (Mathf.Abs(x) < distance && Mathf.Abs(z) < distance) continue;

                // Add cells with a resource node in them to the list.
                Cell cell = Util.GetCellFromPos(currentPos);

                if (cell == null) continue;

                if (cell.m_isOutOfBounds) continue;

                if (cell.m_isOccupied)
                {
                    ResourceNode node = cell.m_occupant.GetComponent<ResourceNode>();
                    if (node)
                    {
                        if (resourceNodes == null) resourceNodes = new List<ResourceNode>();
                        resourceNodes.Add(node);
                    }
                }
            }
        }

        return resourceNodes;
    }

    private bool RotateToGoal(Vector3 goalPos)
    {
        bool facingGoal = false;

        Vector3 gathererPos = transform.position;
        Vector3 directionToGoal = goalPos - gathererPos;

        float cumulativeLookSpeed = m_lookSpeed * m_totalSpeedBoost * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(directionToGoal);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, cumulativeLookSpeed);

        facingGoal = Vector3.Angle(transform.forward, directionToGoal) <= 1f;
        return facingGoal;
    }

    private IEnumerator IdleRotate()
    {
        bool facingGoal = false;
        Vector3 goalPos = transform.position + Vector3.back;
        while (facingGoal == false)
        {
            facingGoal = RotateToGoal(goalPos);
            yield return null;
        }
    }

    private int swingCount;
    private int swingsToHarvest = 4;

    private IEnumerator Harvesting()
    {
        swingCount = 0;
        bool facingGoal = false;
        Vector3 goalPos = new Vector3(CurrentGoalCell.m_cellPos.x, 0, CurrentGoalCell.m_cellPos.y);
        while (facingGoal == false)
        {
            facingGoal = RotateToGoal(goalPos);
            yield return null;
        }

        StartHarvesting();

        while (swingCount < swingsToHarvest)
        {
            yield return null;
        }

        //yield return new WaitForSeconds(m_harvestDuration * m_totalDurationBoost);
        CompletedHarvest();
    }

    private IEnumerator Storing()
    {
        bool facingGoal = false;
        Vector3 goalPos = new Vector3(CurrentGoalCell.m_cellPos.x, 0, CurrentGoalCell.m_cellPos.y);
        while (facingGoal == false)
        {
            facingGoal = RotateToGoal(goalPos);
            yield return null;
        }

        yield return new WaitForSeconds(m_storingDuration * m_totalDurationBoost);

        // When storing, the gatherer has 33% per additional m_gathererLevel to store 1 extra wood.
        int storageAmount;
        int random = Random.Range(0, 100);
        int bonusAmount = random < (GathererLevel - 1) * 25 ? 1 : 0; //Change 33 if we want different percentage change per m_gathererLevel.
        storageAmount = m_carryCapacity + bonusAmount;

        ResourceManager.Instance.UpdateWoodAmount(storageAmount, this);

        Vector3 alertPosition = transform.position;
        alertPosition.y += .7f;

        // AUDIO
        RequestPlayAudio(m_gathererData.m_woodDepositClips);

        // ALERT
        if (storageAmount > 1) //Did we deposit a crit amount?
        {
            IngameUIController.Instance.SpawnCritCurrencyAlert(storageAmount, 0, true, alertPosition);
            //RequestPlayAudio(m_gathererData.m_woodDepositClips, m_audioSource);
        }
        else
        {
            IngameUIController.Instance.SpawnCurrencyAlert(storageAmount, 0, true, alertPosition);
            //RequestPlayAudio(m_gathererData.m_woodDepositClips, m_audioSource);
        }

        ResourceCarried = 0;

        SetAnimatorBool("CarryingWood", false);

        RequestNextHarvestNode();
    }

    private void StartHarvesting()
    {
        //m_animator.SetBool(m_isHarvestingHash, true);
        CurrentHarvestNode.SetIsHarvesting(1);
    }

    private void CompletedHarvest()
    {
        m_curCoroutine = null;

        //m_animator.SetBool(m_isHarvestingHash, false);

        PathToDepositCell();

        ValueTuple<int, int> vars = CurrentHarvestNode.RequestResource(m_carryCapacity);
        ResourceCarried = vars.Item1;
        int resourceRemaining = vars.Item2;

        SetAnimatorBool("CarryingWood", true);

        //If there are resources remaining in the node, unset some of the variables on the node.
        if (resourceRemaining > 0)
        {
            CurrentHarvestNode.SetIsHarvesting(-1);
        }
    }

    public GathererTask GetGathererTask()
    {
        return m_gathererTask;
    }

    protected List<ShrineRuinEffect> m_shrineRuinEffects = new List<ShrineRuinEffect>();
    protected List<ShrineRuinEffect> m_newshrineRuinEffects = new List<ShrineRuinEffect>();
    protected List<ShrineRuinEffect> m_expiredshrineRuinEffects = new List<ShrineRuinEffect>();
    private int m_activeSpeedBoostCount;
    private float m_boostValue;
    private float m_totalDurationBoost = 1;
    private float m_totalSpeedBoost = 1;

//Apply the Effect
    public virtual void ApplyEffect(ShrineRuinEffect ruinEffect)
    {
        m_newshrineRuinEffects.Add(ruinEffect);
    }

//Update the Effect
    public void UpdateStatusEffects()
    {
        //Remove Expired Effects
        if (m_expiredshrineRuinEffects.Count > 0)
        {
            foreach (ShrineRuinEffect expiredshrineRuinEffect in m_expiredshrineRuinEffects)
            {
                --m_activeSpeedBoostCount;
                m_shrineRuinEffects.Remove(expiredshrineRuinEffect);
            }

            m_totalDurationBoost = Math.Min(1, MathF.Pow(1 - m_boostValue, m_activeSpeedBoostCount));
            m_totalSpeedBoost = Math.Max(1, MathF.Pow(1 + m_boostValue, m_activeSpeedBoostCount));
            SetAnimatorFloat("SpeedMultiplier", m_totalSpeedBoost);
            m_expiredshrineRuinEffects.Clear();
        }

        //Update each Effect.
        foreach (ShrineRuinEffect activeShrineRuinEffect in m_shrineRuinEffects)
        {
            HandleEffect(activeShrineRuinEffect);
        }

        //Add New Effects if the sender is unique (Does not already have this effect)
        if (m_newshrineRuinEffects.Count > 0)
        {
            foreach (ShrineRuinEffect newshrineRuinEffect in m_newshrineRuinEffects)
            {
                m_shrineRuinEffects.Add(newshrineRuinEffect);
                ++m_activeSpeedBoostCount;
                m_boostValue = newshrineRuinEffect.m_effectSpeedBoost;
            }

            m_totalDurationBoost = Math.Min(1, MathF.Pow(1 - m_boostValue, m_activeSpeedBoostCount));
            m_totalSpeedBoost = Math.Max(1, MathF.Pow(1 + m_boostValue, m_activeSpeedBoostCount));
            SetAnimatorFloat("SpeedMultiplier", m_totalSpeedBoost);
            m_newshrineRuinEffects.Clear();
        }
    }

//Handle the Effect
    public void HandleEffect(ShrineRuinEffect ruinEffect)
    {
        ruinEffect.m_elapsedTime += Time.deltaTime;
        if (ruinEffect.m_elapsedTime > ruinEffect.m_lifeTime)
        {
            RemoveEffect(ruinEffect);
        }
    }

    public void RemoveEffect(ShrineRuinEffect ruinEffect)
    {
        m_expiredshrineRuinEffects.Add(ruinEffect);
    }

    public GathererTooltipData GetTooltipData()
    {
        GathererTooltipData data = new GathererTooltipData
        {
            m_gathererType = m_gathererData.m_type,
            m_gathererName = gameObject.name,
            m_gathererDescription = m_gathererData.m_gathererDescription,
            m_harvestDuration = m_harvestDuration,
            m_storingDuration = m_storingDuration,
            m_carryCapacity = m_carryCapacity,
            m_gathererLevel = GathererLevel
        };
        return data;
    }
}

public class GathererTooltipData
{
    public ResourceManager.ResourceType m_gathererType;
    public string m_gathererName;
    public string m_gathererDescription;
    public float m_harvestDuration;
    public float m_storingDuration;
    public int m_carryCapacity;
    public int m_gathererLevel;
}

[Serializable]
public class ShrineRuinEffect
{
    public float m_effectSpeedBoost = 0.1f;
    public float m_elapsedTime;
    public float m_lifeTime = 40f;
}