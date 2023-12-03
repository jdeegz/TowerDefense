using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GathererController : MonoBehaviour
{
    [SerializeField] private GathererData m_GathererData;
    [SerializeField] private ParticleSystem m_idleStateVFX;
    public GathererTask m_gathererTask;
    public Animator m_animator;
    [SerializeField] private Vector2Int m_curPos;
    [SerializeField] private Cell m_curCell;

    [Header("Audio")] [SerializeField] private List<AudioClip> m_woodChopClips;
    private AudioSource m_audioSource;

    public enum GathererTask
    {
        Idling,
        FindingHarvestablePoint,
        TravelingToHarvest,
        Harvesting,
        TravelingToCastle,
        Storing,
    }

    private bool m_isSelected;
    private bool m_isMoving;
    private int m_resourceCarried;
    private int m_curHarvestPointIndex;
    private Vector3 m_curHarvestNodePos;
    private ResourceNode m_curHarvestNode;
    private Vector3 m_curHarvestPointPos;
    private Coroutine m_curCoroutine;
    private float m_harvestDuration;
    private int m_carryCapacity;
    private float m_storingDuration;

    private static int m_isHarvestingHash = Animator.StringToHash("isHarvesting");
    private Vector3 m_idlePos;
    private List<Vector2Int> m_gathererPath;
    private int m_gathererPathProgress;
    private Vector3 m_moveDirection;
    private Quaternion m_previousRotation;

    private void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        GameplayManager.OnGameObjectSelected += GathererSelected;
        GameplayManager.OnCommandRequested += CommandRequested;
        m_audioSource = GetComponent<AudioSource>();
        m_idlePos = transform.position;
        m_harvestDuration = m_GathererData.m_harvestDuration;
        m_carryCapacity = m_GathererData.m_carryCapacity;
        m_storingDuration = m_GathererData.m_storingDuration;
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
        GameplayManager.OnGameObjectSelected -= GathererSelected;
        GameplayManager.OnCommandRequested -= CommandRequested;
        GameplayManager.Instance.RemoveGathererFromList(this, m_GathererData.m_type);
    }

    private void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            m_curPos = new Vector2Int((int)Mathf.Floor(transform.position.x + 0.5f), (int)Mathf.Floor(transform.position.z + 0.5f));
            m_curCell = Util.GetCellFromPos(m_curPos);
            m_curCell.UpdateActorCount(1, gameObject.name);
        }
    }

    void Start()
    {
        switch (m_GathererData.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                GameplayManager.Instance.AddGathererToList(this, m_GathererData.m_type);
                break;
            case ResourceManager.ResourceType.Stone:
                GameplayManager.Instance.AddGathererToList(this, m_GathererData.m_type);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void Update()
    {
        CheckHarvestPointAccessible();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        //If we have no path, no need to move.
        if (m_gathererPath == null) return;

        float remainingDistance = Vector3.Distance(transform.position, new Vector3(m_gathererPath.Last().x, 0, m_gathererPath.Last().y));
        float stoppingDistance = .1f;

        if (remainingDistance <= stoppingDistance)
        {
            Debug.Log($"Destination Reached.");
            m_gathererPath = null;
            m_gathererPathProgress = 0;

            //If we are/were traveling to a harvest point, transition to harvesting.
            if (m_gathererTask == GathererTask.TravelingToHarvest)
            {
                UpdateTask(GathererTask.Harvesting);
            }

            //If we are/were travelling to storage, transition to storing.
            if (m_gathererTask == GathererTask.TravelingToCastle)
            {
                UpdateTask(GathererTask.Storing);
            }

            //If we are/were travelling to storage, transition to storing.
            if (m_gathererTask == GathererTask.Idling)
            {
                ToggleDisplayIdleVFX();
            }

            return;
        }

        if (m_curCell.m_cellPos == m_gathererPath[0])
        {
            m_gathererPathProgress = 1;
        }

        //Check to see if we're in a new cell.
        //Update Cell occupancy
        Vector2Int newPos = Util.GetVector2IntFrom3DPos(transform.position);
        if (newPos != m_curPos)
        {
            //Remove self from current cell.
            if (m_curCell != null)
            {
                m_curCell.UpdateActorCount(-1, gameObject.name);
            }

            //Assign new position
            m_curPos = newPos;

            //Get new cell from new position.
            m_curCell = Util.GetCellFromPos(m_curPos);

            //Assign self to cell.
            m_curCell.UpdateActorCount(1, gameObject.name);

            //Increment the path index to know which cell we want to move towards.
            if (m_gathererPathProgress < m_gathererPath.Count - 1)
            {
                ++m_gathererPathProgress;
            }
        }

        //Get the position of the next cell.
        Vector3 m_nextCellPosition = new Vector3(m_gathererPath[m_gathererPathProgress].x, 0, m_gathererPath[m_gathererPathProgress].y);

        m_moveDirection = (m_nextCellPosition - transform.position).normalized;

        //Look towards the move direction.
        float cumulativeLookSpeed = 5f * Time.deltaTime;
        Quaternion targetRotation = Quaternion.LookRotation(m_moveDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, cumulativeLookSpeed);

        //Get the remaining angle from current rotation to destination.
        Vector2 moveDir2d = new Vector2(m_moveDirection.x, m_moveDirection.z);
        Vector2 unitForward2d = new Vector2(transform.forward.x, transform.forward.z);
        float rotationAmount = (float)Math.Pow(1 - Vector2.Angle(moveDir2d, unitForward2d) / 180, 3);


        //Move forward.
        float cumulativeMoveSpeed = 1f * Time.deltaTime;

        // Calculate the decelerated move speed based on the rotation
        float deceleratedMoveSpeed = Mathf.Lerp(cumulativeMoveSpeed * .2f, cumulativeMoveSpeed, rotationAmount);
        transform.position += transform.forward * deceleratedMoveSpeed;
    }

    private void CheckHarvestPointAccessible()
    {
        if (m_gathererTask != GathererTask.TravelingToHarvest) return;

        Cell m_harvestPointCell = m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_harvestPointCell;
        if (m_harvestPointCell.m_isOccupied)
        {
            m_isMoving = false;
            UpdateTask(GathererTask.FindingHarvestablePoint);
        }
    }

    private void GathererSelected(GameObject selectedObj)
    {
        m_isSelected = selectedObj == gameObject;
    }

    public void AudioPlayWoodChop()
    {
        int i = Random.Range(0, m_woodChopClips.Count);
        m_audioSource.PlayOneShot(m_woodChopClips[i]);
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
                if (m_GathererData.m_type == ResourceManager.ResourceType.Wood)
                {
                    RequestedHarvest(requestObj);
                }

                break;
            case Selectable.SelectedObjectType.ResourceStone:
                if (m_GathererData.m_type == ResourceManager.ResourceType.Stone)
                {
                    RequestedHarvest(requestObj);
                }

                break;
            case Selectable.SelectedObjectType.Tower:
                break;
            case Selectable.SelectedObjectType.Gatherer:
                break;
            case Selectable.SelectedObjectType.Castle:
                Debug.Log("Gatherer going to castle.");
                RequestedIdle();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void RequestedIdle()
    {
        ClearHarvestVars();
        if (m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }

        UpdateTask(GathererTask.Idling);
    }

    private void RequestedHarvest(GameObject requestObj)
    {
        // //Find and set variables for this script.
        ValueTuple<ResourceNode, Vector3, int> vars = GetHarvestPointFromObj(requestObj);

        if (vars.Item1)
        {
            ClearHarvestVars();
            if (m_curCoroutine != null)
            {
                StopCoroutine(m_curCoroutine);
                m_curCoroutine = null;
            }

            SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);
            UpdateTask(GathererTask.TravelingToHarvest);
            m_curHarvestNode.WasSelected();
        }
    }

    private void UpdateTask(GathererTask newTask)
    {
        m_gathererTask = newTask;

        ToggleDisplayIdleVFX();

        Debug.Log($"{gameObject.name}'s task updated to: {m_gathererTask}");
        Vector2Int startPos;
        Vector2Int endPos;
        switch (m_gathererTask)
        {
            case GathererTask.Idling:
                startPos = Util.GetVector2IntFrom3DPos(transform.position);
                endPos = Util.GetVector2IntFrom3DPos(m_idlePos);
                m_gathererPath = AStar.FindPath(startPos, endPos);
                break;
            case GathererTask.FindingHarvestablePoint:
                if (m_curHarvestNode)
                {
                    Debug.Log("Finding point to harvest from.");
                    ValueTuple<ResourceNode, Vector3, int> vars = GetHarvestPointFromObj(m_curHarvestNode.gameObject);

                    //If we're given an index greater equal to or greater than 0, we can harvest this node.
                    if (vars.Item3 >= 0)
                    {
                        SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);
                    }
                    //Else we cannot harvest this node because no points are pathable, so we go to idle.
                    else
                    {
                        UpdateTask(GathererTask.Idling);
                        break;
                    }
                }
                else
                {
                    //If we dont have a node, find a nearby node.
                    //Get Neighbor Cells.
                    Debug.Log("Finding new nearby node & point.");
                    ValueTuple<ResourceNode, Vector3, int> vars = GetHarvestPoint(GetHarvestNodes(m_curHarvestNodePos, 1f));
                    if (vars.Item1 == null)
                    {
                        UpdateTask(GathererTask.Idling);
                        break;
                    }

                    SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);
                }

                Debug.Log($"Moving to {m_curHarvestNode.gameObject.name}");
                UpdateTask(GathererTask.TravelingToHarvest);
                break;
            case GathererTask.TravelingToHarvest:
                break;
            case GathererTask.Harvesting:
                m_curCoroutine = StartCoroutine(Harvesting());
                break;
            case GathererTask.TravelingToCastle:
                startPos = Util.GetVector2IntFrom3DPos(transform.position);
                endPos = Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_enemyGoal.position);
                m_gathererPath = AStar.FindPath(startPos, endPos);
                if (m_gathererPath == null)
                {
                    Debug.Log("No path back to castle found");
                }

                break;
            case GathererTask.Storing:
                m_curCoroutine = StartCoroutine(Storing());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ToggleDisplayIdleVFX()
    {
        m_idleStateVFX.gameObject.SetActive(m_gathererTask == GathererTask.Idling);
    }

    private void SetHarvestVars(ResourceNode harvestNode, Vector3 harvestPointPos, int harvestPointIndex)
    {
        //Setup this script
        Debug.Log($"{gameObject.name} Vars set to {harvestNode.gameObject.name} at {harvestPointPos} at index {harvestPointIndex}");
        m_curHarvestNodePos = harvestNode.transform.position;
        m_curHarvestNode = harvestNode;
        m_curHarvestPointPos = harvestPointPos;
        m_curHarvestPointIndex = harvestPointIndex;

        //Sign up to the node.
        m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_isOccupied = true;

        m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = this;

        m_curHarvestNode.OnResourceNodeDepletion += OnNodeDepleted;
    }

    private void ClearHarvestVars()
    {
        //Unassign from the node (used for Player interupting the Harvesting state)
        if (m_curHarvestNode)
        {
            Debug.Log($"Clearing {m_curHarvestNode.gameObject.name} from {gameObject.name} vars.");
            m_curHarvestNode.SetIsHarvesting(-1);
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_isOccupied = false;
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = null;
            m_curHarvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
        }

        //Unset this script
        m_animator.SetBool(m_isHarvestingHash, false);
        m_curHarvestNode = null;
        m_curHarvestPointPos = new Vector3();
        m_curHarvestPointIndex = -1;
    }

    private void OnNodeDepleted(ResourceNode node)
    {
        if (node != m_curHarvestNode)
        {
            return;
        }

        Debug.Log($"{gameObject.name}'s resource node depleted.");

        //If harvesting, stop cur coroutine.
        //Previous Condition: m_resourceCarried == 0 && m_curCoroutine != null
        if (m_gathererTask == GathererTask.Harvesting)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
            Debug.Log($"{gameObject.name}'s Harvesting coroutine stopped.");
        }

        ClearHarvestVars();

        //Interrupt flow if we're not currently carrying or storing resources.
        if (m_resourceCarried == 0)
        {
            UpdateTask(GathererTask.FindingHarvestablePoint);
        }
    }

    private List<ResourceNode> GetHarvestNodes(Vector3 pos, float searchRange)
    {
        LayerMask layerMask = LayerMask.GetMask("Actors");
        List<ResourceNode> nearbyNodes = new List<ResourceNode>();

        //Get a bunch of nodes near the point.
        Collider[] colliders = Physics.OverlapSphere(pos, searchRange, layerMask);

        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                ResourceNode newNode = collider.GetComponent<ResourceNode>();
                if (newNode != null)
                {
                    if (newNode.m_type == m_GathererData.m_type && newNode.HasResources())
                    {
                        nearbyNodes.Add(newNode);
                    }
                }
            }
        }

        return nearbyNodes;
    }

    private (ResourceNode, Vector3, int) GetHarvestPoint(List<ResourceNode> nodes)
    {
        ResourceNode closestNode = null;
        Vector3 closestNodePointPos = new Vector3();
        int harvestPointIndex = -1;

        Vector2Int curPos = Util.GetVector2IntFrom3DPos(transform.position);
        int shortestDistance = 99999;

        //Loop through nodes.
        for (var i = 0; i < nodes.Count; ++i)
        {
            ResourceNode node = nodes[i];

            //Loop through points.
            for (int x = 0; x < node.m_harvestPoints.Count; ++x)
            {
                //If it's occupied by another gatherer, go next.
                bool harvestPointOccupied = node.m_harvestPoints[x].m_isOccupied;
                bool cellOccupied = node.m_harvestPoints[x].m_harvestPointCell.m_isOccupied;

                if (harvestPointOccupied || cellOccupied)
                {
                    continue;
                }

                //Check path to the harvest point cell, not the Harvest Point Position.
                List<Vector2Int> path = AStar.FindPath(curPos, node.m_harvestPoints[x].m_harvestPointCell.m_cellPos);

                if (path == null)
                {
                    continue;
                }

                if (path.Count <= shortestDistance)
                {
                    closestNode = node;
                    shortestDistance = path.Count;
                    closestNodePointPos = node.m_harvestPoints[x].m_harvestPointPos;
                    harvestPointIndex = x;
                    m_gathererPath = path;
                }
            }
        }

        return (closestNode, closestNodePointPos, harvestPointIndex);
    }

    private (ResourceNode, Vector3, int) GetHarvestPointFromObj(GameObject obj)
    {
        ResourceNode node = obj.GetComponent<ResourceNode>();
        Vector3 closestNodePointPos = new Vector3();
        int harvestPointIndex = -1;

        Vector2Int curPos = Util.GetVector2IntFrom3DPos(transform.position);
        int shortestDistance = 99999;

        for (var i = 0; i < node.m_harvestPoints.Count; ++i)
        {
            bool harvestPointOccupied = node.m_harvestPoints[i].m_isOccupied;
            bool cellOccupied = node.m_harvestPoints[i].m_harvestPointCell.m_isOccupied;
            //Debug.Log($"Harvest Point {i} occupancy: {harvestPointOccupied}");
            if (harvestPointOccupied || cellOccupied)
            {
                continue;
            }

            //Check path to the cell, not the Harvest Point Position.
            List<Vector2Int> path = AStar.FindPath(curPos, node.m_harvestPoints[i].m_harvestPointCell.m_cellPos);

            if (path == null)
            {
                continue;
            }

            if (path.Count <= shortestDistance)
            {
                shortestDistance = path.Count;
                closestNodePointPos = node.m_harvestPoints[i].m_harvestPointPos;
                harvestPointIndex = i;
                m_gathererPath = path;
            }
        }

        return (node, closestNodePointPos, harvestPointIndex);
    }

    private IEnumerator Harvesting()
    {
        StartHarvesting();
        yield return new WaitForSeconds(m_harvestDuration);
        CompletedHarvest();
    }

    private IEnumerator Storing()
    {
        yield return new WaitForSeconds(m_storingDuration);
        switch (m_GathererData.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                ResourceManager.Instance.UpdateWoodAmount(m_resourceCarried);
                IngameUIController.Instance.SpawnCurrencyAlert(m_resourceCarried, 0, true, transform.position);
                break;
            case ResourceManager.ResourceType.Stone:
                ResourceManager.Instance.UpdateStoneAmount(m_resourceCarried);
                IngameUIController.Instance.SpawnCurrencyAlert(0, m_resourceCarried, true, transform.position);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        m_resourceCarried = 0;

        UpdateTask(GathererTask.FindingHarvestablePoint);
    }

    private void StartHarvesting()
    {
        m_animator.SetBool(m_isHarvestingHash, true);
        m_curHarvestNode.SetIsHarvesting(1);
    }

    private void CompletedHarvest()
    {
        Debug.Log($"{gameObject.name}'s harvesting completed.");
        ValueTuple<int, int> vars = m_curHarvestNode.RequestResource(m_carryCapacity);
        m_resourceCarried = vars.Item1;
        int resourceRemaining = vars.Item2;

        //If there are resources remaining in the node, unset some of the variables on the node.
        if (resourceRemaining > 0)
        {
            m_curHarvestNode.SetIsHarvesting(-1);
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_isOccupied = false;
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = null;
        }

        m_animator.SetBool(m_isHarvestingHash, false);
        UpdateTask(GathererTask.TravelingToCastle);
    }

    private GameObject GetClosestObject(List<GameObject> objs)
    {
        GameObject closestGameObject = null;
        int shortestDistance = 9999;
        Vector2Int curPos = Util.GetVector2IntFrom3DPos(transform.position);

        foreach (GameObject obj in objs)
        {
            Vector2Int objPos = Util.GetVector2IntFrom3DPos(obj.transform.position);
            List<Vector2Int> path = AStar.FindPath(curPos, objPos);

            if (path == null)
            {
                continue;
            }

            if (path.Count <= shortestDistance)
            {
                shortestDistance = path.Count;
                closestGameObject = obj;
            }
        }

        return closestGameObject;
    }

    public GathererTooltipData GetTooltipData()
    {
        GathererTooltipData data = new GathererTooltipData();
        data.m_gathererType = m_GathererData.m_type;
        data.m_gathererName = m_GathererData.m_gathererName;
        data.m_gathererDescription = m_GathererData.m_gathererDescription;
        data.m_harvestDuration = m_harvestDuration;
        data.m_storingDuration = m_storingDuration;
        data.m_carryCapacity = m_carryCapacity;
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
}