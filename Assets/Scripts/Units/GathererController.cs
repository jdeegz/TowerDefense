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
    private NavMeshAgent m_navMeshAgent;

    public GathererTask m_gathererTask;

    public enum GathererTask
    {
        Idling,
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
            case GathererTask.TravelingToHarvest:
                if (!m_harvestNode)
                {
                    UpdateTask(GathererTask.Idling);
                }
                StartMoving(m_harvestNode.transform.position);
                break;
            case GathererTask.Harvesting:
                break;
            case GathererTask.TravelingToCastle:
                Vector3 castle = GetClosestTransform(GameplayManager.Instance.m_enemyGoals).position;
                StartMoving(castle);
                break;
            case GathererTask.Storing:
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
                if (node.m_type == m_type && CanPath(requestObj.transform.position))
                {
                    if (node != null)
                    {
                        node.OnResourceNodeDepletion -= OnNodeDepleted;
                    }

                    m_harvestNode = node;
                    node.OnResourceNodeDepletion += OnNodeDepleted;
                    UpdateTask(GathererTask.TravelingToHarvest);
                }
            }
            else
            {
                Debug.Log("Cannot Path or Not right Resource Type");
            }
        }
    }

    private void OnNodeDepleted(ResourceNode depletedNode)
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
                        Debug.Log("Added: " + newNode.name);
                    }
                }
            }
        }

        //Get the closest one.
        float closestDistance = float.MaxValue;
        ResourceNode closestNode = null;
        foreach (ResourceNode nearbyNode in nearbyNodes)
        {
            //Compare the previous Node Position with the new ones.
            float distance = Vector3.Distance(depletedNode.transform.position, nearbyNode.transform.position);

            if (CanPath(nearbyNode.transform.position) && distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = nearbyNode;
            }
        }

        //If we're given a node, unsub from previous node, assign the new node, then sub the new node, else set node to null.
        if (closestNode)
        {
            m_harvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
            m_harvestNode = closestNode;
            m_harvestNode.OnResourceNodeDepletion += OnNodeDepleted;
        }
        else
        {
            m_harvestNode.OnResourceNodeDepletion -= OnNodeDepleted;
            m_harvestNode = null;
        }

        //If we're in the middle of traveling to harvest, update the task. 
        if (m_gathererTask == GathererTask.TravelingToHarvest && m_harvestNode)
        {
            StartMoving(m_harvestNode.transform.position);
        }

        //If we dont have a harvest node, return to the castle and be idle.
        if (!m_harvestNode)
        {
            UpdateTask(GathererTask.Idling);
            Vector3 castle = GetClosestTransform(GameplayManager.Instance.m_enemyGoals).position;
            StartMoving(castle);
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

            // Check if the closest point is within the specified distance
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
            }
        }

        return canPath;
    }

    private void StartMoving(Vector3 pos)
    {
        m_navMeshAgent.SetDestination(pos);
    }

    private void ObjectSelected(GameObject selectedObj)
    {
        m_isSelected = selectedObj == gameObject;
    }

    // Start is called before the first frame update
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

    // Update is called once per frame
    void Update()
    {
        if (m_gathererTask == GathererTask.TravelingToHarvest && !m_navMeshAgent.pathPending &&
            m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)
        {
            UpdateTask(GathererTask.Harvesting);
            StartCoroutine(Harvesting());
        }

        if (m_gathererTask == GathererTask.TravelingToCastle && !m_navMeshAgent.pathPending &&
            m_navMeshAgent.remainingDistance <= m_navMeshAgent.stoppingDistance)
        {
            UpdateTask(GathererTask.Storing);
            StartCoroutine(Storing());
        }
    }

    private IEnumerator Harvesting()
    {
        yield return new WaitForSeconds(m_harvestDuration);
        int i = m_harvestNode.RequestResource(m_carryCapacity);
        m_resourceCarried = i;

        UpdateTask(GathererTask.TravelingToCastle);
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
            UpdateTask(GathererTask.TravelingToHarvest);
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