using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionRewardTower", menuName = "ScriptableObjects/Progression/ProgressionRewardTower")]
public class ProgressionRewardTower : ProgressionRewardData
{
    [SerializeField] private TowerData m_towerData; // To refactor to structure data.
    public override string RewardType => "Tower";

    public override void UnlockReward()
    {
        // Write to player prefs to flip the value
        base.UnlockReward();
        
        // Do other stuff
        Debug.Log($"Congratulations, you've unlocked the {m_towerData.m_towerName}!");
    }

    public override void LockReward()
    {
        // Write to player prefs to lock this structure.
        base.LockReward();
        
        // Do other stuff
    }

    public override TowerData GetReward()
    {
        return m_towerData;
    }

    public override int GetRewardQty()
    {
        return -1;
    }
}
