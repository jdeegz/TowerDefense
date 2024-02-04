using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandGathererQuestStep : QuestStep
{
    private QuestState m_currentQuestState;

    void OnEnable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange += QuestStateChange;
        GameplayManager.OnCommandRequested += CommandRequested;
    }

    void OnDisable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange -= QuestStateChange;
        GameplayManager.OnCommandRequested -= CommandRequested;
    }

    private void QuestStateChange(Quest quest)
    {
        if (quest.m_info.m_id.Equals(m_questId))
        {
            m_currentQuestState = quest.m_questState;
            Debug.Log($"Quest {m_questId} updated to {m_currentQuestState}.");
        }
    }

    private void CommandRequested(GameObject requestObj, Selectable.SelectedObjectType type)
    {
        if (type == Selectable.SelectedObjectType.ResourceWood)
        {
            if (m_progressValue < m_progressRequired)
            {
                ++m_progressValue;
                ProgressQuestStep();
            }

            if (m_progressValue >= m_progressRequired)
            {
                FinishedQuestStep();
            }
        }
    }
    
    public override QuestStepUIData GetQuestStepUIData()
    {
        QuestStepUIData questStepUIData = new QuestStepUIData(m_isFinished, m_progressValue, m_progressRequired, m_questStepDescription);
        return questStepUIData;
    }
}