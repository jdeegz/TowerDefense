using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShrineOrbController : MonoBehaviour
{
    public GameObject m_claimVFX;
    public List<AudioClip> m_claimSounds;

    public GameObject m_orbProjectile;
    
    public float m_moveSpeed = 1f;
    public float m_turnSpeed = 1f;
    public float m_radius = 1.5f;                // Define the size of the box.
    
    private Vector3 m_spawnPoint;               // Where we spawned. Referenced to get new destination.
    private Vector3 m_curDestination;           // Where we're currently heading.
    private float m_stoppingDistance = 1f;
    private TowerShrine m_orbParentTower;
    
    void Start()
    {
        m_spawnPoint = transform.position;
        m_curDestination = GetNewDestination();
        Vector3 direction = m_curDestination - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
        
        //GameplayManager.OnGameObjectSelected += GameObjectSelected;
    }

    public void SetShrine(TowerShrine towerShrine)
    {
        m_orbParentTower = towerShrine;
    }
    
    private void GameObjectSelected(GameObject obj)
    {
        if (obj != gameObject) return;
        
        RemoveCharge();
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, m_curDestination) <= m_stoppingDistance)
        {
            m_curDestination = GetNewDestination();
        }

        HandleMovement();
    }
  

    private void HandleMovement()
    {
        // Move forward.
        float speed = m_moveSpeed  * Time.deltaTime;
        transform.position += transform.forward * speed;

        // Rotate towards Target.
        Vector3 direction = m_curDestination - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, m_turnSpeed * Time.fixedDeltaTime);
    }

    void OnMouseDown()
    {
        Debug.Log($"Orb clicked.");
        LaunchProjectiles();
        RemoveCharge();
    }

    private void LaunchProjectiles()
    {
        // Get targets (Gatherers)
        foreach (GathererController gatherer in GameplayManager.Instance.m_woodGathererList)
        {
            GameObject projectileObj = ObjectPoolManager.SpawnObject(m_orbProjectile, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.Projectile);
            ShrineOrbProjectile projectile = projectileObj.GetComponent<ShrineOrbProjectile>();
            projectile.Setup(gatherer);
        }
    }

    void RemoveCharge()
    {
        // Inform Shrine
        m_orbParentTower.ChargeClicked();
        
        // Spawn VFX
        ObjectPoolManager.SpawnObject(m_claimVFX, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
        
        // Play Audio
        m_orbParentTower.RequestPlayAudio(m_claimSounds[Random.Range(0, m_claimSounds.Count)]);
        
        // Remove Object
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.GameObject);
    }

    private Vector3 GetNewDestination()
    {
        Vector3 pos = m_spawnPoint;

        pos.x = Random.Range(m_spawnPoint.x - m_radius, m_spawnPoint.x + m_radius);
        pos.y = Random.Range(1, 1 + m_radius);
        pos.z = Random.Range(m_spawnPoint.z - m_radius, m_spawnPoint.z + m_radius);
        
        return pos;
    }

    private void OnDestroy()
    {
        // GameplayManager.OnGameObjectSelected -= GameObjectSelected;
    }
}
