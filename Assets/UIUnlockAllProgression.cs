using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIUnlockAllProgression : MonoBehaviour
{
    private Button m_button;
    void Awake()
    {
        m_button = GetComponent<Button>();
        m_button.onClick.AddListener(OnUnlockAllButtonClick);   
    }
    
    private void OnUnlockAllButtonClick()
    {
        PlayerDataManager.Instance.CheatPlayerData();
        
        MissionTableController.Instance.RequestTableReset();
    }
}
