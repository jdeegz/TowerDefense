using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;

public class WaveReportCard : MonoBehaviour
{
    public static WaveReportCard Instance;
    
    [Header("Wave Display Report Card")]
    [SerializeField] private GameObject m_waveDisplayRootObj;
    [SerializeField] private GameObject m_waveCompletePerfectPrefab;
    [SerializeField] private GameObject m_waveCompletePrefab;

    private int m_wavesCompleted;
    private UICombatView m_combatHUD;
    private Transform m_homeTransform;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        GameplayManager.OnWaveCompleted += AlertWaveComplete;
        m_combatHUD = GetComponentInParent<UICombatView>();
        m_homeTransform = m_combatHUD.transform;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Update is called once per frame
    void OnDestroy()
    {
        GameplayManager.OnWaveCompleted -= AlertWaveComplete;
    }

    private void AlertWaveComplete(int enemiesCreated, int enemiesKilled, int soulsClaimed, int damageTaken)
    {
        ++m_wavesCompleted;
        
        // Calculate %
        float percentSoulsClaimed = (float)soulsClaimed / enemiesCreated;
        
        // Pick the prefab to create.
        if (percentSoulsClaimed == 1 && damageTaken == 0)
        {
            Instantiate(m_waveCompletePerfectPrefab, m_waveDisplayRootObj.transform);
        }
        else
        {
            Image image = Instantiate(m_waveCompletePrefab, m_waveDisplayRootObj.transform).GetComponent<Image>();
            image.color = m_combatHUD.m_waveCompleteColorRank.Evaluate(percentSoulsClaimed);
        }
        
        // Check to add a spacer.
        if (m_wavesCompleted % 5 == 0)
        {
            GameObject spacerObj = new GameObject("SPACER", typeof(RectTransform));
            spacerObj.transform.SetParent(m_waveDisplayRootObj.transform, false);
            RectTransform spacerRectTransform = spacerObj.GetComponent<RectTransform>();
            spacerRectTransform.sizeDelta = new Vector2(40, 5);
        }
    }

    public void MoveDisplayTo(Transform parentTransform)
    {
        transform.SetParent(parentTransform, false);
    }
    
    public void ReturnDisplayTo()
    {
        transform.SetParent(m_combatHUD.transform, false);
    }
}
