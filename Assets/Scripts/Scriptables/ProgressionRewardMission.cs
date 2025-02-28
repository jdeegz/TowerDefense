using UnityEngine;

[CreateAssetMenu(fileName = "ProgressionRewardMission", menuName = "ScriptableObjects/Progression/ProgressionRewardMission")]
public class ProgressionRewardMission : ProgressionRewardData
{
    public override string RewardType => "Mission";

    public override void UnlockReward()
    {
        // Write to player prefs to flip the value
        base.UnlockReward();
        
        // Do other stuff
        Debug.Log($"Congratulations, you've unlocked the {name}!");
    }

    public override TowerData GetReward()
    {
        return null;
    }

    public override int GetRewardQty()
    {
        return 0;
    }
}
