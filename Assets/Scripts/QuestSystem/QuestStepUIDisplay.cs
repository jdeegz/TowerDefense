using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestStepUIDisplay : MonoBehaviour
{
    [Header("Visual Components")]
    public CanvasGroup m_canvasGroup;
    public TextMeshProUGUI m_questStepLabel;
    public GameObject m_questStepCompleteFill;

    [Header("Animation & States")]
    public Animator m_animator;
    public string m_enterName;
    public string m_updateName;
    public string m_completeName;
    public string m_exitName;

    private QuestStep m_questStep;
    private QuestStepUIData m_curQuestStepUIData;
    
    public void SetupQuestStepUIDisplay(QuestStep questStep)
    {
        //m_canvasGroup.alpha = 0;
        m_questStep = questStep;
        gameObject.name = m_questStep.name;
        m_curQuestStepUIData = m_questStep.GetQuestStepUIData();

        UpdateUIDisplay();
    }

    void OnEnable()
    {
        HandleAnim(m_enterName);
    }

    void OnDestroy()
    {
        m_questStep.onProgressUpdate -= ProgressUpdate;
    }

    private void ProgressUpdate()
    {
        //Debug.Log($"Trying to Update Progress on: {m_questStep.name}");
        m_curQuestStepUIData = m_questStep.GetQuestStepUIData();
        UpdateUIDisplay();

        if (m_curQuestStepUIData.m_isFinished)
        {
            HandleAnim(m_completeName);
        }
        else
        {
            HandleAnim(m_updateName);
        }
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
        //Debug.Log($"Subscribing: {gameObject.name} to {newStep}");
        m_questStep = newStep;
        m_questStep.onProgressUpdate += ProgressUpdate;
    }

    public void HandleAnim(string clipName)
    {
        Debug.Log($"Animator playing: {clipName}");
        m_animator.Play($"{clipName}");
    }

    public void RemoveDisplay()
    {
        m_animator.SetTrigger("Exit");
        Destroy(gameObject, 1f);
    }
}