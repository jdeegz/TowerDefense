using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITooltip : Selectable, IPointerEnterHandler, IPointerExitHandler 
{
    public string m_nameString;
    [TextArea(10,4)]
    public string m_descriptionString;
    public string m_detailsString;
    
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        //Get the selectable component related to this button.
        UITooltipController.Instance.SetUISelectable(this);
    }

    public void OnPointerExit(PointerEventData evenData)
    {
        UITooltipController.Instance.SetUISelectable(null);
    }
}
