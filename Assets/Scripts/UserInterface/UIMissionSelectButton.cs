using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIMissionSelectButton : MonoBehaviour
{
    public string m_missionScene;
    public TextMeshProUGUI m_titleLabel;
    public TextMeshProUGUI m_descriptionLabel;
    public Image m_icon;
    
    public void SetData(Button button, String missionScene, String title, String description, Sprite icon)
    {
        m_missionScene = missionScene;
        m_titleLabel.SetText(title);
        m_descriptionLabel.SetText(description);
        m_icon.sprite = icon;
        
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        GameManager.Instance.RequestChangeScene(m_missionScene, GameManager.GameState.Gameplay);
    }
}
