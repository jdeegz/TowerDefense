using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class QuestPoint : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] private QuestInfoSO m_questInfoForPoint;

    private string m_questId;
    private QuestState m_currentQuestState;
    private bool m_playerIsNear = false;

    [Header("Config")]
    [SerializeField] private bool m_startPoint; 
    [SerializeField] private bool m_finishPoint; 
    
    private void Awake()
    {
        m_questId = m_questInfoForPoint.m_id;
    }

    private void OnEnable()
    {
        QuestManager.Instance.m_questEvents.onQuestStateChange += QuestStateChange;
    }

    private void OnDisable()
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

    private void OnTriggerEnter(Collider otherCollider)
    {
        Debug.Log($"Enter Triggered: {otherCollider.name}");
        if (otherCollider.CompareTag("Player"))
        {
            m_playerIsNear = true;
        }
    }
    
    private void OnTriggerExit(Collider otherCollider)
    {
        if (otherCollider.CompareTag("Player"))
        {
            m_playerIsNear = false;
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            SubmitPressed();
        }
    }

    private void SubmitPressed()
    {
        if (!m_playerIsNear)
        {
            return;
        }

        if (m_currentQuestState.Equals(QuestState.CAN_START) && m_startPoint)
        {
            QuestManager.Instance.m_questEvents.StartQuest(m_questId);
        }
        else if (m_currentQuestState.Equals(QuestState.CAN_FINISH) && m_finishPoint)
        {
            QuestManager.Instance.m_questEvents.FinishQuest(m_questId);
        }
    }

}
