using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMissionSelectView : MonoBehaviour
{
    [SerializeField] private Button m_backButton;
    [SerializeField] private Button m_resetSaveDataButton;
    [SerializeField] private GameObject m_missionButtonRoot;
    [SerializeField] private GameObject m_missionButtonObj;
    [SerializeField] private List<UIMissionSelectButton> m_missionButtons;
    // Start is called before the first frame update

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

    private void ClearMissionList()
    {
        foreach(Transform child in m_missionButtonRoot.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void BuildMissionList()
    {
        int numberOfMissions = 5; //why the hell is this hardcoded lol
        for (int i = 0; i < numberOfMissions; i++)
        {
            //Make the button
            //GameObject newButton = Instantiate(m_missionButtonObj, m_missionButtonRoot.transform);
            //Access the button's script
            UIMissionSelectButton missionSelectButtonScript = m_missionButtons[i];
            //Get the Button script
            //Button button = newButton.GetComponent<Button>();
            //Stash the Mission data
            MissionData data = GameManager.Instance.m_MissionContainer.m_MissionList[i];
            
            //Read from player data for this mission.
            MissionSaveData missionSaveData = PlayerDataManager.Instance.m_playerData.m_missions[i];
            missionSelectButtonScript.SetData(data, missionSaveData.m_missionCompletionRank, missionSaveData.m_missionAttempts);
            
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
    }
}
