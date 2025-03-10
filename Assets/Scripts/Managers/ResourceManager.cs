using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TechnoBabelGames;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class ResourceManager : MonoBehaviour
{
    public enum ResourceType
    {
        Wood,
        Stone
    }

    private static ResourceType m_resourceType;
    public static ResourceManager Instance;
    private static int m_stoneBank = 0;
    private static int m_woodBank = 0;
    private static int m_stoneGathererCount = 0;
    private static int m_woodGathererCount = 0;

    public static event Action<int> UpdateStoneGathererCount;
    public static event Action<int> UpdateWoodGathererCount;

    public static event Action<int, int> UpdateWoodBank;
    public static event Action<float> UpdateWoodRate;
    public static event Action<int, int> UpdateStoneBank;

    public List<GathererLineRendererObject> m_gathererLineRendererObjs;
    public Material m_gathererPathMaterial;
    public GameObject m_gathererTargetObj;

    [Header("RequestRuinIndicator Data")]
    public ResourceManagerData m_resourceManagerData;
    public List<RuinController> m_ruinsInMission;
    public List<RuinController> m_validRuinsInMission;

    [Header("Tree Resource Node Prefabs")]
    public List<GameObject> m_treePrefabs;

    private List<ResourceNode> m_treesInScene;

    private int m_badLuckChargeCounter;
    private int m_foundRuinCounter;
    private int m_depletionCounter;

    //each time wood is deposited, add quantity and timestamp to a list.
    //When a request to update the gpm display, collect all of the items in the list, within the last 60 seconds.
    //Sum the quantities in the list and divide by 60.
    private float m_depositTimer;
    private List<WoodDeposit> m_woodDeposits;
    private float m_woodPerMinute;
    private int m_ruinIndicatedCount;


    private void Awake()
    {
        //Debug.Log("RESOURCE MANAGER AWAKE");
        Instance = this;
        m_stoneBank = 0;
        m_woodBank = 0;
        m_stoneGathererCount = 0;
        m_woodGathererCount = 0;
        UpdateWoodAmount(m_resourceManagerData.m_startingWood);
        UpdateStoneAmount(m_resourceManagerData.m_startingStone);
        m_woodDeposits = new List<WoodDeposit>();
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            SetupRuinIndications();
        }
    }

    void Start()
    {
        
    }

    public void AddGathererLineRenderer(GathererController gatherer)
    {
        if (m_gathererLineRendererObjs == null)
        {
            m_gathererLineRendererObjs = new List<GathererLineRendererObject>();
        }

        m_gathererLineRendererObjs.Add(new GathererLineRendererObject(gatherer));
    }

    void Update()
    {
        /*if (m_gathererLineRendererObjs == null) return; // Dont do this in the TEST WORLD.
        foreach (GathererLineRendererObject line in m_gathererLineRendererObjs)
        {
            if (line.m_lineRenderer.positionCount > 1)
            {
                line.m_lineRenderer.SetPosition(0, line.m_gatherer.transform.position);

                if (Vector3.Distance(line.m_gatherer.transform.position, line.m_lineRenderer.GetPosition(1)) <= 0.71f)
                {
                    // If we're next to the next index, update the list by removing that position.
                    line.UpdateLineRendererProgress();
                }
            }
        }*/
    }

    public void UpdateWoodAmount(int amount, GathererController gatherer = null)
    {
        m_woodBank += amount;
        UpdateWoodBank?.Invoke(m_woodBank, amount);
        //Debug.Log("BANK UPDATED");

        if (gatherer != null && amount > 0) //Check if this is a deposit and not a sell/build/upgrade
        {
            EconomyLogging.Instance.AddToIncomeThisWave(amount);
            WoodDeposit newDeposit = new WoodDeposit();
            newDeposit.m_quantity = amount;
            newDeposit.m_timeStamp = m_depositTimer;
            m_woodDeposits.Add(newDeposit);
            CalculateWoodRate();
        }
    }

    private void CalculateWoodRate()
    {
        float currentTime = m_depositTimer;
        int woodSum = 0;
        float factor;
        float firstDepositThisMinute = 0;
        bool minimumTimeMet = false;

        for (int i = m_woodDeposits.Count - 1; i >= 0; --i)
        {
            if (m_woodDeposits[i].m_timeStamp >= currentTime - 60)
            {
                woodSum += m_woodDeposits[i].m_quantity;
                firstDepositThisMinute = m_woodDeposits[i].m_timeStamp;
                //last time
            }
            else
            {
                minimumTimeMet = true;
                break;
            }
        }

        if (minimumTimeMet)
        {
            factor = (60 - (currentTime - 60 - firstDepositThisMinute)) / 60;
            m_woodPerMinute = woodSum * factor;
        }
        else
        {
            //How long has it been? 30s, 60 / 30 = 2, multiply sum by 2 to get estimated WPM.
            factor = 60 / currentTime;
            m_woodPerMinute = woodSum * factor;
        }

        UpdateWoodRate?.Invoke(m_woodPerMinute);
    }

    public void UpdateStoneAmount(int amount)
    {
        m_stoneBank += amount;
        UpdateStoneBank?.Invoke(m_stoneBank, amount);
    }
    
    public void SetStoneAmount(int amount)
    {
        m_stoneBank = amount;
        UpdateStoneBank?.Invoke(m_stoneBank, amount);
        Debug.Log($"Set Stone Amount: {amount} new Bank {m_stoneBank}");
    }
    
    public void SetWoodAmount(int amount)
    {
        m_woodBank = amount;
        UpdateWoodBank?.Invoke(m_woodBank, amount);
    }

    public int GetStoneAmount()
    {
        return m_stoneBank;
    }


    public int GetWoodAmount()
    {
        return m_woodBank;
    }

    public void UpdateStoneGathererAmount(int amount)
    {
        m_stoneGathererCount += amount;
        UpdateStoneGathererCount?.Invoke(m_stoneGathererCount);
    }

    public int GetStoneGathererAmount()
    {
        return m_stoneGathererCount;
    }

    public void UpdateWoodGathererAmount(int amount)
    {
        m_woodGathererCount += amount;
        UpdateWoodGathererCount?.Invoke(m_woodGathererCount);
    }

    public int GetWoodGathererAmount()
    {
        return m_woodGathererCount;
    }

    public void SetupRuinIndications()
    {
        m_validRuinsInMission = new List<RuinController>(m_ruinsInMission);
        
        if (m_resourceManagerData.m_missionUnlockables.Count == 0) return;

        //List<ProgressionKeyData> keysInMission = new List<ProgressionKeyData>();
        foreach (ProgressionUnlockableData unlockableData in m_resourceManagerData.m_missionUnlockables)
        {
            foreach (ProgressionKeyData key in unlockableData.GetKeyData())
            {
                // Get the total weight, to then pick a random value from it.
                int weightSum = 0;
                for (int i = 0; i < m_validRuinsInMission.Count; ++i)
                {
                    weightSum += m_validRuinsInMission[i].m_ruinWeight;
                }

                int chosenWeight = Random.Range(0, weightSum);
                int lastTotalWeight = 0;
                for (int i = 0; i < m_validRuinsInMission.Count; ++i) // increase lastTotalWeight until it's greater than the chosen (random) weight.
                {
                    if (chosenWeight < lastTotalWeight + m_validRuinsInMission[i].m_ruinWeight)
                    {
                        // This is the node we have chosen.
                        RuinIndicator indicatorToSpawn = unlockableData.GetRuinIndicator();
                        m_validRuinsInMission[i].IndicateThisRuin(key, indicatorToSpawn);
                        m_validRuinsInMission.Remove(m_validRuinsInMission[i]);
                        break;
                    }

                    lastTotalWeight += m_validRuinsInMission[i].m_ruinWeight;
                }
            }
        }

        //for each ruin still in the valid list, replace them with a tree.
        foreach (RuinController ruin in m_validRuinsInMission)
        {
            Transform parent = ruin.transform;
            Vector3 pos = parent.position;
            GameObject obj = m_treePrefabs[Random.Range(0, m_treePrefabs.Count)];
            ResourceNode resourceNode = ObjectPoolManager.SpawnObject(obj, pos, quaternion.identity, parent, ObjectPoolManager.PoolType.GameObject).GetComponent<ResourceNode>();
            resourceNode.CreateResourceNode();
        }
    }


    public void StartDepositTimer()
    {
        m_depositTimer = 0f;
    }
}

[System.Serializable]
public class WoodDeposit
{
    public int m_quantity;
    public float m_timeStamp;
}

public class GathererLineRendererObject
{
    public GathererController m_gatherer;
    public GameObject m_targetObj;
    public LineRenderer m_lineRenderer;

    public GathererLineRendererObject(GathererController gatherer)
    {
        m_gatherer = gatherer;
        GameObject gathererLineRendererGameObject = new GameObject($"{gatherer.m_gathererData.m_gathererName}'s Path");
        gathererLineRendererGameObject.transform.SetParent(ResourceManager.Instance.transform);
        gathererLineRendererGameObject.transform.position = Vector3.zero;
        gathererLineRendererGameObject.transform.rotation = Quaternion.identity;

        m_lineRenderer = gathererLineRendererGameObject.AddComponent<LineRenderer>();

        m_lineRenderer.enabled = false;
        m_lineRenderer.startWidth = 0.075f;
        m_lineRenderer.endWidth = 0.075f;
        m_lineRenderer.material = ResourceManager.Instance.m_gathererPathMaterial;
        foreach (Material mat in m_lineRenderer.materials)
        {
            mat.color = gatherer.m_gathererData.m_gathererPathColor;
        }

        m_lineRenderer.numCornerVertices = 5;

        m_gatherer.OnGathererPathChanged += UpdateLineRendererPositions;

        m_targetObj = ObjectPoolManager.SpawnObject(ResourceManager.Instance.m_gathererTargetObj, ResourceManager.Instance.transform);
        Renderer[] targetRenderer = m_targetObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in targetRenderer) // Needless foreach given there's only one mesh in the target, but OK for now! TODO
        {
            renderer.material.color = gatherer.m_gathererData.m_gathererPathColor;
        }

        m_targetObj.SetActive(false);
    }

    public void UpdateLineRendererProgress()
    {
        if (m_lineRenderer.positionCount <= 2)
        {
            m_targetObj.SetActive(false);
            m_lineRenderer.positionCount = 0;
            m_lineRenderer.enabled = false;
            return;
        }

        Vector3[] oldPositions = new Vector3[m_lineRenderer.positionCount];
        Vector3[] newPositions = new Vector3[m_lineRenderer.positionCount - 1];
        m_lineRenderer.GetPositions(oldPositions);

        for (int i = 0, j = 0; i < oldPositions.Length; i++)
        {
            if (i == 1) continue; // Skip the element at index 1
            newPositions[j] = oldPositions[i];
            j++;
        }

        m_lineRenderer.positionCount = newPositions.Length;
        m_lineRenderer.SetPositions(newPositions);
    }

    void UpdateLineRendererPositions(List<Vector2Int> path)
    {
        return; // disabling for a sec

        MoveTargetObj();
        m_lineRenderer.enabled = true;
        m_lineRenderer.positionCount = path.Count + 2;

        // Add Gatherer Position as index 0.
        m_lineRenderer.SetPosition(0, m_gatherer.transform.position);

        //Add their Path to the array.
        if (path.Count > 0)
        {
            for (var i = 0; i < path.Count; i++)
            {
                var pos = path[i];
                m_lineRenderer.SetPosition(i + 1, new Vector3(pos.x, 0.1f, pos.y));
            }
        }

        // Add the resource node position.
        Vector3 endPos = Vector3.Lerp(m_lineRenderer.GetPosition(m_lineRenderer.positionCount - 2), m_gatherer.m_targetObjPosition, .6f);
        m_lineRenderer.SetPosition(m_lineRenderer.positionCount - 1, endPos);
    }

    void MoveTargetObj()
    {
        if (m_targetObj.transform.position == m_gatherer.m_targetObjPosition) return; // We're in the right spot, no need to move.
        m_targetObj.transform.position = m_gatherer.m_targetObjPosition;
        m_targetObj.transform.localScale = Vector3.one;
        m_targetObj.SetActive(true);
        m_targetObj.transform.DOScale(2f, .3f).From().SetEase(Ease.OutQuint);
    }
}