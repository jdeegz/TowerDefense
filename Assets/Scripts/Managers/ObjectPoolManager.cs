using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager Instance;
    private List<PooledObjectInfo> m_objectPools = new List<PooledObjectInfo>();
    private List<OrphanedObject> m_orphanList = new List<OrphanedObject>();
    private List<OrphanedObject> m_expiredOrphanList = new List<OrphanedObject>();
    private static Dictionary<string, PooledObjectInfo> m_poolDictionary = new Dictionary<string, PooledObjectInfo>();

    public enum PoolType
    {
        ParticleSystem,
        Enemy,
        Tower,
        Projectile,
        GameObject,
        UI,
        None
    }

    private GameObject m_objectPoolEmptyHolder;
    private GameObject m_particleSystemEmpty;
    private GameObject m_enemyEmpty;
    private GameObject m_towerEmpty;
    private GameObject m_projectileEmpty;
    private GameObject m_gameObjectEmpty;


    void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }

        SetupEmpties();

        //Prewarm EnemyController lists for Tower FindTarget()
        ListPool<EnemyController>.WarmUp(50);
    }

    void SetupEmpties()
    {
        m_objectPoolEmptyHolder = new GameObject("Pooled Objects");

        m_particleSystemEmpty = new GameObject("Particle Systems");
        m_particleSystemEmpty.transform.SetParent(m_objectPoolEmptyHolder.transform);

        m_enemyEmpty = new GameObject("Enemies");
        m_enemyEmpty.transform.SetParent(m_objectPoolEmptyHolder.transform);

        m_towerEmpty = new GameObject("Towers");
        m_towerEmpty.transform.SetParent(m_objectPoolEmptyHolder.transform);

        m_projectileEmpty = new GameObject("Projectiles");
        m_projectileEmpty.transform.SetParent(m_objectPoolEmptyHolder.transform);

        m_gameObjectEmpty = new GameObject("Game Objects");
        m_gameObjectEmpty.transform.SetParent(m_objectPoolEmptyHolder.transform);
    }

    void OnDestroy()
    {
        Instance = null;
    }

    void Update()
    {
        for (int i = 0; i < m_orphanList.Count; ++i)
        {
            if (m_orphanList[i].m_poolDelay < Time.time)
            {
                m_expiredOrphanList.Add(m_orphanList[i]);
            }
        }

        foreach (OrphanedObject orphanedObject in m_expiredOrphanList)
        {
            //Debug.Log($"Orphan {orphanedObject.m_orphanObject.name} returned to pool.");
            ReturnObjectToPool(orphanedObject.m_orphanObject, orphanedObject.m_poolType);
            m_orphanList.Remove(orphanedObject);
        }

        m_expiredOrphanList.Clear();
    }

    public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, Transform parent, PoolType poolType = PoolType.None)
    {
        if (Instance == null) return null;

        if (!m_poolDictionary.TryGetValue(objectToSpawn.name, out PooledObjectInfo pool))
        {
            pool = new PooledObjectInfo() { m_lookUpString = objectToSpawn.name };
            Instance.m_objectPools.Add(pool);
            m_poolDictionary[objectToSpawn.name] = pool;
        }

        //check for inactive obj in pool
        GameObject spawnableObj = pool.m_inactiveObjects.FirstOrDefault();
        GameObject parentObject = parent == null ? SetParentObject(poolType) : parent.gameObject;

        if (spawnableObj == null)
        {
            spawnableObj = Instantiate(objectToSpawn, spawnPosition, spawnRotation);
        }
        else
        {
            pool.m_inactiveObjects.Remove(spawnableObj);
            spawnableObj.transform.position = spawnPosition;
            spawnableObj.transform.rotation = spawnRotation;
        }

        spawnableObj.transform.SetParent(parentObject.transform);
        
        PooledObject pooledObj = spawnableObj.GetComponent<PooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnSpawn();
        }
        else
        {
            spawnableObj.SetActive(true);
        }
        
        return spawnableObj;
    }

    public static GameObject SpawnObject(GameObject objectToSpawn, Transform parent, PoolType poolType = PoolType.None)
    {
        if (Instance == null) return null;

        if (!m_poolDictionary.TryGetValue(objectToSpawn.name, out PooledObjectInfo pool))
        {
            pool = new PooledObjectInfo() { m_lookUpString = objectToSpawn.name };
            Instance.m_objectPools.Add(pool);
            m_poolDictionary[objectToSpawn.name] = pool;
        }

        //check for inactive obj in pool
        GameObject spawnableObj = pool.m_inactiveObjects.FirstOrDefault();

        if (spawnableObj == null)
        {
            spawnableObj = Instantiate(objectToSpawn, parent);
        }
        else
        {
            pool.m_inactiveObjects.Remove(spawnableObj);
            
            spawnableObj.transform.SetParent(parent);
            PooledObject pooledObj = spawnableObj.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.OnSpawn();
            }
            else
            {
                spawnableObj.SetActive(true);
            }
        }

        //Debug.Log($"Spawning {spawnableObj.name}.");
        return spawnableObj;
    }

    public static UIPopup SpawnPopup(UIPopup popupPrefab, Transform parent)
    {
        GameObject spawnedObj = SpawnObject(popupPrefab.gameObject, parent, PoolType.UI);
        return spawnedObj ? spawnedObj.GetComponent<UIPopup>() : null;
    }

    public static void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.None)
    {
        string goName = obj.name.Substring(0, obj.name.Length - 7); // Remove (Clone)
        PooledObjectInfo pool = Instance.m_objectPools.Find(p => p.m_lookUpString == goName);

        GameObject parentObject = SetParentObject(poolType);
        if (parentObject != null)
        {
            obj.transform.SetParent(parentObject.transform);
        }

        if (pool == null)
        {
            //Debug.LogWarning($"Trying to release an object that is not pooled: {obj.name}");
        }
        else
        {
            //Debug.Log($"{obj.name} returned to inactive objects pool.");

            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.OnDespawn();
            }
            else
            {
                obj.SetActive(false);
            }

            pool.m_inactiveObjects.Add(obj);
        }
    }

    public static void OrphanObject(GameObject obj, float delay, PoolType poolType = PoolType.None)
    {
        //Debug.Log($"New Orphan {obj.name} with delay of: {delay}.");
        OrphanedObject orphan = new OrphanedObject { m_orphanObject = obj, m_poolDelay = Time.time + delay, m_poolType = poolType };
        Instance.m_orphanList.Add(orphan);
    }


    public static GameObject SetParentObject(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.ParticleSystem:
                return Instance.m_particleSystemEmpty;
            case PoolType.Projectile:
                return Instance.m_projectileEmpty;
            case PoolType.Tower:
                return Instance.m_towerEmpty;
            case PoolType.Enemy:
                return Instance.m_enemyEmpty;
            case PoolType.GameObject:
                return Instance.m_gameObjectEmpty;
            case PoolType.UI:
                return null;
            case PoolType.None:
                return null;
            default:
                return null;
        }
    }
}

public struct OrphanedObject
{
    public GameObject m_orphanObject;
    public float m_poolDelay;
    public ObjectPoolManager.PoolType m_poolType;
}

[Serializable]
public class PooledObjectInfo
{
    public string m_lookUpString;
    public List<GameObject> m_inactiveObjects = new List<GameObject>();
}