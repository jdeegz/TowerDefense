using UnityEngine;
using UnityEngine.VFX;

public class TowerBlacksmith : Tower
{
    
    void Start()
    {
        m_sparkTimer = Random.Range(0, m_sparkPeriodLength);
    }

    // Update is called once per frame
    void Update()
    {
        HandleInteriorSparks();
    }

    [SerializeField] private float m_sparkPeriodLength;
    [SerializeField] private float m_sparkActiveLength;
    [SerializeField] private VisualEffect m_VFXInteriorSparks;
    private float m_sparkTimer;
    private bool m_sparksEnabled;

    void HandleInteriorSparks()
    {
        if (!m_isBuilt) return;
        
        m_sparkTimer += Time.deltaTime;

        if (!m_sparksEnabled && m_sparkTimer > m_sparkPeriodLength) // Turn on if off
        {
            m_sparksEnabled = true;
            m_VFXInteriorSparks.Play();
        }

        if (m_sparksEnabled && m_sparkTimer > m_sparkPeriodLength + m_sparkActiveLength) // Turn off if on
        {
            m_sparkTimer = 0;
            m_sparksEnabled = false;
            m_VFXInteriorSparks.Stop();
        }
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
            gathererController.RequestUpdateGathererLevel(1);
        }
    }

    public override void RemoveTower()
    {
        //De-level the gatherers.
        foreach (GathererController gathererController in GameplayManager.Instance.m_woodGathererList)
        {
            gathererController.RequestUpdateGathererLevel(-1);
        }
        
        base.RemoveTower();
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
