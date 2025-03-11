using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipWoodDisplay : UITooltip
{
    public override void OnPointerEnter(PointerEventData eventData)
    {
        //Get the current resource rate from the resource Manager;
        float resourceRate = ResourceManager.Instance.WoodPerMinute;
        string formattedResourceRate = resourceRate.ToString("F1");  //11.1
        
        m_descriptionString = LocalizationManager.Instance.CurrentLanguage.m_woodBankToolTip;
        m_detailsString = string.Format(LocalizationManager.Instance.CurrentLanguage.m_woodRateToolTip, formattedResourceRate);
        
        base.OnPointerEnter(eventData);
    }
}
