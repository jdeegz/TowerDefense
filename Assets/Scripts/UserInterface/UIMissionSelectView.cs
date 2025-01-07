using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIMissionSelectView : MonoBehaviour
{
    [SerializeField] private Button m_backButton;
    [SerializeField] private Button m_resetSaveDataButton;
    [SerializeField] private GameObject m_missionButtonRoot;
    [SerializeField] private CanvasGroup m_dataResetToast;
    [SerializeField] private UIMissionSelectButton m_missionButtonPrefab;

    private List<UIMissionSelectButton> m_curMissionButtons;
    
    void Awake()
    {
        MenuManager.OnMenuStateChanged += MenuManagerStateChanged;
    }
    void OnDestroy()
    {
        MenuManager.OnMenuStateChanged -= MenuManagerStateChanged;
    }

    private void MenuManagerStateChanged(MenuManager.MenuState state)
    {
        gameObject.SetActive(state == MenuManager.MenuState.MissionSelect);
    }


    void Start()
    {
        m_backButton.onClick.AddListener(OnBackButtonClick);
        m_resetSaveDataButton.onClick.AddListener(OnResetButtonClick);

        BuildMissionList();
    }
    
    private void BuildMissionList()
    {
        if (!GameManager.Instance) return;
        
        int numberOfMissions;
        
        numberOfMissions = GameManager.Instance.m_missionTable.m_MissionList.Length;

        if (m_curMissionButtons == null) m_curMissionButtons = new List<UIMissionSelectButton>();
        
        for (int i = m_curMissionButtons.Count; i < numberOfMissions; i++)
        {
            m_curMissionButtons.Add(null);
        }
        
        for (int i = 0; i < numberOfMissions; i++)
        {
            UIMissionSelectButton button;
            
            if (m_curMissionButtons[i] != null)
            {
                button = m_curMissionButtons[i];
            }
            else
            {
              button = Instantiate(m_missionButtonPrefab, m_missionButtonRoot.transform);
              m_curMissionButtons[i] = button;
            } 
            
            MissionData data = GameManager.Instance.m_missionTable.m_MissionList[i];
            
            //Read from player data for this mission.
            MissionSaveData missionSaveData = PlayerDataManager.Instance.m_playerData.m_missions[i];
            button.SetData(data, missionSaveData.m_missionCompletionRank);
        }
    }
    
    private void OnBackButtonClick()
    {
        MenuManager.Instance.UpdateMenuState(MenuManager.MenuState.StartMenu);
    }

    private void OnResetButtonClick()
    {
        //ClearMissionList();
        PlayerDataManager.Instance.ResetPlayerData();
        BuildMissionList();
        m_dataResetToast.alpha = 1;
        m_dataResetToast.DOFade(0, 3f);
    }
}
