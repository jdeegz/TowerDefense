using System.Collections.Generic;
using UnityEngine;

public class TestMissionList : MonoBehaviour
{
    [SerializeField] private MissionContainerData m_missionTable;
    [SerializeField] private Color m_colorUnlocked;
    [SerializeField] private Color m_colorDefeated;
    [SerializeField] private Color m_colorLocked;

    [SerializeField] private List<GameObject> m_missionObjects;

    void Start()
    {
        if (m_missionTable.m_MissionList.Length != m_missionObjects.Count)
        {
            Debug.Log($"Number of missions: {m_missionTable.m_MissionList.Length} does not match the number of Mission Objects: {m_missionObjects.Count}");
        }

        // Collect the saved state of each mission by reading it's unlock Requirements.
        for (int i = 0; i < m_missionTable.m_MissionList.Length && i < m_missionObjects.Count; ++i)
        {
            MissionData missionData = m_missionTable.m_MissionList[i];
            bool missionUnlocked = false;

            if (missionData.m_isUnlockedByDefault)
            {
                missionUnlocked = true;
            }
            else
            {
                // If the mission requires multiple UnlockableDatas to be earned, assure we've earned each one to determine the missions status.
                int unlockableDataUnlocked = 0;
                for (int x = 0; x < missionData.m_unlockRequirements.Count; ++x)
                {
                    if (missionData.m_unlockRequirements[x].GetProgress().m_isUnlocked)
                    {
                        ++unlockableDataUnlocked;
                    }
                }

                Debug.Log($"{missionData.m_missionName} has {unlockableDataUnlocked} / {missionData.m_unlockRequirements.Count} unlockables Obtained.");

                missionUnlocked = unlockableDataUnlocked == missionData.m_unlockRequirements.Count;
            }

            FormatMissionObject(i, missionUnlocked, missionData);
        }
    }

    void FormatMissionObject(int index, bool missionUnlocked, MissionData missionData)
    {
        GameObject missionObj = m_missionObjects[index];

        Material material = missionObj.GetComponent<Renderer>().material;

        MissionSaveData missionSaveData = PlayerDataManager.Instance.GetMissionSaveDataByMissionData(missionData);

        bool isDefeated = false;
        if (missionUnlocked)
        {
            // Color based on unlocked vs defeated.
            isDefeated = missionSaveData.m_missionCompletionRank == 2;
            material.color = isDefeated ? m_colorUnlocked : m_colorDefeated;
        }
        else
        {
            material.color = m_colorLocked;
        }

        Debug.Log($"FormatMission: {missionData.m_missionName}. Is Unlocked: {missionUnlocked}. Is Defeated: {isDefeated}.");
    }
}