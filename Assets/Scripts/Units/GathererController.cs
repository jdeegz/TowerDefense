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
    [SerializeField] private GameObject m_idleVFXParent;
    public GathererTask m_gathererTask;
    public Animator m_animator;
    [SerializeField] private Vector2Int m_curPos;
    [SerializeField] private Cell m_curCell;

    [Header("Audio")] [SerializeField] private List<AudioClip> m_woodChopClips;
    private AudioSource m_audioSource;

    public enum GathererTask
    {
        NotInitialized,
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
    private List<Vector2Int> m_depositLocations;
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
    private int level;
    private int m_debugIndex;
    private Vector3 m_nextCellPosition;

    public int m_gathererLevel
    {
        get { return level; }
        set
        {
            if (level != value) // Only trigger the event if the value actually changes
            {
                level = value;
                GathererLevelChange?.Invoke(level);
            }
        }
    }

    public event Action<int> GathererLevelChange;

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
            UpdateTask(GathererTask.Idling);
        }

        if (newState == GameplayManager.GameplayState.CreatePaths) // Moved this code out of Place Obstacles, because that is also when Obelisks are added GameplayManager.
        {
            m_depositLocations = new List<Vector2Int>();

            foreach (Obelisk obelisk in GameplayManager.Instance.m_obelisksInMission)
            {
                m_depositLocations.AddRange(Util.GetAdjacentCellPos(Util.GetVector2IntFrom3DPos(obelisk.gameObject.transform.position)));
            }

            m_depositLocations.AddRange(Util.GetBoxAround3x3Grid(Util.GetVector2IntFrom3DPos(GameplayManager.Instance.m_castleController.transform.position)));

            /*foreach (GameObject castleEntrance in GameplayManager.Instance.m_castleController.m_castleEntrancePoints)
            {
                m_depositLocations.Add(Util.GetVector2IntFrom3DPos(castleEntrance.transform.position));
            }*/
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
                m_gathererLevel += 1;
                break;
            case ResourceManager.ResourceType.Stone:
                GameplayManager.Instance.AddGathererToList(this, m_gathererData.m_type);
                m_gathererLevel += 1;
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
            stoppingDistance = .1f;
        }

        if (remainingDistance <= stoppingDistance)
        {
            Debug.Log($"{m_debugIndex += 1}. Destination Reached.");
            m_gathererPath = null;
            m_gathererPathProgress = 0;

            // If we are/were traveling to a harvest point, transition to harvesting.
            if (m_gathererTask == GathererTask.TravelingToHarvest)
            {
                Debug.Log($"{m_debugIndex += 1}. Arrived at harvest position.");
                UpdateTask(GathererTask.Harvesting);
            }

            // If we are/were travelling to storage, transition to storing.
            if (m_gathererTask == GathererTask.TravelingToCastle)
            {
                Debug.Log($"{m_debugIndex += 1}. Arrived at storage position.");
                UpdateTask(GathererTask.Storing);
            }

            // Enable the ZZZ vfx if we're idle.
            if (m_gathererTask == GathererTask.Idling)
            {
                //ToggleDisplayIdleVFX();

                if (m_resourceCarried > 0)
                {
                    UpdateTask(GathererTask.TravelingToCastle);
                }
            }

            if (m_gathererTask == GathererTask.TravelingToRuin)
            {
                if (m_resourceCarried > 0)
                {
                    UpdateTask(GathererTask.TravelingToCastle);
                }
                else
                {
                    UpdateTask(GathererTask.Idling);
                }

                // Request the level up.
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
        if (m_gathererPathProgress < m_gathererPath.Count && m_gathererPathProgress >= 0) //Getting index out of bounds, so wrapping this position in a check.
        {
            m_nextCellPosition = new Vector3(m_gathererPath[m_gathererPathProgress].x, 0, m_gathererPath[m_gathererPathProgress].y);
        }

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

    public void RequestAudioLoop(AudioClip clip, AudioSource source)
    {
        source.Stop();
        source.loop = true;
        source.clip = clip;
        source.Play();
    }

    public void RequestPlayAudio(AudioClip clip, AudioSource source)
    {
        source.Stop();
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
                Debug.Log($"{m_debugIndex += 1}. Gatherer going to castle.");
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
                Debug.Log($"{m_debugIndex += 1}. Gatherer going to Ruin.");
                RequestTravelToRuin(requestObj);
                break;
            case Selectable.SelectedObjectType.Obelisk:
                Debug.Log($"{m_debugIndex += 1}. Obelisk selected for command, nothing to do here.");
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
        if (m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }

        m_curRuin = requestObj.GetComponent<RuinController>();
        //Get a path to the ruin.
        //If the path returned is of length 0, we can activate the ruin, else we travel to it.
        m_gathererPath = GetShortestPathToObj(requestObj);
        Debug.Log($"Gatherer Path to ruin is {m_gathererPath.Count} cells long.");
        if (m_gathererPath.Count == 0)
        {
            m_gathererPath = null;

            //Resume returning to castle if carrying resources, else await further instruction.
            if (m_resourceCarried > 1)
            {
                UpdateTask(GathererTask.TravelingToCastle);
            }
            else
            {
                UpdateTask(GathererTask.Idling);
            }

            //Request the level up.
            RequestIncrementGathererLevel(1);
        }
        else
        {
            UpdateTask(GathererTask.TravelingToRuin);
        }

        /*//If the current position is within stopping distance of the ruin...
        float stoppingDistance = 1.41f;
        Vector2Int requestObjPos = Util.GetVector2IntFrom3DPos(requestObj.transform.position);
        float currentDistance = Vector2Int.Distance(m_curCell.m_cellPos, requestObjPos);

        Debug.Log($"Current distance to ruin is {currentDistance}.");
        //If we are far away, path home.
        if (currentDistance > stoppingDistance)
        {
            //Generate a path to the ruin.
            m_gathererPath = GetShortestPathToObj(requestObj);
            Debug.Log($"Gatherer Path to ruin is {m_gathererPath.Count} cells long.");

            //Move to the ruin.
            UpdateTask(GathererTask.TravelingToRuin);
        }
        else
        {
            m_gathererPath = null;

            //Resume returning to castle if carrying resources, else await further instruction.
            if (m_resourceCarried > 1)
            {
                UpdateTask(GathererTask.TravelingToCastle);
            }
            else
            {
                UpdateTask(GathererTask.Idling);
            }

            //Request the level up.
            RequestIncrementGathererLevel(1);
        }*/
    }

    private void RequestedHarvest(GameObject requestObj)
    {
        //Are we next to the node already?
        ResourceNode node = requestObj.GetComponent<ResourceNode>();

        //If this is the node we're already harvesting ignore the command.
        if (node == m_curHarvestNode)
        {
            Debug.Log($"{m_debugIndex += 1}. {gameObject.name} is already harvesting {node}. Ignoring new request.");
            return;
        }

        ClearHarvestVars();
        if (m_curCoroutine != null) //Assumption; if there is an active coroutine, it is Harvesting.
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }

        //Find harvest nodes around the requested resource.
        for (var i = 0; i < node.m_harvestPoints.Count; i++)
        {
            var harvestPoint = node.m_harvestPoints[i];
            if (harvestPoint.m_harvestPointCell == m_curCell && (harvestPoint.m_gatherer == this || harvestPoint.m_gatherer == null))
            {
                //We're in a harvest point cell.
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
            SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);

            UpdateTask(GathererTask.TravelingToHarvest);

            m_curHarvestNode.WasSelected();
        }
    }

    private void UpdateTask(GathererTask newTask)
    {
        Debug.Log($"{m_debugIndex += 1}. {gameObject.name}'s current task: {m_gathererTask} & new task: {newTask}.");
        m_gathererTask = newTask;

        ToggleDisplayIdleVFX();
        m_resourceAnchor.SetActive(m_resourceCarried > 0);

        Vector2Int startPos;
        Vector2Int endPos;
        switch (m_gathererTask)
        {
            case GathererTask.Idling:

                break;
            case GathererTask.FindingHarvestablePoint:
                if (m_curHarvestNode)
                {
                    Debug.Log($"{m_debugIndex += 1}. HarvestNode still active. Finding point to harvest from.");
                    ValueTuple<ResourceNode, Vector2Int, int> vars = GetHarvestPointFromObj(m_curHarvestNode.gameObject);

                    //If we're given an index greater equal to or greater than 0, we can harvest this node.
                    if (vars.Item3 >= 0)
                    {
                        Debug.Log($"{m_debugIndex += 1}. Setting HarvestNode variables (FindingHarvestablePoint)");
                        m_curHarvestNodePos = vars.Item1.transform.position;
                        m_curHarvestNode = vars.Item1;
                        m_curHarvestPointPos = vars.Item2;
                        m_curHarvestPointIndex = vars.Item3;
                        //SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);
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
                    Debug.Log($"{m_debugIndex += 1}. Finding NEW nearby {m_curHarvestNodePos}.");
                    ValueTuple<ResourceNode, Vector2Int, int> vars = GetHarvestPoint(GetHarvestNodes(m_curHarvestNodePos, 3f));
                    if (vars.Item1 == null)
                    {
                        UpdateTask(GathererTask.Idling);
                        break;
                    }

                    SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);
                }

                //Debug.Log($"Moving to {m_curHarvestNode.gameObject.name}");
                //If we're already on the desired cell, do not need to move.
                if (m_curHarvestPointPos != m_curCell.m_cellPos)
                {
                    UpdateTask(GathererTask.TravelingToHarvest);
                }
                else
                {
                    m_gathererPath = null;
                    UpdateTask(GathererTask.Harvesting);
                }

                break;
            case GathererTask.TravelingToHarvest:
                break;
            case GathererTask.Harvesting:
                if (m_curHarvestNode)
                {
                    Debug.Log($"{m_debugIndex += 1}. {gameObject.name} arrived at node and is harvesting.");
                }
                else
                {
                    Debug.Log($"{m_debugIndex += 1}. {gameObject.name} arrived at node and it is missing.");
                }

                m_curCoroutine = StartCoroutine(Harvesting());
                break;
            case GathererTask.TravelingToCastle:

                foreach (Vector2Int pos in m_depositLocations)
                {
                    if (pos == m_curCell.m_cellPos)
                    {
                        m_gathererPath = null;
                        Debug.Log($"{m_debugIndex += 1}. At storage already.");
                        UpdateTask(GathererTask.Storing);
                        return;
                    }
                }

                Debug.Log($"{m_debugIndex += 1}. Finding point to store from.");
                startPos = Util.GetVector2IntFrom3DPos(transform.position);
                m_gathererPath = AStar.FindShortestPath(startPos, m_depositLocations);
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
        bool isIdle = m_gathererTask == GathererTask.Idling;
        m_idleVFXParent.gameObject.SetActive(isIdle);

        if (isIdle)
        {
            RequestAudioLoop(m_gathererData.m_idleClip, m_audioSource);
        }
        else if (m_audioSource.isPlaying)
        {
            m_audioSource.Stop();
        }
    }

    private void SetHarvestVars(ResourceNode harvestNode, Vector2Int harvestPointPos, int harvestPointIndex)
    {
        //Setup this script
        Debug.Log($"{m_debugIndex += 1}. {gameObject.name} Vars set to {harvestNode.gameObject.name} at {harvestPointPos} at index {harvestPointIndex}");
        m_curHarvestNodePos = harvestNode.transform.position;
        m_curHarvestNode = harvestNode;
        m_curHarvestPointPos = harvestPointPos;
        m_curHarvestPointIndex = harvestPointIndex;

        //Sign up to the node.
        m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = this;

        Debug.Log($"{m_debugIndex += 1}. {gameObject.name} subscribing to node {m_curHarvestNode}");
        m_curHarvestNode.OnResourceNodeDepletion += OnNodeDepleted;
    }

    private void ClearHarvestVars()
    {
        //Unassign from the node (used for Player interupting the Harvesting state)
        if (m_curHarvestNode)
        {
            Debug.Log($"{m_debugIndex += 1}. Clearing {m_curHarvestNode.gameObject.name} from {gameObject.name} vars.");
            if (m_gathererTask == GathererTask.Harvesting) m_curHarvestNode.SetIsHarvesting(-1); // We only want to do this if we've started harvesting, which increments the value.
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = null;

            Debug.Log($"{m_debugIndex += 1}. {gameObject.name} un-subscribing to node {m_curHarvestNode}");
            m_curHarvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
        }

        //Unset this script
        m_animator.SetBool(m_isHarvestingHash, false);
        m_curHarvestNode = null;
        Debug.Log($"{m_debugIndex += 1}. Clear vars as set curHarvestNode to null.");
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

        Debug.Log($"{m_debugIndex += 1}. {gameObject.name}'s resource node depleted.");
        ClearHarvestVars();

        //If harvesting, stop cur coroutine.
        //Previous Condition: m_resourceCarried == 0 && m_curCoroutine != null
        //There is a period of time where the gatherer is still in the harvesting state, while it is waiting for resources from the node.
        if (m_gathererTask == GathererTask.Harvesting && m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
            Debug.Log($"{m_debugIndex += 1}. {gameObject.name}'s Harvesting coroutine stopped due to deplete node.");

            UpdateTask(GathererTask.FindingHarvestablePoint);
            return;
        }

        //Interrupt flow if we're not currently carrying or storing resources.
        if (m_gathererTask == GathererTask.TravelingToHarvest)
        {
            UpdateTask(GathererTask.FindingHarvestablePoint);
        }
    }

    private List<ResourceNode> GetHarvestNodes(Vector3 pos, float searchRange)
    {
        LayerMask layerMask = LayerMask.GetMask("Actors");
        List<ResourceNode> nearbyNodes = new List<ResourceNode>();

        //Get a bunch of nodes near the point.
        Collider[] colliders = Physics.OverlapBox(center: pos,
            halfExtents: new Vector3(searchRange * .5f, 1f, searchRange * .5f),
            orientation: Quaternion.identity,
            layerMask: layerMask);

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
                        //Debug.Log($"Nearby Node found {newNode} and added to list.");
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

                if ((node.m_harvestPoints[x].m_gatherer != null && node.m_harvestPoints[x].m_gatherer != this) || cellOccupied)
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

            //If we're already on the point, lets use it.
            if (m_curCell.m_cellPos == node.m_harvestPoints[i].m_harvestPointCell.m_cellPos)
            {
                Debug.Log($"{m_debugIndex += 1}. We're already on the HarvestPoint.");
                m_gathererPath = null;
                return (node, node.m_harvestPoints[i].m_harvestPointPos, i);
            }

            //Check path to the cell, not the Harvest Point Position.
            List<Vector2Int> path = AStar.FindPath(curPos, node.m_harvestPoints[i].m_harvestPointCell.m_cellPos);

            if (path == null)
            {
                continue;
            }

            if (path.Count <= shortestDistance)
            {
                //Debug.Log($"shortest path to harvest point found.");
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

        int storageAmount;
        int random = Random.Range(0, 2);
        storageAmount = m_carryCapacity + ((m_gathererLevel - 1) * random); //Calculate storage based on level. Lvl 1 = 1, Lvl 2 = 1 + 50% * 2
        switch (m_gathererData.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                ResourceManager.Instance.UpdateWoodAmount(storageAmount);

                if (storageAmount > 1) //Did we deposit a crit amount?
                {
                    RequestPlayAudio(m_gathererData.m_critDepositClip, m_audioSource);
                    IngameUIController.Instance.SpawnCritCurrencyAlert(storageAmount, 0, true, transform.position);
                }
                else
                {
                    IngameUIController.Instance.SpawnCurrencyAlert(storageAmount, 0, true, transform.position);
                }

                break;
            case ResourceManager.ResourceType.Stone:
                storageAmount = m_carryCapacity; //To add stone levelng later!
                ResourceManager.Instance.UpdateStoneAmount(storageAmount);
                IngameUIController.Instance.SpawnCurrencyAlert(0, storageAmount, true, transform.position);
                if (storageAmount > 1) RequestPlayAudio(m_gathererData.m_critDepositClip, m_audioSource);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Debug.Log($"{m_debugIndex += 1}. Stored: {m_carryCapacity} + {m_gathererLevel - 1} * {random}.");

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
        m_curCoroutine = null;

        m_animator.SetBool(m_isHarvestingHash, false);

        ValueTuple<int, int> vars = m_curHarvestNode.RequestResource(m_carryCapacity);
        m_resourceCarried = vars.Item1;
        int resourceRemaining = vars.Item2;

        //If there are resources remaining in the node, unset some of the variables on the node.
        if (resourceRemaining > 0)
        {
            m_curHarvestNode.SetIsHarvesting(-1);
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = null;
        }

        Debug.Log($"{m_debugIndex += 1}. {gameObject.name}'s harvesting completed. Now carrying {m_resourceCarried}.");
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