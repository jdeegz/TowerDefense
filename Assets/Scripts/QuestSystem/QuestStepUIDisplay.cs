using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestStepUIDisplay : MonoBehaviour
{
    public TextMeshProUGUI m_questStepLabel;

    public GameObject m_questStepCompleteFill;

    private QuestStep m_questStep;
    private QuestStepUIData m_curQuestStepUIData;

    public void SetupQuestStepUIDisplay(QuestStep questStep)
    {
        m_questStep = questStep;
        gameObject.name = m_questStep.name;
        ProgressUpdate();
    }

    void OnDestroy()
    {
        m_questStep.onProgressUpdate -= ProgressUpdate;
    }

    private void ProgressUpdate()
    {
        Debug.Log($"Trying to Update Progress on: {m_questStep.name}");
        m_curQuestStepUIData = m_questStep.GetQuestStepUIData();
        UpdateUIDisplay();
    }

    private void UpdateUIDisplay()
    {
        m_questStepCompleteFill.SetActive(m_curQuestStepUIData.m_isFinished);
        
        //Format the string.
        string progressString = $"<b>{m_curQuestStepUIData.m_progressValue} / {m_curQuestStepUIData.m_requiredValue}</b>";
        m_questStepLabel.SetText($"{progressString} {m_curQuestStepUIData.m_descriptionString}");
    }

    public void SetSubscription(QuestStep newStep)
    {
        Debug.Log($"Subscribing: {gameObject.name} to {newStep}");
        m_questStep = newStep;
        m_questStep.onProgressUpdate += ProgressUpdate;
    }
}
