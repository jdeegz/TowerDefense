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
