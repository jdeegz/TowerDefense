using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class QuestStep : MonoBehaviour
{
    [SerializeField] protected int m_progressValue = 0;
    [SerializeField] protected int m_progressRequired = 1;
    [SerializeField] [TextArea(3, 10)] protected string m_questStepDescription;
    protected bool m_isFinished = false;
    protected string m_questId;
    public event Action onProgressUpdate;

    public void InitializeQuestStep(string questId)
    {
        m_questId = questId;
    }

    protected void FinishedQuestStep()
    {
        if (!m_isFinished)
        {
            m_isFinished = true;
            QuestManager.Instance.m_questEvents.AdvanceQuest(m_questId);
            ProgressQuestStep();
            Destroy(gameObject);
        }
    }

    protected void ProgressQuestStep()
    {
        onProgressUpdate.Invoke();
    }

    public abstract QuestStepUIData GetQuestStepUIData();
}

public class QuestStepUIData
{
    public bool m_isFinished;
    public int m_progressValue;
    public int m_requiredValue;
    public string m_descriptionString;

    public QuestStepUIData(bool isFinished, int progressValue, int requiredValue, string description)
    {
        m_isFinished = isFinished;
        m_progressValue = progressValue;
        m_requiredValue = requiredValue;
        m_descriptionString = description;
    }
}