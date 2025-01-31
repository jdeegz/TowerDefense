using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private RectTransform m_questRectTransform;
    private ContentSizeFitter m_questContentSizeFitter;
    
    public void SetupQuestStepUIDisplay(QuestStep questStep)
    {
        //m_canvasGroup.alpha = 0;
        m_questStep = questStep;
        gameObject.name = m_questStep.name;
        m_curQuestStepUIData = m_questStep.GetQuestStepUIData();
        m_questRectTransform = GetComponent<RectTransform>();
        m_questContentSizeFitter = GetComponent<ContentSizeFitter>();

        UpdateUIDisplay();
        
        HandleAnim(m_enterName);
        m_canvasGroup.alpha = 0;
        m_canvasGroup.DOFade(1, .5f);
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
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_questRectTransform);
    }

    public void SetSubscription(QuestStep newStep)
    {
        //Debug.Log($"Subscribing: {gameObject.name} to {newStep}");
        m_questStep = newStep;
        m_questStep.onProgressUpdate += ProgressUpdate;
    }

    public void HandleAnim(string clipName)
    {
        //Debug.Log($"Animator playing: {clipName}");
        m_animator.Play($"{clipName}");
    }

    public void RemoveDisplay()
    {
        m_questContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        
        Sequence sequence = DOTween.Sequence();
        sequence.Join(m_questRectTransform.DOSizeDelta(new Vector2(m_questRectTransform.sizeDelta.x, 0), 1f));
        sequence.Join(m_canvasGroup.DOFade(0, .5f));
        sequence.OnComplete(() => Destroy(gameObject));
        
        sequence.Play();
    }
}