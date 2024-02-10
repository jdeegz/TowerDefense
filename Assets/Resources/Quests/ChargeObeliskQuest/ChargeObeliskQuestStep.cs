using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeObeliskQuestStep : QuestStep
{
    private QuestState m_currentQuestState;
    private bool m_progressRequiredSet;
    
    void OnEnable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange += QuestStateChange;
        for (int i = 0; i < GameplayManager.Instance.m_obelisksInMission.Count; ++i)
        {
            GameplayManager.Instance.m_obelisksInMission[i].OnObeliskChargeChanged += ObeliskCharged;
        }
        SetProgressRequired();
    }
    
    void OnDisable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange -= QuestStateChange;
        for (int i = 0; i < GameplayManager.Instance.m_obelisksInMission.Count; ++i)
        {
            GameplayManager.Instance.m_obelisksInMission[i].OnObeliskChargeChanged -= ObeliskCharged;
        }
    }

    private void ObeliskCharged(int curChargeValue)
    {
        if (m_progressValue < m_progressRequired)
        {
            m_progressValue = curChargeValue;
            ProgressQuestStep();
        }

        if (m_progressValue >= m_progressRequired)
        {
            FinishedQuestStep();
        }
    }

    private void QuestStateChange(Quest quest)
    {
        if (quest.m_info.m_id.Equals(m_questId))
        {
            m_currentQuestState = quest.m_questState;
            Debug.Log($"Quest {m_questId} updated to {m_currentQuestState}.");
        }
    }

    private void SetProgressRequired()
    {
        //We're assuming all obelisks have the same max charge count.
        m_progressRequired = GameplayManager.Instance.m_obelisksInMission[0].m_obeliskData.m_maxChargeCount;
        m_progressRequiredSet = true;
    }

    public override QuestStepUIData GetQuestStepUIData()
    {
        if(!m_progressRequiredSet) SetProgressRequired();
        QuestStepUIData questStepUIData = new QuestStepUIData(m_isFinished, m_progressValue, m_progressRequired, m_questStepDescription);
        return questStepUIData;
    }
}
