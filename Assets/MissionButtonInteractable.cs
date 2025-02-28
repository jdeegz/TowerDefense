using System;
using UnityEngine;

public class MissionButtonInteractable : Interactable
{
    [SerializeField] private MissionData m_missionData;
    [SerializeField] private Renderer m_spireRenderer;
    [SerializeField] private GameObject m_spireTopMesh;
    [SerializeField] private GameObject m_defeatedRootObj;
    [SerializeField] private GameObject m_lockedRootObj;

    [SerializeField] private Color m_lockedColorTint;
    private Color m_defaultColorTint;

    private MissionSaveData m_missionSaveData;
    private Material m_spireMaterial;

    private DisplayState m_buttonDisplayState;
    private DisplayState ButtonButtonDisplayState
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
    
    private enum DisplayState
    {
        Locked,
        Unlocked,
        Defeated,
        Perfected, 
    }
    
    void Start()
    {
        // Get the Save Data for this mission.
        //if (PlayerDataManager.Instance == null) return;
        if (m_missionData != null)
        {
            m_missionSaveData = PlayerDataManager.Instance.GetMissionSaveDataByMissionData(m_missionData);
        }

        // Get the spire material to tint it.
        m_spireMaterial = m_spireRenderer.material;
        m_defaultColorTint = m_spireMaterial.GetColor("_BaseTint");
        
        SetInitialState();
        
        ButtonButtonDisplayState = CalculateDisplayState();
    }

    void SetInitialState()
    {
        m_lockedRootObj.SetActive(true);
        m_defeatedRootObj.SetActive(false);
        m_spireMaterial.SetColor("_BaseTint", m_lockedColorTint);
    }

    DisplayState CalculateDisplayState()
    {
        if (m_missionSaveData == null)
        {
            return DisplayState.Locked;
        }

        switch (m_missionSaveData.m_missionCompletionRank)
        {
            case (0):
                return DisplayState.Locked;
            case (1):
                return DisplayState.Unlocked;
            case (2):
                return DisplayState.Defeated;
            case (>2):
                return DisplayState.Perfected;
            default:
                return DisplayState.Locked;
        }
    }
    
    void FormatMissionButton(DisplayState displayState)
    {
        switch (displayState)
        {
            case DisplayState.Locked:
                m_lockedRootObj.SetActive(true);
                m_defeatedRootObj.SetActive(false);
                m_spireMaterial.SetColor("_BaseTint", m_lockedColorTint);
                break;
            case DisplayState.Unlocked:
                m_lockedRootObj.SetActive(false);
                m_defeatedRootObj.SetActive(false);
                m_spireMaterial.SetColor("_BaseTint", m_defaultColorTint);
                break;
            case DisplayState.Defeated:
                m_lockedRootObj.SetActive(false);
                m_defeatedRootObj.SetActive(true);
                m_spireMaterial.SetColor("_BaseTint", m_defaultColorTint);
                break;
            case DisplayState.Perfected:
                break;
            default:
                return;
        }
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
        MissionTableController.Instance.SetTargetRotation(transform);

        MissionInfoData missionInfoData = new MissionInfoData(m_missionSaveData, m_missionData);
        UIPopupManager.Instance.ShowPopup<UIMissionInfo>("MissionInfo", missionInfoData);
    }
}
