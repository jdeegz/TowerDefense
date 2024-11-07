using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIMissionSelectButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_titleLabel;
    [SerializeField] private GameObject m_progressionDisplayObj;
    
    private Button m_buttonScript;
    private MissionData m_missionData;
    private string m_missionScene;
    private int m_completetionRank;
    private int m_attempts;

    public void SetData(MissionData data, int completionRank)
    {
        m_missionData = data;
        m_missionScene = data.m_missionScene;
        m_titleLabel.SetText(data.m_missionName);
        m_completetionRank = completionRank;
        m_progressionDisplayObj.SetActive(false);
        m_buttonScript = GetComponent<Button>();
        switch (m_completetionRank)
        {
            case 0: // LOCKED
                m_buttonScript.interactable = false;
                break;
            case 1: // UNLOCKED
                m_buttonScript.interactable = true;
                m_progressionDisplayObj.SetActive(true);
                break;
            case 2: // DEFEATED
                m_buttonScript.interactable = true;
                break;
            default:
                break;
        }
        
        m_buttonScript.onClick.RemoveListener(OnButtonClick); // Avoid duplicate listeners on list rebuild.
        m_buttonScript.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        GameManager.Instance.RequestChangeScene(m_missionScene, GameManager.GameState.Gameplay);
        GameManager.Instance.m_curMission = m_missionData;
    }
}