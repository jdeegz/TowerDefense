using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CheatButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_buttonLabel;

    private ProgressionUnlockableData m_unlockableData;
    private Button m_button;

    void Awake()
    {
        m_button = GetComponent<Button>();
    }

    public void SetupButton(String buttonString, Action action, Action update)
    {
        m_buttonLabel.SetText(buttonString);
        m_button.onClick.AddListener(() => action());
        m_button.onClick.AddListener(() => update());
    }
}