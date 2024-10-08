using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class IngameUIController : MonoBehaviour
{

    public static IngameUIController Instance;

    public UIStringData m_uiStringData;
    public UITowerSelectHUD m_towerSelectHUD;
    
    [Header("Meter Prefabs")]
    public UIHealthMeter m_healthMeter;
    public UIHealthMeter m_healthMeterBoss;
    public UIIngameMeter m_ingameMeter;
    
    [Header("Alert Prefabs")]
    public UIAlert m_currencyAlert;
    public UIAlert m_critCurrencyAlert;
    public UIAlert m_levelUpAlert;
    public UIAlert m_gathererIdleAlert;
    public RectTransform m_healthMeterBossRect;
    
    [Header("Colors")]
    [SerializeField] private Color m_currencyGoodcolor;
    [SerializeField] private Color m_currencyBadcolor;
    [SerializeField] private Color m_levelUpColor;
    [SerializeField] private Color m_idleColor;
    private String m_stoneIcon = "<sprite name=\"ResourceStone\">";
    private String m_woodIcon = "<sprite name=\"ResourceWood\">";
    private String m_positiveValue = "+";
    private String m_negativeValue = "-";

    private Camera m_camera;
    private Canvas m_canvas;
    private float m_screenWidth;
    private float m_screenHeight;


    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        m_camera = Camera.main;
        m_canvas = GetComponentInParent<Canvas>();
        m_screenWidth = Screen.width / m_canvas.scaleFactor;
        m_screenHeight = Screen.height / m_canvas.scaleFactor;
    }
    
    public void SpawnIdleAlert(GameObject obj, Vector3 worldPos)
    {
        UIAlert uiAlert = Instantiate(m_gathererIdleAlert, transform);
        RectTransform rectTransform = uiAlert.GetComponent<RectTransform>();
        
        Vector2 screenPos = GetScreenPosition(worldPos);
        rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        string alertString = string.Format(m_uiStringData.m_gathererIdle, obj.name);
        uiAlert.SetLabelText($"{alertString}", m_idleColor);
    }

    public void SpawnCurrencyAlert(int woodValue, int stoneValue, bool isGood, Vector3 worldPos)
    {
        var values = SetCurrencyAlertValues(woodValue, stoneValue, isGood);
        
        //Build the alert.
        //GameObject uiAlertObj = ObjectPoolManager.SpawnObject(m_currencyAlert.gameObject, transform);
        UIAlert uiAlert = Instantiate(m_currencyAlert, transform);
        RectTransform rectTransform = uiAlert.GetComponent<RectTransform>();
        
        Vector2 screenPos = GetScreenPosition(worldPos);
        rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        uiAlert.SetLabelText($"{values.Item1}", values.Item2);
    }
    
    public void SpawnCritCurrencyAlert(int woodValue, int stoneValue, bool isGood, Vector3 worldPos)
    {
        var values = SetCurrencyAlertValues(woodValue, stoneValue, isGood);
        
        //Build the alert.
        //GameObject uiAlertObj = ObjectPoolManager.SpawnObject(m_currencyAlert.gameObject, transform);
        UIAlert uiAlert = Instantiate(m_critCurrencyAlert, transform);
        RectTransform rectTransform = uiAlert.GetComponent<RectTransform>();
        
        Vector2 screenPos = GetScreenPosition(worldPos);
        rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        uiAlert.SetLabelText($"{values.Item1}", values.Item2);
    }
    
    public (string, Color) SetCurrencyAlertValues(int woodValue, int stoneValue, bool isGood)
    {
        //Define the text
        Color textColor = isGood ? m_currencyGoodcolor : m_currencyBadcolor;

        string woodMagnitude;
        string stoneMagnitude;
        if (isGood) //Determine + or -
        {
            woodMagnitude = woodValue > 0 ? m_positiveValue : m_negativeValue;
            stoneMagnitude = stoneValue > 0 ? m_positiveValue : m_negativeValue;
        }
        else
        {
            woodMagnitude = woodValue > 0 ? m_negativeValue : m_positiveValue;
            stoneMagnitude = stoneValue > 0 ? m_negativeValue : m_positiveValue;
        }
        
        string alertString = "Something Broke.";
        
        if (woodValue > 0 && stoneValue > 0)
        {
            alertString = $"{woodMagnitude}{woodValue}{m_woodIcon}  {stoneMagnitude}{stoneValue}{m_stoneIcon}";
        }

        if (stoneValue > 0)
        {
            alertString = $"{stoneMagnitude}{stoneValue}{m_stoneIcon}";
        }
        
        if(woodValue > 0)
        {
            alertString = $"{woodMagnitude}{woodValue}{m_woodIcon}";
        }
        
        return (alertString, textColor);
    }

    public void SpawnLevelUpAlert(GameObject obj, Vector3 worldPos)
    {
        UIAlert uiAlert = Instantiate(m_levelUpAlert, transform);
        RectTransform rectTransform = uiAlert.GetComponent<RectTransform>();
        
        Vector2 screenPos = GetScreenPosition(worldPos);
        rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        string alertString = string.Format(m_uiStringData.m_gathererLevelUp, obj.name);
        uiAlert.SetLabelText($"{alertString}", m_levelUpColor);
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
    
    public void SpawnMaxHealthAlert(int healthValue, Vector3 worldPos)
    {
        UIAlert uiAlert = Instantiate(m_currencyAlert, transform);
        RectTransform rectTransform = uiAlert.GetComponent<RectTransform>();
        
        Vector2 screenPos = GetScreenPosition(worldPos);
        rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        string alertString = $"-{healthValue}<sprite name=\"ResourceHealth\"> CRITICAL DAMAGE!";
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
