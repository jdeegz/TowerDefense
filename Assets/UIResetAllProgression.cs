using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIResetAllProgression : MonoBehaviour
{
    private Button m_button;
    void Awake()
    {
        m_button = GetComponent<Button>();
        m_button.onClick.AddListener(OnResetButtonClick);   
    }
    
    private void OnResetButtonClick()
    {
        PlayerDataManager.Instance.ResetPlayerData();
    }
}
