using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MissionTableTear : MonoBehaviour
{
    public List<MissionTableTearSpawnable> m_spawnables;
    public float m_spawnRateMin = 1; // Number of enemies to spawn per second.
    public float m_spawnRateMax = 3; // Number of enemies to spawn per second.
    public float m_spawnDuration; // Number of seconds to spawn enemies for.
    public Renderer m_tearRenderer;
    public GameObject m_tearFlash;
    
    private Transform m_centerTransform;
    private Transform m_targetTransform;
    private Transform m_rotationRootTransform;
    
    private bool m_clockwise;
    private bool m_isSpawning;

    private float m_secondsUntilNextSpawn;
    private float m_spawnRateTimeElapsed;
    private float m_spawnDurationtimeElapsed;
    
    private int m_spawnablesWeightSum;

    private Material m_tearMaterial;
    private MaterialPropertyBlock m_materialPropertyBlock;
    private float m_paddingValue;
    private static readonly int PaddingID = Shader.PropertyToID("_Padding");

    public void SpawnTear(Transform centerTransform, Transform targetTransform, Transform rotationRootTransform, bool clockwise)
    {
        m_centerTransform = centerTransform;
        m_targetTransform = targetTransform;
        m_rotationRootTransform = rotationRootTransform;
        m_clockwise = clockwise;

        
        DOTween.To(() => m_paddingValue, x => m_paddingValue = x, 0.5f, .5f)
            .OnUpdate(UpdatePadding)
            .OnComplete(TearReadyToSpawn)
            .SetEase(Ease.InBounce);

        ObjectPoolManager.SpawnObject(m_tearFlash, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
    }

    void CloseTear()
    {
        DOTween.To(() => m_paddingValue, x => m_paddingValue = x, 1f, .25f)
            .OnUpdate(UpdatePadding)
            .OnComplete(() => ObjectPoolManager.SpawnObject(m_tearFlash, null, ObjectPoolManager.PoolType.ParticleSystem))
            .OnComplete(() => ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.GameObject));
    }

    void UpdatePadding()
    {
        m_tearRenderer.GetPropertyBlock(m_materialPropertyBlock);
        m_materialPropertyBlock.SetFloat(PaddingID, m_paddingValue);
        m_tearRenderer.SetPropertyBlock(m_materialPropertyBlock);
    }
    
    void TearReadyToSpawn()
    {
        m_isSpawning = true;
    }

    void Awake()
    {
        m_materialPropertyBlock = new MaterialPropertyBlock();
        for (int i = 0; i < m_spawnables.Count; ++i)
        {
            m_spawnablesWeightSum += m_spawnables[i].m_weight;
        }
    }

    void Update()
    {
        if (!m_isSpawning) return;
        
        m_spawnRateTimeElapsed += Time.deltaTime;
        m_spawnDurationtimeElapsed += Time.deltaTime;

        if (m_spawnDurationtimeElapsed > m_spawnDuration)
        {
            m_spawnDurationtimeElapsed = 0;
            m_spawnRateTimeElapsed = 0;
            m_isSpawning = false;
            CloseTear();
            return;
        }

        if (m_spawnRateTimeElapsed > m_secondsUntilNextSpawn)
        {
            m_spawnRateTimeElapsed = 0;
            m_secondsUntilNextSpawn = 1 / Random.Range(m_spawnRateMin, m_spawnRateMax);
            
            GameObject spawnedObj = ObjectPoolManager.SpawnObject(GetSpawnableObject(), transform.parent, ObjectPoolManager.PoolType.GameObject);
            float xNoise = Random.Range(-1f, 1f);
            float yNoise = Random.Range(-1f, 1f);
            float zNoise = Random.Range(-1f, 1f);
            Vector3 spawnPos = new Vector3(xNoise, yNoise, zNoise);
            spawnedObj.transform.position = transform.position + spawnPos;
            spawnedObj.GetComponent<ObjectOrbitController>().SetupObject(m_centerTransform, m_targetTransform, m_rotationRootTransform, m_clockwise);
        }
        
    }

    private GameObject GetSpawnableObject()
    {
        int chosenWeight = Random.Range(0, m_spawnablesWeightSum);
        int lastTotalWeight = 0;
        for (int i = 0; i < m_spawnables.Count; ++i) // increase lastTotalWeight until it's greater than the chosen (random) weight.
        {
            if (chosenWeight < lastTotalWeight + m_spawnables[i].m_weight)
            {
                // This is the node we have chosen.
                return m_spawnables[i].m_unitPrefab;
            }

            lastTotalWeight += m_spawnables[i].m_weight;
        }

        Debug.Log($"did not return a spawnable object.");
        return null;
    }
}

[System.Serializable]
public class MissionTableTearSpawnable
{
    public GameObject m_unitPrefab;
    public int m_weight = 1;
}
