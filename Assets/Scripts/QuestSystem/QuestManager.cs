using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;
using UnityEngine.Assertions;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;
    public QuestEvents m_questEvents;
    private Dictionary<string, Quest> m_questMap;
    public event Action<QuestStep> onQuestStepCreated;
    private bool m_questManagerActive; // Used to delay the function of this script until we're past Setup mode.
    
    private void Awake()
    {
        Instance = this;
        m_questMap = CreateQuestMap();
        m_questEvents = new QuestEvents();
        GameplayManager.OnGameplayStateChanged += GamepalyStateChanged;
    }

    private void GamepalyStateChanged(GameplayManager.GameplayState newState)
    {
        // Setting up the script, was formerly in a Start function.
        if (newState == GameplayManager.GameplayState.Build && !m_questManagerActive)
        {
            m_questManagerActive = true;
            
            foreach (Quest quest in m_questMap.Values)
            {
                m_questEvents.QuestStateChange(quest);
            }
        }
    }

    void OnEnable()
    {
        m_questEvents.onStartQuest += StartQuest;
        m_questEvents.onAdvanceQuest += AdvanceQuest;
        m_questEvents.onFinishQuest += FinishQuest;
    }

    void OnDestroy()
    {
        m_questEvents.onStartQuest -= StartQuest;
        m_questEvents.onAdvanceQuest -= AdvanceQuest;
        m_questEvents.onFinishQuest -= FinishQuest;
    }

    private void Update()
    {
        if (!m_questManagerActive) return;
        
        foreach (Quest quest in m_questMap.Values)
        {
            if (quest.m_questState == QuestState.REQUIREMENTS_NOT_MET && CheckRequirementsMet(quest))
            {
                if (quest.m_info.m_startAutomatically)
                {
                    StartQuest(quest.m_info.m_id);
                }
                else
                {
                    ChangeQuestState(quest.m_info.m_id, QuestState.CAN_START);
                }
            }
        }
    }

    private void ChangeQuestState(string id, QuestState questState)
    {
        Quest quest = GetQuestById(id);
        quest.m_questState = questState;
        m_questEvents.QuestStateChange(quest);
        Debug.Log($"Quest State Change for ");
    }

    private bool CheckRequirementsMet(Quest quest)
    {
        bool meetsRequirements = true;

        foreach (QuestInfoSO prerequisiteQuestInfo in quest.m_info.m_questPrerequisites)
        {
            if (GetQuestById(prerequisiteQuestInfo.m_id).m_questState != QuestState.FINISHED)
            {
                meetsRequirements = false;
            }
        }

        return meetsRequirements;
    }

    private void StartQuest(string id)
    {
        Debug.Log($"Start Quest {id}");
        Quest quest = GetQuestById(id);
        QuestStep newStep = quest.InstantiateCurrentQuestStep(transform);
        ChangeQuestState(quest.m_info.m_id, QuestState.IN_PROGRESS);
        m_questEvents.QuestStepCreated(newStep);
    }

    private void AdvanceQuest(string id)
    {
        Debug.Log($"Advance Quest {id}");
        Quest quest = GetQuestById(id);
        quest.MoveToNextStep();

        if (quest.CurrentStepExists())
        {
            QuestStep newStep = quest.InstantiateCurrentQuestStep(transform);
            m_questEvents.QuestStepCreated(newStep);
        }
        else
        {
            //This used to change state to CAN FINISH (Can turn in to complete). If we need the player to 'turn a quest in' extend here.
            //ChangeQuestState(quest.m_info.m_id, QuestState.CAN_FINISH);
            m_questEvents.FinishQuest(id);
        }
    }

    private void FinishQuest(string id)
    {
        Debug.Log($"Finish Quest {id}");
        Quest quest = GetQuestById(id);
        quest.m_questState = QuestState.FINISHED;
        
        ClaimRewards(quest);
        //ChangeQuestState(quest.m_info.m_id, QuestState.FINISHED);
    }

    private void ClaimRewards(Quest quest)
    {
        // TODO - Claim rewards if there are any.
    }

    private Dictionary<string, Quest> CreateQuestMap()
    {
        QuestInfoSO[] allQuests = Resources.LoadAll<QuestInfoSO>("Quests");
        Dictionary<string, Quest> idToQuestMap = new Dictionary<string, Quest>();
        foreach (QuestInfoSO questInfo in allQuests)
        {
            if (idToQuestMap.ContainsKey(questInfo.m_id))
            {
                Debug.Log($"Duplicate ID Found when creating quest map: {questInfo.m_id}");
            }

            idToQuestMap.Add(questInfo.m_id, new Quest(questInfo));
        }

        return idToQuestMap;
    }

    public Quest GetQuestById(string id)
    {
        Quest quest = m_questMap[id];
        if (quest == null)
        {
            Debug.Log($"ID not found in the Quest Map: {id}");
        }

        return quest;
    }


    //Quest 1 - Objective 1
    // Select a Gatherer.

    //Quest 1 - Objective 2
    // Command a Gatherer to harvest Wood.

    //Quest 2 - Objective 1
    // Build a tower.

    //Quest 2 - Objective 2
    // Draw a path through cells.

    //Quest 3 - Objective 1
    // Upgrade a Tower.

    //Quest 4 - Objective 1
    // Charge the Obelisk
}