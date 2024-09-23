using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuinController : MonoBehaviour
{
    public RuinData m_ruinData;
    public GameObject m_ruinChargeVisualObj;
    
    private AudioSource m_audioSource;
    private bool m_ruinIsCharged;

    void Start()
    {
        GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
        GridManager.Instance.RefreshGrid();
        m_ruinIsCharged = true;
        m_ruinChargeVisualObj.SetActive(true);
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.PlayOneShot(m_ruinData.m_discoveredAudioClip);

        m_audioSource.clip = m_ruinData.m_unclaimedAudioClip;
        m_audioSource.Play();
    }

    public bool RequestPowerUp()
    {
        if (m_ruinIsCharged)
        {
            m_audioSource.Stop();
            m_ruinChargeVisualObj.SetActive(false);
            m_ruinIsCharged = false;
            m_audioSource.PlayOneShot(m_ruinData.m_chargeConsumedAudioClip);
            return true;
        }
        else
        {
            return false;
        }
    }

    public RuinTooltipData GetTooltipData()
    {
        RuinTooltipData data = new RuinTooltipData();
        data.m_ruinName = m_ruinData.m_ruinName;
        data.m_ruinDescription = m_ruinData.m_ruinDescription;
        data.m_ruinDetails = m_ruinIsCharged ? "Gatherer Level-up Available." : "Level-up Consumed.";
        return data;
    }
}

public class RuinTooltipData
{
    public string m_ruinName;
    public string m_ruinDescription;
    public string m_ruinDetails;
}