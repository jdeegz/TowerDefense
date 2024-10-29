using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObeliskHUDTooltip : UITooltip, IPointerEnterHandler
{
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        // Get the obelisk data to update the Description String
        m_detailsString = String.Empty;
        foreach (Obelisk obelisk in GameplayManager.Instance.m_obelisksInMission)
        {
            string obeliskName = obelisk.m_obeliskData.m_obeliskName;
            string obeliskProgress;
            if (obelisk.GetObeliskChargeCount() == obelisk.m_obeliskData.m_maxChargeCount)
            {
                obeliskProgress = $"<br>{obeliskName} Progress: Complete";
            }
            else
            {
                obeliskProgress = $"<br>{obeliskName} Progress: {obelisk.GetObeliskChargeCount()} / {obelisk.m_obeliskData.m_maxChargeCount}";
            }

            m_detailsString = m_detailsString + obeliskProgress;
        }
        
        base.OnPointerEnter(eventData);
    }
}