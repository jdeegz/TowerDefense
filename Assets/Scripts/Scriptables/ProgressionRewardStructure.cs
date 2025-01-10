using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionRewardStructure", menuName = "ScriptableObjects/Progression/ProgressionRewardStructure")]
public class ProgressionRewardStructure : ProgressionRewardData
{
    [SerializeField] private TowerData m_structureData; // To refactor to structure data.
    [SerializeField] private int m_qty = 1;
    public override string RewardType => "Structure";
    
    public override void UnlockReward()
    {
        // Write to player prefs to flip the value
        base.UnlockReward();
        
        // Do other stuff
        Debug.Log($"Congratulations, you've unlocked the {m_structureData.m_towerName}!");
    }

    public override void LockReward()
    {
        // Write to player prefs to lock this structure.
        base.LockReward();
        
        // Do other stuff
    }
    
    public override TowerData GetReward()
    {
        return m_structureData;
    }

    public override int GetRewardQty()
    {
        return m_qty;
    }
}
