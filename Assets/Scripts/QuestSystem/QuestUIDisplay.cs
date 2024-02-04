using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Object = System.Object;

public class QuestUIDisplay : MonoBehaviour
{
    public Quest m_questInProgress;
    public QuestStepUIDisplay m_questStepUIDisplay;
    public List<QuestStepUIDisplay> m_questStepUIDisplays;
    
    void OnEnable()
    {
        QuestManager.Instance.m_questEvents.onStartQuest += StartQuest;
        QuestManager.Instance.m_questEvents.onAdvanceQuest += AdvanceQuest;
        QuestManager.Instance.m_questEvents.onFinishQuest += FinishQuest;
        QuestManager.Instance.m_questEvents.onQuestStateChange += QuestStateChange;
    }

    void OnDestroy()
    {
        QuestManager.Instance.m_questEvents.onStartQuest -= StartQuest;
        QuestManager.Instance.m_questEvents.onAdvanceQuest -= AdvanceQuest;
        QuestManager.Instance.m_questEvents.onFinishQuest -= FinishQuest;
        QuestManager.Instance.m_questEvents.onQuestStateChange -= QuestStateChange;
    }

    private void QuestStateChange(Quest quest)
    {
        switch (quest.m_questState)
        {
            case QuestState.REQUIREMENTS_NOT_MET:
                break;
            case QuestState.CAN_START:
                break;
            case QuestState.IN_PROGRESS:
                //New Quest In Progress.
                //Create a UI display for each step, and store them. The order and index matters!
                BuildQuestStepUIDisplay(quest);
                break;
            case QuestState.CAN_FINISH:
                break;
            case QuestState.FINISHED:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void BuildQuestStepUIDisplay(Quest quest)
    {
        m_questStepUIDisplays = new List<QuestStepUIDisplay>(quest.m_info.m_questStepPrefabs.Length);

        for (var i = 0; i < quest.m_info.m_questStepPrefabs.Length; ++i)
        {
            var stepObj = quest.m_info.m_questStepPrefabs[i];
            QuestStep questStep = stepObj.GetComponent<QuestStep>();
            QuestStepUIDisplay obj = Instantiate(m_questStepUIDisplay, gameObject.transform);
            obj.SetupQuestStepUIDisplay(questStep);

            m_questStepUIDisplays[i] = obj;
        }
    }

    private void FinishQuest(string obj)
    {
        
    }

    private void AdvanceQuest(string obj)
    {
        
    }

    private void StartQuest(string id)
    {
        
    }
}
