using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectWoodQuestStep : QuestStep
{
    private void OnEnable()
    {
        ResourceManager.UpdateWoodBank += WoodCollected;
    }

    private void OnDisable()
    {
        ResourceManager.UpdateWoodBank -= WoodCollected;
    }

    private void WoodCollected(int bankTotal, int woodCollected)
    {
        if (woodCollected <= 0) return;

        if (m_progressValue < m_progressRequired)
        {
            m_progressValue += woodCollected;
            ProgressQuestStep();
        }

        if (m_progressValue >= m_progressRequired)
        {
            FinishedQuestStep();
            GameplayManager.Instance.m_delayForQuest = false;
        }
    }

    public override QuestStepUIData GetQuestStepUIData()
    {
        QuestStepUIData questStepUIData = new QuestStepUIData(m_isFinished, m_progressValue, m_progressRequired, m_questStepDescription);
        return questStepUIData;
    }
}