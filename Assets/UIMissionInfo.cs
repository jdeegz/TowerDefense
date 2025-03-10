using System;
using DG.Tweening;
using GameUtil;
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
    private MissionButtonInteractable.DisplayState m_displayState;

    private void Awake()
    {
        base.Awake();
        m_popupRect = GetComponent<RectTransform>();
        m_defaultYPosition = m_popupRect.anchoredPosition.y;
    }

    public override void HandleShow()
    {
        //Debug.Log($"MissionInfo: Opening popup.");
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
        //Debug.Log($"MissionInfo: Closing popup.");
        MissionTableController.Instance.SetSelectedMission(null);
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
        switch (m_displayState)
        {
            case MissionButtonInteractable.DisplayState.Uninitialized:
                break;
            case MissionButtonInteractable.DisplayState.Locked:
                FormatMissionInfo(false, LocalizationManager.Instance.CurrentLanguage.m_missionInfoButtonLocked, false);
                break;
            case MissionButtonInteractable.DisplayState.Unlocked:
                FormatMissionInfo(true, LocalizationManager.Instance.CurrentLanguage.m_missionInfoButtonPlay, false);
                break;
            case MissionButtonInteractable.DisplayState.Defeated:
                FormatMissionInfo(true, "Play", true);
                break;
            case MissionButtonInteractable.DisplayState.Perfected:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    void FormatMissionInfo(bool buttonInteractable, string buttonString, bool showDetailsLabel)
    {
        //Set UI display.
        string missionName;
        string missionDescription;
        Sprite missionThumbnail;
        string missionDetailsString;
        string missionWaveHighScore;
        string missionPerfectWaveScore;
        
        if (m_missionData == null)
        {
            missionName = LocalizationManager.Instance.CurrentLanguage.m_missionInfoNameDefault;
            missionDescription = LocalizationManager.Instance.CurrentLanguage.m_missionInfoDescriptionDefault;
            missionThumbnail = null;
            missionWaveHighScore = "";
            missionPerfectWaveScore = "";
        }
        else
        {
            missionName = m_missionData.m_missionName;
            missionDescription = m_missionData.m_missionDescription;
            missionThumbnail = m_missionData.m_missionSprite;
            missionWaveHighScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentEndlessHighScore, m_missionSaveData.m_waveHighScore);
            missionPerfectWaveScore = string.Format(LocalizationManager.Instance.CurrentLanguage.m_tooltipCurrentPerfectHighScore, m_missionSaveData.m_perfectWaveScore);
        }

        m_missionNameLabel.SetText(missionName);
        m_missionDescriptionLabel.SetText(missionDescription);
        m_missionPlayButton.interactable = buttonInteractable;
        m_missionPlayButtonLabel.SetText(buttonString);
        m_missionDetailsLabel.gameObject.SetActive(showDetailsLabel);

        
        missionDetailsString = $"{missionWaveHighScore}<br>{missionPerfectWaveScore}";
        m_missionDetailsLabel.SetText(missionDetailsString);

        m_missionThumbnail.sprite = missionThumbnail;
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
            m_displayState = missionInfoData.m_missionDisplayState;
        }

        FormatDisplay();
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
    public MissionButtonInteractable.DisplayState m_missionDisplayState;

    public MissionInfoData(MissionSaveData missionSaveData, MissionData missionData, MissionButtonInteractable.DisplayState missionDisplayState)
    {
        m_missionSaveData = missionSaveData;
        m_missionData = missionData;
        m_missionDisplayState = missionDisplayState;
    }
}