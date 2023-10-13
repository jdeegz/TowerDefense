using System;
using System.Collections;
using System.Collections.Generic;
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
    
    [Header("Audio")]
    [SerializeField] private List<AudioClip> m_woodChopClips;
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
    private NavMeshAgent m_navMeshAgent;
    private Coroutine m_curCoroutine;

    private static int m_isHarvestingHash = Animator.StringToHash("isHarvesting");

    private void Awake()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        GameplayManager.OnGameObjectSelected += GathererSelected;
        GameplayManager.OnCommandRequested += CommandRequested;
        m_audioSource = GetComponent<AudioSource>();
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
            m_navMeshAgent.enabled = true;
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
        if (m_isMoving && m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance && !m_navMeshAgent.pathPending)
        {
            m_isMoving = false;

            switch (m_gathererTask)
            {
                case GathererTask.Idling:
                    ToggleDisplayIdleVFX();
                    break;
                case GathererTask.FindingHarvestablePoint:
                    break;
                case GathererTask.TravelingToHarvest:
                    UpdateTask(GathererTask.Harvesting);
                    break;
                case GathererTask.Harvesting:
                    break;
                case GathererTask.TravelingToCastle:
                    UpdateTask(GathererTask.Storing);
                    break;
                case GathererTask.Storing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        Vector2Int newPos = new Vector2Int((int)Mathf.Floor(transform.position.x + 0.5f), (int)Mathf.Floor(transform.position.z + 0.5f));
        
        if(newPos != m_curPos){
            m_curCell.UpdateActorCount(-1, gameObject.name);
            m_curPos = newPos;
            m_curCell = Util.GetCellFromPos(m_curPos);
            m_curCell.UpdateActorCount(1, gameObject.name);
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
        
        Debug.Log($"{gameObject.name}'s task updated to: {m_gathererTask}");
        switch (m_gathererTask)
        {
            case GathererTask.Idling:
                Vector3 idlePos = GameplayManager.Instance.m_castleController.gameObject.transform.position;
                idlePos = new Vector3(idlePos.x, 0, idlePos.z-2f);
                StartMoving(idlePos);
                break;
            case GathererTask.FindingHarvestablePoint:
                if (m_curHarvestNode)
                {
                    Debug.Log("Finding point to harvest from.");
                    ValueTuple<ResourceNode, Vector3, int> vars = GetHarvestPointFromObj(m_curHarvestNode.gameObject);
                    SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);
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
                StartMoving(m_curHarvestPointPos);
                break;
            case GathererTask.Harvesting:
                m_curCoroutine = StartCoroutine(Harvesting());
                break;
            case GathererTask.TravelingToCastle:
                StartMoving(GameplayManager.Instance.m_enemyGoal.position);
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

    private void StartMoving(Vector3 pos)
    {
        m_isMoving = true;
        ToggleDisplayIdleVFX();
        m_navMeshAgent.SetDestination(pos);
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
        if (m_resourceCarried == 0 && m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            Debug.Log($"{gameObject.name}'s Harvesting coroutine stopped.");
            m_curCoroutine = null;
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

                //Check path to the cell, not the Harvest Point Position.
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
            }
        }

        return (node, closestNodePointPos, harvestPointIndex);
    }

    private IEnumerator Harvesting()
    {
        StartHarvesting();
        yield return new WaitForSeconds(m_GathererData.m_harvestDuration);
        CompletedHarvest();
    }

    private IEnumerator Storing()
    {
        yield return new WaitForSeconds(m_GathererData.m_storingDuration);
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
        ValueTuple<int, int> vars = m_curHarvestNode.RequestResource(m_GathererData.m_carryCapacity);
        m_resourceCarried = vars.Item1;
        int resourceRemaining = vars.Item2;

        if (resourceRemaining > 0 && m_resourceCarried > 0)
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

    private float CalculatePathLength(Vector3 targetPosition)
    {
        // Calculate the length of the path to the specified target position.
        NavMeshPath path = new NavMeshPath();
        if (m_navMeshAgent.CalculatePath(targetPosition, path))
        {
            float pathLength = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                pathLength += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }

            return pathLength;
        }
        else
        {
            return Mathf.Infinity; // Return a large value if path calculation fails.
        }
    }
}