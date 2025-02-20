using TMPro;
using UnityEngine;

public class WaveCompleteLabelSetter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_descriptionLabel;
    [SerializeField] private UIStringData m_uiStrings;

    void OnEnable()
    {
        string text;

        if (GameplayManager.Instance.IsEndlessModeActive())
        {
            text = m_uiStrings.m_waveCompletedEndless;
        }
        else
        {
            text = m_uiStrings.m_waveCompleted;
        }

        m_descriptionLabel.SetText(text);
    }
}