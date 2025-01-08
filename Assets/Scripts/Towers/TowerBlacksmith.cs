using UnityEngine;

public class TowerBlacksmith : Tower
{
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void SetupTower()
    {
        base.SetupTower();

        TriggerBlacksmith();
    }

    private void TriggerBlacksmith()
    {
        foreach (GathererController gathererController in GameplayManager.Instance.m_woodGathererList)
        {
            gathererController.RequestIncrementGathererLevel(1);
        }
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
        TowerUpgradeData data = new TowerUpgradeData();

        data.m_turretRotation = GetTurretRotation();

        return data;
    }

    public override void SetUpgradeData(TowerUpgradeData data)
    {
        SetTurretRotation(data.m_turretRotation);
    }
}
