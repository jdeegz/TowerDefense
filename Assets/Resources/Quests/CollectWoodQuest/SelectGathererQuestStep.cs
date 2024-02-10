using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectGathererQuestStep : QuestStep
{
    private QuestState m_currentQuestState;
    public int m_gathererIndex;
    private GathererController m_gathererController;

    void Awake()
    {
        if (!m_gathererController) GetGathererController();
    }

    private void GetGathererController()
    {
        m_gathererController = GameplayManager.Instance.m_woodGathererList[m_gathererIndex];
    }

    void OnEnable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange += QuestStateChange;
        GameplayManager.OnGameObjectSelected += GameObjectSelected;
    }

    void OnDisable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange -= QuestStateChange;
        GameplayManager.OnGameObjectSelected -= GameObjectSelected;
    }

    private void QuestStateChange(Quest quest)
    {
        if (quest.m_info.m_id.Equals(m_questId))
        {
            m_currentQuestState = quest.m_questState;
            Debug.Log($"Quest {m_questId} updated to {m_currentQuestState}.");
        }
    }

    private void GameObjectSelected(GameObject obj)
    {
        if (obj == m_gathererController.gameObject)
        {
            GathererSelected();
        }
    }

    void GathererSelected()
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

    public override QuestStepUIData GetQuestStepUIData()
    {
        if (!m_gathererController) GetGathererController();
        string formattedString = string.Format(m_questStepDescription, m_gathererController.gameObject.name);
        QuestStepUIData questStepUIData = new QuestStepUIData(m_isFinished, m_progressValue, m_progressRequired, formattedString);
        return questStepUIData;
    }
}