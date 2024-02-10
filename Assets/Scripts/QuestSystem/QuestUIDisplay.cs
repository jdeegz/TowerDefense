using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Object = System.Object;

public class QuestUIDisplay : MonoBehaviour
{
    public QuestStepUIDisplay m_questStepUIDisplay;
    public List<QuestStepGroup> m_questsInProgress;
    
    void Awake()
    {
        QuestManager.Instance.m_questEvents.onStartQuest += StartQuest;
        QuestManager.Instance.m_questEvents.onAdvanceQuest += AdvanceQuest;
        QuestManager.Instance.m_questEvents.onFinishQuest += FinishQuest;
        QuestManager.Instance.m_questEvents.onQuestStateChange += QuestStateChange;
        QuestManager.Instance.m_questEvents.onQuestStepCreated += QuestStepCreated;
    }

    void OnDestroy()
    {
        QuestManager.Instance.m_questEvents.onStartQuest -= StartQuest;
        QuestManager.Instance.m_questEvents.onAdvanceQuest -= AdvanceQuest;
        QuestManager.Instance.m_questEvents.onFinishQuest -= FinishQuest;
        QuestManager.Instance.m_questEvents.onQuestStateChange -= QuestStateChange;
        QuestManager.Instance.m_questEvents.onQuestStepCreated -= QuestStepCreated;
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

    //We create new UI displays for each step despite each step not being active yet.
    //We store all the UI displays in a group so we know their relationship to quests and can remove the together.
    //Quest UI displays are subscribed to Quest Step prefabs as they're spawned through the QuestStepCreated() function.
    private void BuildQuestStepUIDisplay(Quest quest)
    {
        if (m_questsInProgress == null) m_questsInProgress = new List<QuestStepGroup>();
        
        QuestStepGroup newQuestStepGroup = new QuestStepGroup();
        newQuestStepGroup.m_quest = quest;
        newQuestStepGroup.m_questStepUIDisplays = new QuestStepUIDisplay[quest.m_info.m_questStepPrefabs.Length];

        for (var i = 0; i < newQuestStepGroup.m_questStepUIDisplays.Length; ++i)
        {
            var stepObj = quest.m_info.m_questStepPrefabs[i];
            QuestStep questStep = stepObj.GetComponent<QuestStep>();
            QuestStepUIDisplay obj = Instantiate(m_questStepUIDisplay, gameObject.transform);
            obj.SetupQuestStepUIDisplay(questStep);

            newQuestStepGroup.m_questStepUIDisplays[i] = obj;
        }
        m_questsInProgress.Add(newQuestStepGroup);
    }

    private void QuestStepCreated(QuestStep newStep)
    {
        //Find the QuestInProgress ID that matches this Quest Step's Quest ID.
        foreach (QuestStepGroup questInProgress in m_questsInProgress)
        {
            if (questInProgress.m_quest.m_info.m_id == newStep.GetQuestID())
            {
                int i = questInProgress.m_quest.GetCurrentStepIndex();
                questInProgress.m_questStepUIDisplays[i].SetSubscription(newStep);
                break;
            }
        }
    }
    
    
    private void FinishQuest(string id)
    {
        Debug.Log($"Quest {id} has finished. Attempting to destroy quest list.");
        //Compare the string ID with the quests in our QuestsInProgress list.
        //We want to remove the matching quest step UI displays.
        foreach (QuestStepGroup questInProgress in m_questsInProgress)
        {
            if (questInProgress.m_quest.m_info.m_id == id)
            {
                Debug.Log($"Quest found. Deleting quest list.");
                //We found the quest that finished in our list of quests In Progress.
                foreach (QuestStepUIDisplay display in questInProgress.m_questStepUIDisplays)
                {
                    display.RemoveDisplay();
                }
            }
        }
    }

    private void AdvanceQuest(string id)
    {
        
    }

    private void StartQuest(string id)
    {
        
    }
}

[Serializable]
public class QuestStepGroup
{
    public Quest m_quest;
    public QuestStepUIDisplay[] m_questStepUIDisplays;
}
