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
    public GathererData m_gathererData;
    public GameObject m_resourceAnchor;
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
        TravelingToRuin,
    }

    private bool m_isSelected;
    private bool m_isMoving;
    private int m_resourceCarried;
    private int m_curHarvestPointIndex;
    private Vector3 m_curHarvestNodePos;
    private ResourceNode m_curHarvestNode;
    private RuinController m_curRuin;
    private Vector2Int m_curHarvestPointPos;
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
    private int m_gathererLevel = 1;

    private void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        GameplayManager.OnGameObjectSelected += GathererSelected;
        GameplayManager.OnCommandRequested += CommandRequested;
        m_audioSource = GetComponent<AudioSource>();
        m_idlePos = transform.position;
        m_harvestDuration = m_gathererData.m_harvestDuration;
        m_carryCapacity = m_gathererData.m_carryCapacity;
        m_storingDuration = m_gathererData.m_storingDuration;
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
        GameplayManager.OnGameObjectSelected -= GathererSelected;
        GameplayManager.OnCommandRequested -= CommandRequested;
        GameplayManager.Instance.RemoveGathererFromList(this, m_gathererData.m_type);
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

    public void RequestIncrementGathererLevel(int i)
    {
        //If the ruin has no power up to give, get outta here.
        if (!m_curRuin.RequestPowerUp()) return;

        IngameUIController.Instance.SpawnLevelUpAlert(gameObject, transform.position);
        m_gathererLevel += i;
        RequestPlayAudio(m_gathererData.m_levelUpClip, m_audioSource);
    }

    void Start()
    {
        switch (m_gathererData.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                GameplayManager.Instance.AddGathererToList(this, m_gathererData.m_type);
                break;
            case ResourceManager.ResourceType.Stone:
                GameplayManager.Instance.AddGathererToList(this, m_gathererData.m_type);
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
        HandleLookRotation();
    }

    private void HandleLookRotation()
    {
        //Only take over rotation if the unit is not pathing somewhere.
        if (m_gathererPath != null) return;
        Vector3 directionToTarget;
        Quaternion targetRotation;
        float cumulativeLookSpeed = 10f * Time.deltaTime;

        switch (m_gathererTask)
        {
            case GathererTask.Idling:
                directionToTarget = Vector3.back;
                targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, cumulativeLookSpeed);
                break;
            case GathererTask.Harvesting:
                directionToTarget = m_curHarvestNode.transform.position - transform.position;
                targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, cumulativeLookSpeed);
                break;
            default:
                break;
        }
    }

    private void HandleMovement()
    {
        //If we have no path, no need to move.
        if (m_gathererPath == null) return;

        float remainingDistance = Vector3.Distance(transform.position, new Vector3(m_gathererPath.Last().x, 0, m_gathererPath.Last().y));
        float stoppingDistance = .05f;

        if (m_gathererTask == GathererTask.TravelingToCastle)
        {
            stoppingDistance = 2f;
        }

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

            if (m_gathererTask == GathererTask.TravelingToRuin)
            {
                UpdateTask(GathererTask.Idling);

                //Request the level up.
                RequestIncrementGathererLevel(1);
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
        if (m_isSelected) RequestPlayAudio(m_gathererData.m_selectedGathererClips, m_audioSource);
    }

    public void AudioPlayWoodChop()
    {
        int i = Random.Range(0, m_gathererData.m_harvestingClips.Count);
        m_audioSource.PlayOneShot(m_gathererData.m_harvestingClips[i]);
    }

    public void RequestPlayAudio(AudioClip clip, AudioSource source)
    {
        source.PlayOneShot(clip);
    }

    public void RequestPlayAudio(List<AudioClip> clips, AudioSource source)
    {
        int i = Random.Range(0, clips.Count);
        source.PlayOneShot(clips[i]);
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
                if (m_gathererData.m_type == ResourceManager.ResourceType.Wood && m_resourceCarried == 0)
                {
                    RequestedHarvest(requestObj);
                }

                break;
            case Selectable.SelectedObjectType.ResourceStone:
                if (m_gathererData.m_type == ResourceManager.ResourceType.Stone && m_resourceCarried == 0)
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
                if (m_resourceCarried == 0)
                {
                    RequestedIdle();
                }
                else
                {
                    UpdateTask(GathererTask.TravelingToCastle);
                }
                break;
            case Selectable.SelectedObjectType.Ruin:
                Debug.Log("Gatherer going to castle.");
                RequestTravelToRuin(requestObj);
                break;
            case Selectable.SelectedObjectType.Obelisk:
                Debug.Log($"Obelisk selected for command, nothing to do here.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        RequestPlayAudio(m_gathererData.m_commandRequestClips, m_audioSource);
    }

    private void RequestedIdle()
    {
        ClearHarvestVars();
        if (m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }

        //If the current position is within 2 cells of the exit, just idle here. If not path to an idle position.
        float stoppingDistance = 2.1f;
        float currentDistance = Vector2Int.Distance(m_curCell.m_cellPos, GameplayManager.Instance.m_goalPointPos);

        //If we are far away, path home.
        if (currentDistance > stoppingDistance)
        {
            Vector2Int startPos = Util.GetVector2IntFrom3DPos(transform.position);
            Vector2Int endPos = Util.GetVector2IntFrom3DPos(m_idlePos);
            m_gathererPath = AStar.FindPath(startPos, endPos);
        }

        //Update task.
        UpdateTask(GathererTask.Idling);
    }

    private void RequestTravelToRuin(GameObject requestObj)
    {
        ClearHarvestVars();

        m_curRuin = requestObj.GetComponent<RuinController>();
        if (m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }

        //If the current position is within stopping distance of the ruin...
        float stoppingDistance = 1.41f;
        Vector2Int requestObjPos = Util.GetVector2IntFrom3DPos(requestObj.transform.position);
        float currentDistance = Vector2Int.Distance(m_curCell.m_cellPos, requestObjPos);

        //If we are far away, path home.
        if (currentDistance > stoppingDistance)
        {
            //Generate a path to the ruin.
            m_gathererPath = GetShortestPathToObj(requestObj);

            //Move to the ruin.
            UpdateTask(GathererTask.TravelingToRuin);
        }
        else
        {
            m_gathererPath = null;
            UpdateTask(GathererTask.Idling);

            //Request the level up.
            RequestIncrementGathererLevel(1);
        }
    }

    private void RequestedHarvest(GameObject requestObj)
    {
        //Are we next to the node already?
        ResourceNode node = requestObj.GetComponent<ResourceNode>();
        for (var i = 0; i < node.m_harvestPoints.Count; i++)
        {
            var harvestPoint = node.m_harvestPoints[i];
            if (harvestPoint.m_harvestPointCell == m_curCell && (harvestPoint.m_gatherer == this || harvestPoint.m_gatherer == null))
            {
                //We're in a harvest point cell.
                ClearHarvestVars();
                if (m_curCoroutine != null)
                {
                    StopCoroutine(m_curCoroutine);
                    m_curCoroutine = null;
                }

                SetHarvestVars(node, harvestPoint.m_harvestPointPos, i);
                m_gathererPath = null;
                UpdateTask(GathererTask.Harvesting);
                
                m_curHarvestNode.WasSelected();
                return;
            }
        }

        //We're not next to the node, we need to find a path to it.
        //Find and set variables for this script.
        ValueTuple<ResourceNode, Vector2Int, int> vars = GetHarvestPointFromObj(requestObj);

        //Do we have a path to the node, and is there a valid point to harvest from?
        if (vars.Item1 && vars.Item3 >= 0)
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
        m_resourceAnchor.SetActive(m_resourceCarried > 0);

        Debug.Log($"{gameObject.name}'s task updated to: {m_gathererTask}");
        Vector2Int startPos;
        Vector2Int endPos;
        switch (m_gathererTask)
        {
            case GathererTask.Idling:

                break;
            case GathererTask.FindingHarvestablePoint:
                if (m_curHarvestNode)
                {
                    Debug.Log("Finding point to harvest from.");
                    ValueTuple<ResourceNode, Vector2Int, int> vars = GetHarvestPointFromObj(m_curHarvestNode.gameObject);

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
                    ValueTuple<ResourceNode, Vector2Int, int> vars = GetHarvestPoint(GetHarvestNodes(m_curHarvestNodePos, 1f));
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
            case GathererTask.TravelingToRuin:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ToggleDisplayIdleVFX()
    {
        m_idleStateVFX.gameObject.SetActive(m_gathererTask == GathererTask.Idling);
    }

    private void SetHarvestVars(ResourceNode harvestNode, Vector2Int harvestPointPos, int harvestPointIndex)
    {
        //Setup this script
        Debug.Log($"{gameObject.name} Vars set to {harvestNode.gameObject.name} at {harvestPointPos} at index {harvestPointIndex}");
        m_curHarvestNodePos = harvestNode.transform.position;
        m_curHarvestNode = harvestNode;
        m_curHarvestPointPos = harvestPointPos;
        m_curHarvestPointIndex = harvestPointIndex;

        //Sign up to the node.
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
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = null;
            m_curHarvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
        }

        //Unset this script
        m_animator.SetBool(m_isHarvestingHash, false);
        m_curHarvestNode = null;
        m_curRuin = null;
        m_curHarvestPointPos = new Vector2Int();
        m_curHarvestPointIndex = -1;
    }

    private void OnNodeDepleted(ResourceNode node)
    {
        if (node != m_curHarvestNode)
        {
            return;
        }

        Debug.Log($"{gameObject.name}'s resource node depleted.");
        ClearHarvestVars();

        //If harvesting, stop cur coroutine.
        //Previous Condition: m_resourceCarried == 0 && m_curCoroutine != null
        if (m_gathererTask == GathererTask.Harvesting)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
            Debug.Log($"{gameObject.name}'s Harvesting coroutine stopped.");
        }

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
                    if (newNode.m_type == m_gathererData.m_type && newNode.HasResources())
                    {
                        nearbyNodes.Add(newNode);
                    }
                }
            }
        }

        return nearbyNodes;
    }

    private (ResourceNode, Vector2Int, int) GetHarvestPoint(List<ResourceNode> nodes)
    {
        ResourceNode closestNode = null;
        Vector2Int closestNodePointPos = new Vector2Int();
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
                bool cellOccupied = node.m_harvestPoints[x].m_harvestPointCell.m_isOccupied;

                if ((node.m_harvestPoints[i].m_gatherer != null && node.m_harvestPoints[i].m_gatherer != this) || cellOccupied)
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

    private (ResourceNode, Vector2Int, int) GetHarvestPointFromObj(GameObject obj)
    {
        ResourceNode node = obj.GetComponent<ResourceNode>();
        Vector2Int closestNodePointPos = new Vector2Int();
        int harvestPointIndex = -1;

        Vector2Int curPos = Util.GetVector2IntFrom3DPos(transform.position);
        int shortestDistance = 99999;

        for (var i = 0; i < node.m_harvestPoints.Count; ++i)
        {
            bool cellOccupied = node.m_harvestPoints[i].m_harvestPointCell.m_isOccupied;
            
            if ((node.m_harvestPoints[i].m_gatherer != null && node.m_harvestPoints[i].m_gatherer != this) || cellOccupied)
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

    private List<Vector2Int> GetShortestPathToObj(GameObject obj)
    {
        List<Vector2Int> shortestPath = new List<Vector2Int>();

        Vector2Int curPos = Util.GetVector2IntFrom3DPos(transform.position);
        Vector2Int endPos = Util.GetVector2IntFrom3DPos(obj.transform.position);

        //Get the neighbor positions to check for distance.
        ValueTuple<List<Cell>, List<Vector2Int>> vars = Util.GetNeighborHarvestPointCells(endPos);

        for (var i = 0; i < vars.Item2.Count; ++i)
        {
            bool cellOccupied = vars.Item1[i].m_isOccupied;
            if (cellOccupied)
            {
                continue;
            }

            //Check path to the cell
            List<Vector2Int> path = AStar.FindPath(curPos, vars.Item1[i].m_cellPos);

            if (path == null)
            {
                continue;
            }

            if (shortestPath.Count == 0 || path.Count <= shortestPath.Count)
            {
                shortestPath = path;
            }
        }

        return shortestPath;
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
        //Calculate storage based on level. Lvl 1 = 1, Lvl 2 = 1 + 50% * 2
        int storageAmount;
        int random = Random.Range(0, 2);
        switch (m_gathererData.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                storageAmount = m_carryCapacity + ((m_gathererLevel - 1) * random);
                ResourceManager.Instance.UpdateWoodAmount(storageAmount);
                IngameUIController.Instance.SpawnCurrencyAlert(storageAmount, 0, true, transform.position);
                break;
            case ResourceManager.ResourceType.Stone:
                storageAmount = m_carryCapacity; //To add stone levelng later!
                ResourceManager.Instance.UpdateStoneAmount(storageAmount);
                IngameUIController.Instance.SpawnCurrencyAlert(0, storageAmount, true, transform.position);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Debug.Log($"Stored: {m_carryCapacity} + {m_gathererLevel - 1} * {random}.");

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
        data.m_gathererType = m_gathererData.m_type;
        data.m_gathererName = gameObject.name;
        data.m_gathererDescription = m_gathererData.m_gathererDescription;
        data.m_harvestDuration = m_harvestDuration;
        data.m_storingDuration = m_storingDuration;
        data.m_carryCapacity = m_carryCapacity;
        data.m_gathererLevel = m_gathererLevel;
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