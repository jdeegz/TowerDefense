using System;
using UnityEngine;

public class MissionButtonInteractable : Interactable
{
    [SerializeField] private MissionData m_missionData;
    [SerializeField] private Renderer m_spireRenderer;
    [SerializeField] private GameObject m_defeatedRootObj;
    [SerializeField] private GameObject m_lockedRootObj;

    [SerializeField] private Color m_lockedColorTint;
    private Color m_defaultColorTint;

    private MissionSaveData m_missionSaveData;
    private Material m_spireMaterial;

    private DisplayState m_buttonDisplayState;

    public DisplayState ButtonDisplayState
    {
        get { return m_buttonDisplayState; }
        set
        {
            if (value != m_buttonDisplayState)
            {
                m_buttonDisplayState = value;
                FormatMissionButton(m_buttonDisplayState);
                Debug.Log($"DisplayState: {m_missionData.m_missionName} is {m_buttonDisplayState}.");
            }
        }
    }

    public MissionSaveData MissionSaveData => m_missionSaveData;

    public enum DisplayState
    {
        Uninitialized,
        Locked,
        Unlocked,
        Defeated,
        Perfected,
    }

    void Awake()
    {
        // Get the Save Data for this mission.
        //if (PlayerDataManager.Instance == null) return;
        if (m_missionData != null)
        {
            m_missionSaveData = PlayerDataManager.Instance.GetMissionSaveDataByMissionData(m_missionData);
        }

        // Get the spire material to tint it.
        m_spireMaterial = m_spireRenderer.material;
        m_defaultColorTint = m_spireMaterial.GetColor("_BaseColor");

        SetState(true, false, m_lockedColorTint);

        ButtonDisplayState = CalculateDisplayState();
    }

    public DisplayState CalculateDisplayState()
    {
        if (m_missionSaveData == null) // If we have no data, we're not ready. This is not good!
        {
            return DisplayState.Locked;
        }

        bool isUnlocked = true;
        foreach (var unlockReq in m_missionData.m_unlockRequirements) // Do we have all of the keys unlocked for this mission?
        {
            if (!unlockReq.GetProgress().m_isUnlocked)
            {
                isUnlocked = false;
                break;
            }
        }

        if (!isUnlocked)
        {
            return DisplayState.Locked;
        }

        if (m_missionSaveData.m_missionCompletionRank < 2)
        {
            return DisplayState.Unlocked;
        }

        return DisplayState.Defeated;
    }

    void FormatMissionButton(DisplayState displayState)
    {
        switch (displayState)
        {
            case DisplayState.Uninitialized:
                return;
            case DisplayState.Locked:
                SetState(true, false, m_lockedColorTint);
                break;
            case DisplayState.Unlocked:
                SetState(false, false, m_defaultColorTint);
                break;
            case DisplayState.Defeated:
                SetState(false, true, m_defaultColorTint);
                break;
            case DisplayState.Perfected:
                break;
            default:
                return;
        }
    }

    void SetState(bool showLockedRoot, bool showDefeatedRoot, Color spireColor)
    {
        m_lockedRootObj.SetActive(showLockedRoot);
        m_defeatedRootObj.SetActive(showDefeatedRoot);
        m_spireMaterial.SetColor("_BaseColor", spireColor);
    }

    public override void OnHover()
    {
        //Debug.Log($"OnHoverEnter: {m_missionData.m_missionName}.");
    }

    public override void OnHoverExit()
    {
        //Debug.Log($"OnHoverExit: {m_missionData.m_missionName}.");
    }

    public override void OnClick()
    {
        Debug.Log($"OnClick: {m_missionData.m_missionName}.");
        MissionTableController.Instance.SetSelectedMission(this);

        RequestMissionInfoPopup();
    }

    public void RequestMissionInfoPopup()
    {
        MissionInfoData missionInfoData = new MissionInfoData(m_missionSaveData, m_missionData, m_buttonDisplayState);
        UIPopupManager.Instance.ShowPopup<UIMissionInfo>("MissionInfo", missionInfoData);
    }

    public void UpdateDisplayState()
    {
        Debug.Log($"{name} Update Display State request serviced.");

        if (m_missionData != null)
        {
            m_missionSaveData = PlayerDataManager.Instance.GetMissionSaveDataByMissionData(m_missionData);
        }

        ButtonDisplayState = CalculateDisplayState();
    }
}