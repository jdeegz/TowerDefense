using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class GathererController : MonoBehaviour
{
    [SerializeField] private ResourceManager.ResourceType m_type;
    [SerializeField] private bool m_isSelected;
    [SerializeField] private float m_harvestDuration;
    [SerializeField] private float m_storingDuration;
    [SerializeField] private int m_carryCapacity;
    private int m_resourceCarried;
    private ResourceNode m_harvestNode;
    private Transform m_harvestPoint;
    private int m_harvestPointIndex;
    private NavMeshAgent m_navMeshAgent;
    private Coroutine m_curCoroutine;
    public Animator m_animator;
    private static int m_isHarvestingHash = Animator.StringToHash("isHarvesting");
    public GathererTask m_gathererTask;

    public enum GathererTask
    {
        Idling,
        FindingHarvestablePoint,
        TravelingToHarvest,
        Harvesting,
        TravelingToCastle,
        Storing,
    }

    private void UpdateTask(GathererTask newTask)
    {
        m_gathererTask = newTask;
        switch (m_gathererTask)
        {
            case GathererTask.Idling:
                break;
            case GathererTask.FindingHarvestablePoint:
                //Get the Node from right clicking a node or from a node being depleted.

                //If no node, go to idle.
                if (!m_harvestNode)
                {
                    //Debug.Log("No harvest node assigned.");
                    //Find a new node.
                    UpdateTask(GathererTask.Idling);
                }

                //Get the point to travel to.
                (m_harvestPoint, m_harvestPointIndex) = GetHarvestPoint(m_harvestNode);

                //If no harvest Point available, go to idle.
                if (!m_harvestPoint || m_harvestPointIndex < 0)
                {
                    //Debug.Log("No harvest point available.");
                    UpdateTask(GathererTask.Idling);
                    break;
                }

                //Assign ourselves to the node's point we are going to us.
                m_harvestNode.m_harvestPoints[m_harvestPointIndex].m_isOccupied = true;
                m_harvestNode.m_harvestPoints[m_harvestPointIndex].m_gatherer = this;
                m_harvestNode.OnResourceNodeDepletion += OnNodeDepleted;
                UpdateTask(GathererTask.TravelingToHarvest);
                break;
            case GathererTask.TravelingToHarvest:
                //NavMesh function, triggers Harvesting when close (uses Update to check proximity).
                StartMoving(m_harvestPoint.position);
                break;
            case GathererTask.Harvesting:
                m_curCoroutine = StartCoroutine(Harvesting());
                //Enumerator to harvest, triggers Travelling to castle.
                break;
            case GathererTask.TravelingToCastle:
                //Disabled this, no need to remove from a destroyed object!
                //Unassign ourselves from the node, opening the spot.
                //m_harvestNode.m_harvestPoints[m_harvestPointIndex].m_isOccupied = false;
                //m_harvestNode.m_harvestPoints[m_harvestPointIndex].m_gatherer = null;

                //Get the point on the castle we want to move to. Triggers Storing when close (uses Update to check proximity).
                Vector3 castle = GetClosestTransform(GameplayManager.Instance.m_enemyGoals).position;
                //Debug.Log("Moving to castle point: " + castle);
                StartMoving(castle);
                break;
            case GathererTask.Storing:
                m_curCoroutine = StartCoroutine(Storing());
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Awake()
    {
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        GameplayManager.OnGameObjectSelected += ObjectSelected;
        GameplayManager.OnCommandRequested += CommandRequested;
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameObjectSelected -= ObjectSelected;
        GameplayManager.OnCommandRequested -= CommandRequested;
        GameplayManager.Instance.RemoveGathererFromList(this, m_type);
    }

    private void CommandRequested(GameObject requestObj)
    {
        if (m_isSelected)
        {
            //Try and get the resource node if there was one clicked on. Check to see if it matches the gatherer type.
            ResourceNode node = requestObj.GetComponent<ResourceNode>();
            if (node != null)
            {
                if (node.m_type == m_type && m_harvestNode != node)
                {
                    m_harvestNode = node;
                    UpdateTask(GathererTask.FindingHarvestablePoint);
                }
            }
            else
            {
                //Debug.Log("Not right Resource Type");
            }
        }
    }
    //Get Node
    //Right clicking sends me an object. Check if it's a node. If it's the right type. Set Harvest Node to the object. Get accessible harvest point.
    //If node is still null, check surrounding area for a node that is valid and has accessible harvest point. Set harvest node to the object.
    private void SetResourceNode()
    {
        float detectionRadius = 1.5f;
        LayerMask layerMask = LayerMask.GetMask("Actors");
        Collider depletedNodeCollider = depletedNode.GetComponent<Collider>();
        List<ResourceNode> nearbyNodes = new List<ResourceNode>();

        //Get a bunch of nodes near the point.
        Collider[] colliders = Physics.OverlapSphere(depletedNode.transform.position, detectionRadius, layerMask);

        foreach (Collider collider in colliders)
        {
            if (collider != null && collider != depletedNodeCollider)
            {
                ResourceNode newNode = collider.GetComponent<ResourceNode>();
                if (newNode != null)
                {
                    if (newNode.m_type == m_type)
                    {
                        nearbyNodes.Add(newNode);
                        //Debug.Log("Added: " + newNode.name);
                    }
                }
            }
        }

        //Get the closest one harvest point out of all of the valid nodes.
        float closestDistance = float.MaxValue;
        ResourceNode closestNode = null;

        for (var i = 0; i < nearbyNodes.Count; ++i)
        {
            var nearbyNode = nearbyNodes[i];
            Transform nodesClosestTransform = null;
            int nodesClosestTransformIndex = -1;
            (nodesClosestTransform, nodesClosestTransformIndex) = GetHarvestPoint(nearbyNode);

            float distance = Vector3.Distance(nodesClosestTransform.position, transform.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestNode = nearbyNode;
                m_harvestPoint = nodesClosestTransform;
                m_harvestPointIndex = nodesClosestTransformIndex;
            }
        }
        if (closestNode)
        {
            m_harvestNode = closestNode;
        }
        else
        {
            m_harvestNode = null;
        }
    }
    
    private void OnNodeDepleted(ResourceNode depletedNode)
    {
        StopHarvesting();
        //Save Node's position.
        //Set Node, Harvest Point, and Harvest Point Index to null/0
        //Go to FindGatheringPoint state to find a new node and point.
        
        //UpdateTask(GathererTask.Idling);
        float detectionRadius = 1.5f;
        LayerMask layerMask = LayerMask.GetMask("Actors");
        Collider depletedNodeCollider = depletedNode.GetComponent<Collider>();
        List<ResourceNode> nearbyNodes = new List<ResourceNode>();

        //Get a bunch of nodes near the point.
        Collider[] colliders = Physics.OverlapSphere(depletedNode.transform.position, detectionRadius, layerMask);

        foreach (Collider collider in colliders)
        {
            if (collider != null && collider != depletedNodeCollider)
            {
                ResourceNode newNode = collider.GetComponent<ResourceNode>();
                if (newNode != null)
                {
                    if (newNode.m_type == m_type)
                    {
                        nearbyNodes.Add(newNode);
                        //Debug.Log("Added: " + newNode.name);
                    }
                }
            }
        }

        //Get the closest one harvest point out of all of the valid nodes.
        float closestDistance = float.MaxValue;
        ResourceNode closestNode = null;

        for (var i = 0; i < nearbyNodes.Count; ++i)
        {
            var nearbyNode = nearbyNodes[i];
            Transform nodesClosestTransform = null;
            int nodesClosestTransformIndex = -1;
            (nodesClosestTransform, nodesClosestTransformIndex) = GetHarvestPoint(nearbyNode);

            float distance = Vector3.Distance(nodesClosestTransform.position, transform.position);
            if (distance <= closestDistance)
            {
                closestDistance = distance;
                closestNode = nearbyNode;
                m_harvestPoint = nodesClosestTransform;
                m_harvestPointIndex = nodesClosestTransformIndex;
            }
        }

        //If we're given a node, unsub from previous node, assign the new node, then sub the new node, else set node to null.
        if (closestNode)
        {
            //m_harvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
            m_harvestNode = closestNode;
            m_harvestNode.m_harvestPoints[m_harvestPointIndex].m_isOccupied = true;
            m_harvestNode.m_harvestPoints[m_harvestPointIndex].m_gatherer = this;
            m_harvestNode.OnResourceNodeDepletion += OnNodeDepleted;
            if (m_gathererTask == GathererTask.Harvesting)
            {
                UpdateTask(GathererTask.TravelingToHarvest);
            }
        }
        else
        {
            //m_harvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
            m_harvestNode = null;
            //UpdateTask(GathererTask.Idling);
            //Vector3 castle = GetClosestTransform(GameplayManager.Instance.m_enemyGoals).position;
            //StartMoving(castle);
        }
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
            /*// Check if the closest point is within the specified distance
            if (Vector3.Distance(pos, closestPoint) <= maxDistance)
            {
                // Find the closest point on the NavMesh's edge
                NavMesh.FindClosestEdge(closestPoint, out navMeshHit, NavMesh.AllAreas);

                // Check if the closest edge is within the specified distance
                if (Vector3.Distance(closestPoint, navMeshHit.position) <= maxDistance)
                {
                    // Calculate the path
                    m_navMeshAgent.CalculatePath(closestPoint, path);
                    canPath = path.status == NavMeshPathStatus.PathComplete;
                }
            }*/
        }

        return canPath;
    }

    private (Transform, int) GetHarvestPoint(ResourceNode node)
    {
        float closestDistance = float.MaxValue;
        Transform closestTransform = null;
        int closestTransformIndex = -1;

        for (int i = 0; i < node.m_harvestPoints.Count; ++i)
        {
            Transform harvestPointTransform = node.m_harvestPoints[i].m_transform;
            //If it's occupied by another gatherer, go next.
            if (node.m_harvestPoints[i].m_isOccupied)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, harvestPointTransform.position);

            if (CanPath(harvestPointTransform.position) && distance < closestDistance)
            {
                closestDistance = distance;
                closestTransform = harvestPointTransform;
                closestTransformIndex = i;
            }
        }

        if (closestTransform != null)
        {
            //Debug.Log("Closest transform : " + closestTransform.name + " and Closest Index: " + closestTransformIndex);
        }
        else
        {
            //Debug.Log("No valid point found.");
        }

        return (closestTransform, closestTransformIndex);
    }

    private void StartMoving(Vector3 pos)
    {
        m_navMeshAgent.SetDestination(pos);
    }

    private void ObjectSelected(GameObject selectedObj)
    {
        m_isSelected = selectedObj == gameObject;
    }

    void Start()
    {
        switch (m_type)
        {
            case ResourceManager.ResourceType.Wood:
                GameplayManager.Instance.AddGathererToList(this, m_type);
                ResourceManager.Instance.UpdateWoodGathererAmount(1);
                break;
            case ResourceManager.ResourceType.Stone:
                GameplayManager.Instance.AddGathererToList(this, m_type);
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

    private IEnumerator Harvesting()
    {
        StartHarvesting();
        yield return new WaitForSeconds(m_harvestDuration);
        CompletedHarvest();
    }

    private void StopHarvesting()
    {
        if (m_gathererTask != GathererTask.Harvesting)
        {
            return;
        }
        if (m_curCoroutine != null)
        {
            StopCoroutine(m_curCoroutine);
            m_curCoroutine = null;
        }

        m_animator.SetBool(m_isHarvestingHash, false);
        
        //Since node is depleted, we dont need to talk to it anymore.
        //m_harvestNode.SetIsHarvesting(-1);
        
    }

    private void StartHarvesting()
    {
        m_animator.SetBool(m_isHarvestingHash, true);
        m_harvestNode.SetIsHarvesting(1);
    }

    private void CompletedHarvest()
    {
        m_harvestNode.SetIsHarvesting(-1);
        m_resourceCarried = m_harvestNode.RequestResource(m_carryCapacity);
        m_animator.SetBool(m_isHarvestingHash, false);
        UpdateTask(GathererTask.TravelingToCastle);
        Debug.Log(gameObject.name + " has finished harvesting and -1'd the node: " + m_harvestNode.name);
        m_curCoroutine = null;
    }

    private IEnumerator Storing()
    {
        yield return new WaitForSeconds(m_storingDuration);
        switch (m_type)
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

        if (m_harvestNode)
        {
            UpdateTask(GathererTask.FindingHarvestablePoint);
        }
        else
        {
            UpdateTask(GathererTask.Idling);
        }
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