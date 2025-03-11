using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;


public class MissionButtonInteractable : Interactable
{
    [Header("Data")]
    [SerializeField] private MissionData m_missionData;

    [Header("Renderers")]
    [SerializeField] private Renderer m_spireRenderer;

    [Header("Objects")]
    [SerializeField] private GameObject m_defeatedRootObj;
    [SerializeField] private GameObject m_lockedRootObj;
    [SerializeField] private GameObject m_undefeatedRootObj;
    [SerializeField] private VisualEffect m_undefeatedVFX;

    [Header("Audio Clips")]
    [SerializeField] private List<AudioClip> m_hoverEnterSFX;
    [SerializeField] private List<AudioClip> m_selectedSFX;

    [Header("Colors")]
    [SerializeField] private Color m_lockedColorTint;
    private Color m_defaultColorTint;

    private MissionSaveData m_missionSaveData;
    private Material m_spireMaterial;

    private DisplayState m_buttonDisplayState;

    private string m_selectedLayerString = "Outline Selected"; //Must sync with layer name.
    private string m_hoveredLayerString = "Outline Hover"; //Must sync with layer name.
    private string m_defaultLayerString;

    private AudioSource m_audioSource;

    private GameObject m_currentSelectedIndicator;

    private bool m_isHovered;

    private bool Hovered
    {
        get { return m_isHovered; }
        set
        {
            if (value != m_isHovered)
            {
                m_isHovered = value;
                
                if(m_isHovered && !m_isSelected) MenuManager.Instance.RequestAudioOneShot(m_hoverEnterSFX[Random.Range(0, m_hoverEnterSFX.Count)]);
                UpdateOutline();
            }
        }
    }

    private bool m_isSelected;

    private bool Selected
    {
        get { return m_isSelected; }
        set
        {
            if (value != m_isSelected)
            {
                m_isSelected = value;
                UpdateOutline();
            }
        }
    }

    public DisplayState ButtonDisplayState
    {
        get { return m_buttonDisplayState; }
        set
        {
            if (value != m_buttonDisplayState)
            {
                m_buttonDisplayState = value;
                FormatMissionButton(m_buttonDisplayState);
                //Debug.Log($"DisplayState: {m_missionData.m_missionName} is {m_buttonDisplayState}.");
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
        MissionTableController.OnMissionSelected += OnMissionSelected;
        m_defaultLayerString = LayerMask.LayerToName(gameObject.layer);
        m_audioSource = GetComponent<AudioSource>();

        // Get the Save Data for this mission.
        if (m_missionData != null)
        {
            m_missionSaveData = PlayerDataManager.Instance.GetMissionSaveDataByMissionData(m_missionData);
        }

        // Get the spire material to tint it.
        m_spireMaterial = m_spireRenderer.material;
        m_defaultColorTint = m_spireMaterial.GetColor("_BaseColor");

        SetState(true, false, m_lockedColorTint, false);

        ButtonDisplayState = CalculateDisplayState();
    }

    void OnDestroy()
    {
        MissionTableController.OnMissionSelected -= OnMissionSelected;
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
                SetState(true, false, m_lockedColorTint, false);
                break;
            case DisplayState.Unlocked:
                SetState(false, false, m_defaultColorTint, true);
                break;
            case DisplayState.Defeated:
                SetState(false, true, m_defaultColorTint, false);
                break;
            case DisplayState.Perfected:
                break;
            default:
                return;
        }
    }

    void SetState(bool showLockedRoot, bool showDefeatedRoot, Color spireColor, bool showUndefeatedVFX)
    {
        m_lockedRootObj.SetActive(showLockedRoot);
        m_defeatedRootObj.SetActive(showDefeatedRoot);
        m_undefeatedRootObj.SetActive(showUndefeatedVFX);
        m_spireMaterial.SetColor("_BaseColor", spireColor);

        if (showUndefeatedVFX)
        {
            m_undefeatedVFX.Play();
        }
        else
        {
            m_undefeatedVFX.Stop();
        }
    }


    public override void OnClick()
    {
        //Debug.Log($"OnClick: {m_missionData.m_missionName}.");
        MissionTableController.Instance.SetSelectedMission(this);
        
        MenuManager.Instance.RequestAudioOneShot(m_selectedSFX[Random.Range(0, m_selectedSFX.Count)]);
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

    public override void OnHover()
    {
        Hovered = true;
    }

    public override void OnHoverExit()
    {
        Hovered = false;
    }

    private void OnMissionSelected(MissionButtonInteractable obj)
    {
        // Handle Deselection
        Selected = obj == this;
    }

    void UpdateOutline()
    {
        string layerName = m_defaultLayerString;
        if (m_isSelected)
        {
            // Selected Outline
            layerName = m_selectedLayerString;
        }
        else
        {
            if (m_isHovered)
            {
                // Hovered Outline
                layerName = m_hoveredLayerString;
            }
            else
            {
                // No Outline
                layerName = m_defaultLayerString;
            }
        }

        if (m_spireRenderer == null) return;

        m_spireRenderer.gameObject.layer = LayerMask.NameToLayer(layerName);
    }
}