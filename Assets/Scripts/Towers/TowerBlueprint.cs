using Unity.Mathematics;
using UnityEngine;

public class TowerBlueprint : Tower
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public override void SetupTower()
    {
        //Grid
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GameplayManager.Instance.AddBlueprintToList(this);

        //Operational
        gameObject.GetComponent<Collider>().enabled = true;
        m_isBuilt = false;

        //Animation
        m_animator.SetTrigger("Construct");

        //Audio
        m_audioSource.PlayOneShot(m_towerData.m_audioBuildClip);

        //VFX
        ObjectPoolManager.SpawnObject(m_towerData.m_towerConstructionPrefab, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
    }
    

    public override TowerTooltipData GetTooltipData()
    {
        TowerTooltipData data = new TowerTooltipData();
        data.m_towerName = m_towerData.m_towerName;
        data.m_towerDescription = m_towerData.m_towerDescription;
        return data;
    }

    public override TowerUpgradeData GetUpgradeData()
    {
        Debug.Log($"Blueprint Tower has no Upgrade Data to get.");
        return null;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        Debug.Log($"Blueprint Tower has no Upgrade Data to set.");
    }
}
