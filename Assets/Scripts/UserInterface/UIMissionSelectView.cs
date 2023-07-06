using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMissionSelectView : MonoBehaviour
{
    [SerializeField] private Button m_backButton;
    [SerializeField] private GameObject m_missionButtonRoot;
    [SerializeField] private GameObject m_missionButtonObj;
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

        for (int i = 0; i < GameManager.Instance.m_MissionContainer.m_MissionList.Length; i++)
        {
            //Make the button
            GameObject newButton = Instantiate(m_missionButtonObj, m_missionButtonRoot.transform);
            //Access the button's script
            UIMissionSelectButton missionSelectButtonScript = newButton.GetComponent<UIMissionSelectButton>();
            //Get the Button script
            Button button = newButton.GetComponent<Button>();
            //Stash the Mission data
            ScriptableMissionDataObject data = GameManager.Instance.m_MissionContainer.m_MissionList[i];
            
            missionSelectButtonScript.SetData(button, data.m_missionScene, data.m_missionName, data.m_missionDescription, data.m_missionSprite);
        }
    }
    
    private void OnBackButtonClick()
    {
        MenuManager.Instance.UpdateMenuState(MenuManager.MenuState.StartMenu);
    }
}
