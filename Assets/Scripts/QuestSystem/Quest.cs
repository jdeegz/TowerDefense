using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest
{
    public QuestInfoSO m_info;
    public QuestState m_questState;
    private int m_currentQuestStepIndex;

    public Quest(QuestInfoSO questInfo)
    {
        m_info = questInfo;
        m_questState = QuestState.REQUIREMENTS_NOT_MET;
        m_currentQuestStepIndex = 0;
    }

    public void MoveToNextStep()
    {
        ++m_currentQuestStepIndex;
    }

    public bool CurrentStepExists()
    {
        return (m_currentQuestStepIndex < m_info.m_questStepPrefabs.Length);
    }

    public int GetCurrentStepIndex()
    {
        return m_currentQuestStepIndex;
    }

    public QuestStep InstantiateCurrentQuestStep(Transform parentTransform)
    {
        GameObject questStepPrefab = GetCurrentQuestStepPrefab();
        QuestStep questStep = null;
        if (questStepPrefab != null)
        {
            questStep = Object.Instantiate(questStepPrefab, parentTransform).GetComponent<QuestStep>();
            questStep.InitializeQuestStep(m_info.m_id);
        }

        return questStep;
    }

    private GameObject GetCurrentQuestStepPrefab()
    {
        GameObject questStepPrefab = null;
        if (CurrentStepExists())
        {
            questStepPrefab = m_info.m_questStepPrefabs[m_currentQuestStepIndex];
        }
        else
        {
            Debug.Log($"Tried to get quest step prefab, but stepIndex was out of range indicating that there's no current step: Quest ID = {m_info.m_id}, stepIndex = {m_currentQuestStepIndex}.");
        }

        return questStepPrefab;
    }

}
