using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GathererController : MonoBehaviour
{
    [SerializeField] private ScriptableGatherer m_gatherer;
    public GathererTask m_gathererTask;
    public Animator m_animator;

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
    private int m_resourceCarried;
    private int m_curHarvestPointIndex;
    private Vector3 m_curHarvestNodePos;
    private ResourceNode m_curHarvestNode;
    private Transform m_curHarvestPoint;
    private NavMeshAgent m_navMeshAgent;
    private Coroutine m_curCoroutine;

    private static int m_isHarvestingHash = Animator.StringToHash("isHarvesting");

    private void Awake()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        GameplayManager.OnGameObjectSelected += GathererSelected;
        GameplayManager.OnCommandRequested += RequestedHarvest;
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameObjectSelected -= GathererSelected;
        GameplayManager.OnCommandRequested -= RequestedHarvest;
        GameplayManager.Instance.RemoveGathererFromList(this, m_gatherer.m_type);
    }

    void Start()
    {
        switch (m_gatherer.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                GameplayManager.Instance.AddGathererToList(this, m_gatherer.m_type);
                ResourceManager.Instance.UpdateWoodGathererAmount(1);
                break;
            case ResourceManager.ResourceType.Stone:
                GameplayManager.Instance.AddGathererToList(this, m_gatherer.m_type);
                ResourceManager.Instance.UpdateStoneGathererAmount(1);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void Update()
    {
        if (m_gathererTask == GathererTask.TravelingToHarvest && !m_navMeshAgent.pathPending &&
            m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)
        {
            UpdateTask(GathererTask.Harvesting);
        }

        if (m_gathererTask == GathererTask.TravelingToCastle && !m_navMeshAgent.pathPending &&
            m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)
        {
            UpdateTask(GathererTask.Storing);
        }
    }

    private void GathererSelected(GameObject selectedObj)
    {
        m_isSelected = selectedObj == gameObject;
    }

    private void RequestedHarvest(GameObject requestObj)
    {
        if (m_isSelected)
        {
            //If i am currently harvesting, clear variables, clear coroutine.
            if (m_gathererTask == GathererTask.Harvesting)
            {
                ClearHarvestVars();
            }
            
            //Find and set variables for this script.
            ValueTuple<ResourceNode, Transform, int> vars = GetHarvestPoint(GetHarvestNodes(requestObj.transform.position, 0.2f));
            SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);

            if (m_curHarvestNode)
            {
                UpdateTask(GathererTask.TravelingToHarvest);
            }
        }
    }

    private void UpdateTask(GathererTask newTask)
    {
        m_gathererTask = newTask;
        switch (m_gathererTask)
        {
            case GathererTask.Idling:
                break;
            case GathererTask.TravelingToHarvest:
                StartMoving(m_curHarvestPoint.position);
                break;
            case GathererTask.Harvesting:
                m_curCoroutine = StartCoroutine(Harvesting());
                break;
            case GathererTask.TravelingToCastle:
                StartMoving(GetClosestTransform(GameplayManager.Instance.m_enemyGoals).position);
                break;
            case GathererTask.Storing:
                m_curCoroutine = StartCoroutine(Storing());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void StartMoving(Vector3 pos)
    {
        m_navMeshAgent.SetDestination(pos);
    }

    private void SetHarvestVars(ResourceNode harvestNode, Transform harvestPoint, int harvestPointIndex)
    {
        //Setup this script
        m_curHarvestNodePos = harvestNode.transform.position;
        m_curHarvestNode = harvestNode;
        m_curHarvestPoint = harvestPoint;
        m_curHarvestPointIndex = harvestPointIndex;

        //Sign up to the node.
        m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_isOccupied = true;
        m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = this;
        m_curHarvestNode.OnResourceNodeDepletion += OnNodeDepleted;
    }

    private void ClearHarvestVars()
    {
        Debug.Log(gameObject.name + " is clearing vars.");
        //Unassign from the node (used for Player interupting the Harvesting state)
        if (m_curHarvestNode)
        {
            m_curHarvestNode.SetIsHarvesting(-1);
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_isOccupied = false;
            m_curHarvestNode.m_harvestPoints[m_curHarvestPointIndex].m_gatherer = null;
        }

        //Unset this script
        m_animator.SetBool(m_isHarvestingHash, false);
        m_curHarvestNode = null;
        m_curHarvestPoint = null;
        m_curHarvestPointIndex = -1;
        Debug.Log(m_curHarvestNode + " and " + m_curHarvestPoint + " and " + m_curHarvestPointIndex);
    }

    private void OnNodeDepleted(ResourceNode obj)
    {
        Debug.Log(m_curHarvestNode + " has depleted.");
        //Clear current variables.
        ClearHarvestVars();
        //If harvesting, stop cur coroutine.
        if (m_gathererTask == GathererTask.Harvesting)
        {
            StopCoroutine(m_curCoroutine);
        }

        //Set new variables.
        ValueTuple<ResourceNode, Transform, int> vars = GetHarvestPoint(GetHarvestNodes(m_curHarvestNodePos, 2f));
        SetHarvestVars(vars.Item1, vars.Item2, vars.Item3);

        //If harvesting, Start TravelingToHarvest.
        if (m_gathererTask == GathererTask.Harvesting)
        {
            UpdateTask(GathererTask.TravelingToHarvest);
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
                    if (newNode.m_type == m_gatherer.m_type)
                    {
                        nearbyNodes.Add(newNode);
                    }
                }
            }
        }

        return nearbyNodes;
    }

    private (ResourceNode, Transform, int) GetHarvestPoint(List<ResourceNode> nodes)
    {
        float closestDistance = float.MaxValue;
        ResourceNode closestNode = null;
        Transform closestNodePointTransform = null;
        int closestNodePointIndex = -1;

        for (var i = 0; i < nodes.Count; ++i)
        {
            ResourceNode node = nodes[i];

            for (int x = 0; x < node.m_harvestPoints.Count; ++x)
            {
                //If it's occupied by another gatherer, go next.
                if (node.m_harvestPoints[x].m_isOccupied)
                {
                    continue;
                }

                Transform harvestPointTransform = node.m_harvestPoints[x].m_transform;
                float distance = Vector3.Distance(transform.position, harvestPointTransform.position);

                if (CanPath(harvestPointTransform.position) && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = node;
                    closestNodePointTransform = harvestPointTransform;
                    closestNodePointIndex = x;
                }
            }
        }

        return (closestNode, closestNodePointTransform, closestNodePointIndex);
    }

    private bool CanPath(Vector3 pos)
    {
        bool canPath = false;
        NavMeshPath path = new NavMeshPath();
        float maxDistance = 1f;

        // Sample the position to find the closest point on the NavMesh
        NavMeshHit navMeshHit;
        if (NavMesh.SamplePosition(pos, out navMeshHit, maxDistance, NavMesh.AllAreas))
        {
            // Use the closest point as the destination for path calculation
            Vector3 closestPoint = navMeshHit.position;
            if (Vector3.Distance(closestPoint, navMeshHit.position) <= maxDistance)
            {
                // Calculate the path
                m_navMeshAgent.CalculatePath(closestPoint, path);
                canPath = path.status == NavMeshPathStatus.PathComplete;
            }
        }

        return canPath;
    }

    private IEnumerator Harvesting()
    {
        StartHarvesting();
        yield return new WaitForSeconds(m_gatherer.m_harvestDuration);
        CompletedHarvest();
    }

    private IEnumerator Storing()
    {
        yield return new WaitForSeconds(m_gatherer.m_storingDuration);
        switch (m_gatherer.m_type)
        {
            case ResourceManager.ResourceType.Wood:
                ResourceManager.Instance.UpdateWoodAmount(m_resourceCarried);
                break;
            case ResourceManager.ResourceType.Stone:
                ResourceManager.Instance.UpdateStoneAmount(m_resourceCarried);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        m_resourceCarried = 0;

        if (m_curHarvestNode)
        {
            UpdateTask(GathererTask.TravelingToHarvest);
        }
        else
        {
            UpdateTask(GathererTask.Idling);
        }
    }

    private void StartHarvesting()
    {
        m_animator.SetBool(m_isHarvestingHash, true);
        m_curHarvestNode.SetIsHarvesting(1);
    }

    private void CompletedHarvest()
    {
        m_curHarvestNode.SetIsHarvesting(-1);
        m_resourceCarried = m_curHarvestNode.RequestResource(m_gatherer.m_carryCapacity);
        m_animator.SetBool(m_isHarvestingHash, false);
        UpdateTask(GathererTask.TravelingToCastle);
        Debug.Log(gameObject.name + " has finished harvesting and -1'd the node: " + m_curHarvestNode.name);
        m_curCoroutine = null;
    }

    private Transform GetClosestTransform(Transform[] transforms)
    {
        Transform closestTransform = null;
        float closestDistance = Mathf.Infinity;
        Vector3 curPos = transform.position;

        foreach (Transform t in transforms)
        {
            float distance = Vector3.Distance(t.position, curPos);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTransform = t;
            }
        }

        return closestTransform;
    }
}