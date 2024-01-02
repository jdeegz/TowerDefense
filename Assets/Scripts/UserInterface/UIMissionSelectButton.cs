using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIMissionSelectButton : MonoBehaviour
{
    public Button m_buttonScript;
    public string m_missionScene;
    public TextMeshProUGUI m_titleLabel;
    public TextMeshProUGUI m_descriptionLabel;
    public TextMeshProUGUI m_attemptsLabel;
    public TextMeshProUGUI m_completionRankLabel;
    public Image m_icon;
    public Material m_lockedMaterial;
    public GameObject m_completionStarObj;
    public GameObject m_progressionDisplayObj;
    public HorizontalLayoutGroup m_starsLayoutGroup;
    public GameObject m_missionCompleteRunes;
    private int m_completetionRank;
    private int m_attempts;

    public void SetData(String missionScene, String title, String description, Sprite icon, int completionRank, int attempts)
    {
        m_missionScene = missionScene;
        m_titleLabel.SetText(title);
        m_descriptionLabel.SetText(description);
        //m_icon.sprite = icon;
        m_attempts = attempts;
        m_attemptsLabel.SetText($"Tries: {m_attempts}");
        m_completetionRank = completionRank;
        m_starsLayoutGroup.gameObject.SetActive(false);
        m_progressionDisplayObj.SetActive(false);
        string completionRankString = "";
        switch (m_completetionRank)
        {
            case 0:
                completionRankString = "Locked";
                m_icon.material = m_lockedMaterial;
                m_buttonScript.interactable = false;
                m_attemptsLabel.gameObject.SetActive(false);
                m_missionCompleteRunes.SetActive(false);
                break;
            case 1:
                completionRankString = "";
                m_progressionDisplayObj.SetActive(true);
                m_missionCompleteRunes.SetActive(false);
                //do more defeated stuff here.
                break;
            case 2:
                completionRankString = "";
                m_completionRankLabel.gameObject.SetActive(false);
                //do more defeated stuff here.
                //m_starsLayoutGroup.gameObject.SetActive(true);
                m_missionCompleteRunes.SetActive(true);
                for (int i = 1; i < m_completetionRank; ++i)
                {
                    Instantiate(m_completionStarObj, m_starsLayoutGroup.transform);
                }
                m_completionStarObj.SetActive(false);
                break;
            default:
                completionRankString = "";
                break;
        }
        m_completionRankLabel.SetText(completionRankString);
        m_buttonScript.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        GameManager.Instance.RequestChangeScene(m_missionScene, GameManager.GameState.Gameplay);
    }
}