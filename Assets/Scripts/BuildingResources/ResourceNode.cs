using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ResourceNode : MonoBehaviour, IResourceNode
{
    public ResourceNodeData m_nodeData;
    [SerializeField] private GameObject m_modelRoot;
    [SerializeField] private GameObject m_treeBurnedVFX;
    [SerializeField] private GameObject m_treeShedVFX;
    [SerializeField] private GameObject m_treeFelledVFX;
    [SerializeField] private List<GameObject> m_objectsToToggle; // Objects enabled if count > 1
    [SerializeField] private List<GameObject> m_resourceCountMaximumObjs; // Objects enabled if count == maximum

    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private List<AudioClip> m_woodHarvestedClips;
    [SerializeField] private List<AudioClip> m_woodDepletedClips;

    private int m_resourcesRemaining;
    private float m_refreshResourceTimeElapsed;

    [HideInInspector] public ResourceManager.ResourceType m_type;
    public List<HarvestPoint> m_harvestPoints;
    public event Action<ResourceNode> OnResourceNodeDepletion;
    private Quaternion m_treeRotation;
    private static int m_gatherersHarvestingHash = Animator.StringToHash("gatherersHarvesting");

    void Awake()
    {
        m_resourcesRemaining = m_nodeData.m_maxResources;
        m_type = m_nodeData.m_resourceType;
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        m_treeRotation = transform.rotation; // Used for harvesting animations.
        RandomResourceAmount();
    }

    private void RandomResourceAmount()
    {
        int randomInt = Random.Range(0, 20);
        if (randomInt == 1)
        {
            m_resourcesRemaining = 1;
            if (m_resourcesRemaining == 1 && m_objectsToToggle.Count > 0)
            {
                foreach (GameObject obj in m_objectsToToggle)
                {
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }
    }

    void Update()
    {
        if (m_nodeData.m_refreshResources && m_resourcesRemaining < m_nodeData.m_maxResources)
        {
            m_refreshResourceTimeElapsed += Time.deltaTime;

            if (m_refreshResourceTimeElapsed >= m_nodeData.m_refreshRate)
            {
                m_resourcesRemaining = Math.Min(m_resourcesRemaining + m_nodeData.m_refreshQuantity, m_nodeData.m_maxResources);
                UpdateResourceDisplayState();
                m_refreshResourceTimeElapsed = 0;
            }
        }
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1, this);
        }
    }

    public (int, int) RequestResource(int i)
    {
        int resourcesHarvested = 0;
        if (m_resourcesRemaining >= 1)
        {
            //Give the gatherer how much they ask for or all that is remaining.
            resourcesHarvested = Math.Min(i, m_resourcesRemaining);
            m_resourcesRemaining -= resourcesHarvested;

            if (!m_nodeData.m_rewardsResources) resourcesHarvested = 0; // If the tree does not award resources, cancel out the grant.
            ObjectPoolManager.SpawnObject(m_treeShedVFX, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
        }

        m_audioSource.PlayOneShot(Util.GetRandomElement(m_woodHarvestedClips));
        
        UpdateResourceDisplayState();

        if (m_resourcesRemaining <= 0)
        {
            //If we hit 0 resources after giving some up, send the gatherer nearby nodes and start the destroy process.
            int index = Random.Range(0, m_woodDepletedClips.Count);
            m_audioSource.PlayOneShot(m_woodDepletedClips[index]);
            OnDepletion(true);
        }

        
        
        return (resourcesHarvested, m_resourcesRemaining);
    }

    void UpdateResourceDisplayState()
    {
        foreach (GameObject obj in m_objectsToToggle)
        {
            obj.SetActive(m_resourcesRemaining > 1);
        }
        
        foreach (GameObject obj in m_resourceCountMaximumObjs)
        {
            obj.SetActive(m_resourcesRemaining == m_nodeData.m_maxResources);
        }
    }

    public void RequestPlayAudioClip(AudioClip clip)
    {
        if (clip == null) return;

        m_audioSource.PlayOneShot(clip);
    }

    public bool HasResources()
    {
        return m_resourcesRemaining > 0;
    }

    public void WasSelected()
    {
        //m_animator.SetTrigger("isSelected");
    }

    public void OnDepletion(bool harvested)
    {
        if (harvested){ OnResourceNodeDepletion?.Invoke(this); } // dont invoke if we were destroyed by something else.
        
        if (!m_nodeData.m_limitedCount) return; // a node with a limited count should be removed below.

        GridCellOccupantUtil.SetOccupant(gameObject, false, 1, 1, this);

        //Setting this to 0 so it wont show up in Nearby Nodes check. (When dragon destroys node, the node was appearing in the FindNearbyNodes check)
        m_resourcesRemaining = 0;

        if (!harvested)
        {
            ObjectPoolManager.SpawnObject(m_treeBurnedVFX, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
            Destroy(gameObject);
        }
        else
        {
            RequestFelledRotation();
        }
    }

    public void CreateResourceNode()
    {
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1, this);
    }

    public void RemoveResourceNodeImmediate()
    {
        GridCellOccupantUtil.SetOccupant(gameObject, false, 1, 1, this);
        Destroy(gameObject);
        Debug.Log($"destroying tree.");
    }

    private Tween m_curContactTween;
    private Sequence m_curContactSequence;
    private Transform m_curGathererTransform;

    public void RequestContactRotation(Transform gathererTransform)
    {
        if (m_curContactSequence != null && m_curContactSequence.IsActive())
        {
            Debug.Log("Rotation already in progress!");
            return;
        }

        // Collect and assign Rotations
        m_curGathererTransform = gathererTransform;
        Quaternion offset = Quaternion.AngleAxis(8f, m_curGathererTransform.right);
        Quaternion targetRotation = offset * m_treeRotation;

        // Build and fire the Sequence
        m_curContactSequence = DOTween.Sequence();
        m_curContactSequence.Append(transform.DORotateQuaternion(targetRotation, 0.075f))
            .Append(transform.DORotateQuaternion(m_treeRotation, 0.1f))
            .OnComplete(() => { m_curContactSequence = null; });
    }

    public void RequestFelledRotation()
    {
        gameObject.GetComponent<Collider>().enabled = false;

        // Collect and assign Rotations
        Quaternion offset = Quaternion.AngleAxis(90f, m_curGathererTransform.right);
        Quaternion targetRotation = offset * m_treeRotation;

        // Build and fire the Sequence
        m_curContactSequence = DOTween.Sequence();
        m_curContactSequence.Append(transform.DORotateQuaternion(targetRotation, 1f).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                ObjectPoolManager.SpawnObject(m_treeFelledVFX, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
                m_curContactSequence = null;
                m_modelRoot.SetActive(false);
                Destroy(gameObject, 2f);
            });
    }

    public void RequestPlayAudio(AudioClip clip)
    {
        //source.Stop();
        m_audioSource.PlayOneShot(clip);
    }

    public void RequestPlayAudio(List<AudioClip> clips)
    {
        int i = Random.Range(0, clips.Count);
        m_audioSource.PlayOneShot(clips[i]);
    }


    public ResourceNodeTooltipData GetTooltipData()
    {
        ResourceNodeTooltipData data = new ResourceNodeTooltipData();
        data.m_resourceType = m_nodeData.m_resourceType;
        data.m_resourceNodeName = m_nodeData.m_resourceNodeName;
        data.m_resourceNodeDescription = m_nodeData.m_resourceNodeDescription;
        data.m_maxResources = m_nodeData.m_maxResources;
        data.m_curResources = m_resourcesRemaining;
        return data;
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("ForestRemover"))
        {
            OnDepletion(false);
        }
    }
}

[Serializable]
public class HarvestPoint
{
    public Vector2Int m_harvestPointPos;

    public Cell m_harvestPointCell;

    //public bool m_isOccupied;
    public GathererController m_gatherer;
}

public class ResourceNodeTooltipData
{
    public ResourceManager.ResourceType m_resourceType;
    public string m_resourceNodeName;
    public string m_resourceNodeDescription;
    public int m_maxResources;
    public int m_curResources;
}