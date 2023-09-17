using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class IngameUIController : MonoBehaviour
{

    public static IngameUIController Instance;

    public UITowerSelectHUD m_towerSelectHUD;
    public UIHealthMeter m_healthMeter;
    public UIAlert m_currencyAlert;
    [SerializeField] private Color m_currencyGoodcolor;
    [SerializeField] private Color m_currencyBadcolor;
    private String m_stoneIcon = "<sprite name=\"ResourceStone\">";
    private String m_woodIcon = "<sprite name=\"ResourceWood\">";
    private String m_positiveValue = "+";
    private String m_negativeValue = "-";

    private Camera m_camera;
    private Canvas m_canvas;
    private float m_screenWidth;
    private float m_screenHeight;
    
        
    
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        m_camera = Camera.main;
        m_canvas = GetComponent<Canvas>();
    }

    public void SpawnCurrencyAlert(int woodValue, int stoneValue, bool isGood, Vector3 worldPos)
    {
        UIAlert uiAlert = Instantiate(m_currencyAlert, transform);
        RectTransform rectTransform = uiAlert.GetComponent<RectTransform>();
        
        Vector2 screenPos = GetScreenPosition(worldPos);
        rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);

        Color textColor = isGood ? m_currencyGoodcolor : m_currencyBadcolor;

        string woodMagnitude;
        string stoneMagnitude;
        if (isGood)
        {
            woodMagnitude = woodValue > 0 ? m_positiveValue : m_negativeValue;
            stoneMagnitude = stoneValue > 0 ? m_positiveValue : m_negativeValue;
        }
        else
        {
            woodMagnitude = woodValue > 0 ? m_negativeValue : m_positiveValue;
            stoneMagnitude = stoneValue > 0 ? m_negativeValue : m_positiveValue;
        }
        
        string alertString;
        if (stoneValue > 0)
        {
            alertString = $"{woodMagnitude}{woodValue}{m_woodIcon}  {stoneMagnitude}{stoneValue}{m_stoneIcon}";
        }
        else
        {
            alertString = $"{woodMagnitude}{woodValue}{m_woodIcon}";
        }
        
        uiAlert.SetLabelText($"{alertString}", textColor);
    }

    public void SpawnHealthAlert(int healthValue, Vector3 worldPos)
    {
        UIAlert uiAlert = Instantiate(m_currencyAlert, transform);
        RectTransform rectTransform = uiAlert.GetComponent<RectTransform>();
        
        Vector2 screenPos = GetScreenPosition(worldPos);
        rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        string alertString = $"-{healthValue}<sprite name=\"ResourceHealth\">";
        uiAlert.SetLabelText($"{alertString}", m_currencyBadcolor);
    }

    private Vector2 GetScreenPosition(Vector3 pos)
    {
        m_screenWidth = Screen.width;
        m_screenHeight = Screen.height;
        Vector2 screenPos = m_camera.WorldToScreenPoint(pos);
        screenPos.x -= m_screenWidth / 2;
        screenPos.y -= m_screenHeight / 2 - 35f;
        return screenPos;
    }
}
