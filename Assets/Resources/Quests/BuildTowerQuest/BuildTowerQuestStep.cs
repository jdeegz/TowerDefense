using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildTowerQuestStep : QuestStep
{
    private QuestState m_currentQuestState;
    
    void OnEnable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange += QuestStateChange;
        GameplayManager.OnTowerBuild += TowerBuilt;
    }

    private void TowerBuilt()
    {
        if (m_progressValue < m_progressRequired)
        {
            ++m_progressValue;
            ProgressQuestStep();
        }

        if (m_progressValue >= m_progressRequired)
        {
            FinishedQuestStep();
            GameplayManager.Instance.m_delayForQuest = false;
        }
    }

    void OnDisable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange -= QuestStateChange;
    }

    private void QuestStateChange(Quest quest)
    {
        if (quest.m_info.m_id.Equals(m_questId))
        {
            m_currentQuestState = quest.m_questState;
            Debug.Log($"Quest {m_questId} updated to {m_currentQuestState}.");
        }
    }

    public override QuestStepUIData GetQuestStepUIData()
    {
        QuestStepUIData questStepUIData = new QuestStepUIData(m_isFinished, m_progressValue, m_progressRequired, m_questStepDescription);
        return questStepUIData;
    }
}
