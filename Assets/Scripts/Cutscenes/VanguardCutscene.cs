using UnityEngine;
using UnityEngine.Serialization;

public class VanguardCutscene : MonoBehaviour
{
    public TowerData m_towerData;
    public GameObject m_projectilePrefab;
    public AudioSource m_audioSource;
    public Transform m_muzzlePoint;
    public EnemyController m_curTarget;
    public GameObject m_muzzleFlashPrefab;
    public Transform m_projectilesRoot;

    [Header("Vanguard Units")]
    public GameObject m_vanguardRoot;
    public GameObject m_vanguardObj;

    public float m_spawnInterval = 5f;
    public int m_spawnCounter = 3;

    private int m_spawnedCount;
    private float m_timeUntilSpawn;
    private float m_timeUntilFire;

    void Start()
    {
        m_timeUntilSpawn = 3f;
        //Debug.Log($"Time Until Fire set to -6.");
        m_timeUntilFire -= 1.5f;
    }

    void Spawn()
    {
        if (m_spawnCounter == m_spawnedCount) return;

        GameObject newVanguard = Instantiate(m_vanguardObj, m_vanguardRoot.transform);
        m_spawnedCount++;
        //Debug.Log($"Spawned Vanguard.");
    }

    void Update()
    {
        m_timeUntilFire += Time.unscaledDeltaTime;
        m_timeUntilSpawn += Time.unscaledDeltaTime;

        //If we have elapsed time, and are looking at the target, fire.
        if (m_timeUntilFire >= 1f / m_towerData.m_fireRate)
        {
            Fire();
            m_timeUntilFire = 0; 
        }
        
        if (m_spawnInterval < m_timeUntilSpawn)
        {
            Spawn();
            m_timeUntilSpawn = 0;
        }
    }

    private void Fire()
    {
        GameObject projectileObj = Instantiate(m_projectilePrefab, m_muzzlePoint.position, m_muzzlePoint.rotation, m_projectilesRoot);
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        projectileScript.SetProjectileData(m_curTarget, m_curTarget.m_targetPoint, m_towerData.m_baseDamage, m_muzzlePoint.position);

        int i = Random.Range(0, m_towerData.m_audioFireClips.Count - 1);
        m_audioSource.PlayOneShot(m_towerData.m_audioFireClips[i]);
        //m_animator.SetTrigger("Fire");
        FireVFX();

        //Debug.Log($"Fire. {Time.time}");
    }

    public void FireVFX()
    {
        if (!m_towerData.m_muzzleFlashPrefab) return;

        Instantiate(m_muzzleFlashPrefab, m_muzzlePoint.position, m_muzzlePoint.rotation, m_projectilesRoot);
    }
}