using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatToggle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_toggleLabel;

    private ProgressionUnlockableData m_unlockableData;
    private Toggle m_toggle;

    void Awake()
    {
        m_toggle = GetComponent<Toggle>();
    }
    
    public void UpdateState()
    {
        UnlockProgress progress = m_unlockableData.GetProgress();
        if (m_toggle.isOn == progress.m_isUnlocked) return;
        
        m_toggle.isOn = progress.m_isUnlocked;
    }

    public void SetupToggle(ProgressionUnlockableData unlockableData)
    {
        m_unlockableData = unlockableData;
        string toggleLabelString = m_unlockableData.name.Replace("_ProgressionUnlockableData", "");
        m_toggleLabel.SetText(toggleLabelString);
        
        m_toggle.onValueChanged.AddListener(ToggleChangedValue);
        UpdateState();
    }

    private void ToggleChangedValue(bool value)
    {
        //if (m_toggle.isOn == value) return;
        
        //Set the unlockable key and reward to this value.
        if (value)
        {
            m_unlockableData.CheatProgression();
        }
        else
        {
            m_unlockableData.ResetProgression();
        }
    }
}
