using System;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

public class UIMissionInfo : UIPopup, IDataPopup
{
    [SerializeField] private TextMeshProUGUI m_missionNameLabel;
    [SerializeField] private TextMeshProUGUI m_missionDescriptionLabel;
    [SerializeField] private Image m_missionThumbnail;
    [SerializeField] private TextMeshProUGUI m_missionDetailsLabel;
    [SerializeField] private TextMeshProUGUI m_missionPlayButtonLabel;
    [SerializeField] private Button m_missionPlayButton;

    private RectTransform m_popupRect;
    private MissionSaveData m_missionSaveData;
    private MissionData m_missionData;
    private float m_defaultYPosition;
    private Tween m_curTween;
    private Sequence m_curSequence;
    
    private void Awake()
    {
        base.Awake();
        m_popupRect = GetComponent<RectTransform>();
        m_defaultYPosition = m_popupRect.anchoredPosition.y;
    }

    public override void HandleShow()
    {
        Debug.Log($"MissionInfo: Opening popup.");
        gameObject.SetActive(true);
        if (m_curSequence != null && m_curSequence.IsActive()) 
        {
            m_curSequence.Kill(false);
        }

        m_canvasGroup.blocksRaycasts = true;
        m_canvasGroup.interactable = true;
        
        m_curSequence = DOTween.Sequence();
        m_curSequence.Join(m_canvasGroup.DOFade(1, 0.3f));
        m_curSequence.Join(m_popupRect.DOAnchorPosY(m_defaultYPosition - 20f, 0.3f).From());
        m_curSequence.Play();
    }
    
    public override void HandleClose()
    {
        Debug.Log($"MissionInfo: Closing popup.");
        ResetData();
        m_canvasGroup.interactable = false;
        m_canvasGroup.blocksRaycasts = false;
        
        if (m_curSequence != null && m_curSequence.IsActive()) 
        {
            m_curSequence.Kill(false);
        }

        m_curSequence = DOTween.Sequence();
        
        m_curSequence.Join(m_canvasGroup.DOFade(0, 0.3f).SetUpdate(true).OnComplete(() => CompleteClose()));
        m_curSequence.Play();
    }
    
    public void FormatDisplay()
    {
        switch (m_missionSaveData.m_missionCompletionRank)
        {
            case 0: // Locked
                m_missionDetailsLabel.gameObject.SetActive(false);
                m_missionPlayButton.interactable = false;
                m_missionPlayButtonLabel.SetText("Locked");
                break;
            case 1: // Unlocked
                m_missionDetailsLabel.gameObject.SetActive(false);
                m_missionPlayButton.interactable = true;
                m_missionPlayButtonLabel.SetText("Play");
                break;
            case 2: // Defeated
                m_missionPlayButton.interactable = true;
                m_missionPlayButtonLabel.SetText("Play");
                m_missionDetailsLabel.gameObject.SetActive(true);
                break;
            case >2: // Perfected
                m_missionPlayButton.interactable = true;
                m_missionPlayButtonLabel.SetText("Play");
                m_missionDetailsLabel.gameObject.SetActive(true);
                break;
            default:
                break;
        }
        //Set UI display.
        m_missionNameLabel.SetText(m_missionData.m_missionName);
        m_missionDescriptionLabel.SetText(m_missionData.m_missionDescription);

        string missionDetailsString;
        string missionWaveHighScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentEndlessHighScore, m_missionSaveData.m_waveHighScore);
        string missionPerfectWaveScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentEndlessScore, m_missionSaveData.m_perfectWaveScore);
        missionDetailsString = $"{missionWaveHighScore}<br>{missionPerfectWaveScore}";
        m_missionDetailsLabel.SetText(missionDetailsString);

        m_missionThumbnail.sprite = m_missionData.m_missionSprite;
        m_missionPlayButton.onClick.AddListener(OnPlayButtonClicked);
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_popupRect);
    }

    public void OnPlayButtonClicked()
    {
        Debug.Log($"PlayButtonClicked: Request Change Scene to {m_missionData.m_missionScene}.");
        
        if (GameManager.Instance == null) return;

        GameManager.Instance.RequestChangeScene(m_missionData.m_missionScene, GameManager.GameState.Gameplay);
        GameManager.Instance.m_curMission = m_missionData;
    }

    public void SetData(object data)
    {
        if (data is MissionInfoData missionInfoData)
        {
            m_missionSaveData = missionInfoData.m_missionSaveData;
            m_missionData = missionInfoData.m_missionData;
            FormatDisplay();
        }
    }

    public void ResetData()
    {
        m_missionSaveData = null;
        m_missionData = null;
    }
}

public class MissionInfoData
{
    public MissionSaveData m_missionSaveData;
    public MissionData m_missionData;
    
    public MissionInfoData(MissionSaveData missionSaveData, MissionData missionData)
    {
        m_missionSaveData = missionSaveData;
        m_missionData = missionData;
    }
}