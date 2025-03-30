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
    public UIAlert m_levelLostAlert;
    public UIAlert m_gathererIdleAlert;
    public UIAlert m_ruinAlert;
    public RectTransform m_healthMeterBossRect;
    
    [Header("Colors")]
    [SerializeField] private Color m_currencyGoodcolor;
    [SerializeField] private Color m_currencyCritcolor;
    [SerializeField] private Color m_currencyBadcolor;
    [SerializeField] private Color m_levelUpColor;
    [SerializeField] private Color m_ruinColor;
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
    }

    public void SpawnCurrencyAlert(int woodValue, int stoneValue, bool isGood, Vector3 worldPos)
    {
        var values = SetCurrencyAlertValues(woodValue, stoneValue, isGood);
        
        //Build the alert.
        UIAlert alert = ObjectPoolManager.SpawnObject(m_currencyAlert.gameObject, transform).GetComponent<UIAlert>();
        Vector2 screenPos = GetScreenPosition(worldPos);
        alert.SetLabelText($"{values.Item1}", values.Item2);
        alert.SetupAlert(screenPos);
    }
    
    public void SpawnCritCurrencyAlert(int woodValue, int stoneValue, bool isGood, Vector3 worldPos)
    {
        var values = SetCurrencyAlertValues(woodValue, stoneValue, isGood);
        
        //Build the alert.
        UIAlert alert = ObjectPoolManager.SpawnObject(m_critCurrencyAlert.gameObject, transform).GetComponent<UIAlert>();
        Vector2 screenPos = GetScreenPosition(worldPos);
        alert.SetLabelText($"{values.Item1}", m_currencyCritcolor);
        alert.SetupAlert(screenPos);
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
        
        string alertString = $"Something Broke. Wood Value {woodValue}, Stone Value {stoneValue}.";
        /*if (woodValue <= 0 || stoneValue <= 0)
        {
            Debug.Log($"Wood Value {woodValue}, Stone Value {stoneValue}");
        }*/
        
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
        string alertString = string.Format(m_uiStringData.m_gathererLevelUp, obj.name);
        
        UIAlert alert = Instantiate(m_levelUpAlert, transform);
        Vector2 screenPos = GetScreenPosition(worldPos);
        alert.SetLabelText($"{alertString}", m_levelUpColor);
        alert.SetupAlert(screenPos);
    }
    
    public void SpawnLevelLostAlert(GameObject obj, Vector3 worldPos)
    {
        string alertString = string.Format(m_uiStringData.m_gathererLevelLost, obj.name);
        
        UIAlert alert = Instantiate(m_currencyAlert, transform);
        Vector2 screenPos = GetScreenPosition(worldPos);
        alert.SetLabelText($"{alertString}", m_currencyBadcolor);
        alert.SetupAlert(screenPos);
    }
    
    public void SpawnRuinDiscoveredAlert(Vector3 worldPos, string unlockableName, int requirementTotal, int requirementsMet)
    {
        string alertString;
        if (requirementsMet >= requirementTotal)
        {
            // unlockableName Unlocked!
            string name = unlockableName.Replace("_ProgressionUnlockableData", "");
            alertString = string.Format(m_uiStringData.m_unlockableUnlocked, name);
        }
        else
        {
            // 2 / 3 Ruins Discovered!
            alertString = string.Format(m_uiStringData.m_ruinDiscovered, requirementsMet, requirementTotal);
        }
       
        
        UIAlert alert = Instantiate(m_ruinAlert, transform);
        Vector2 screenPos = GetScreenPosition(worldPos);
        alert.SetLabelText($"{alertString}", m_ruinColor);
        alert.SetupAlert(screenPos);
        
    }

    public void SpawnHealthAlert(int healthValue, Vector3 worldPos)
    {
        string alertString = $"-{healthValue}<sprite name=\"ResourceHealth\">";
        
        UIAlert alert = Instantiate(m_currencyAlert, transform);
        Vector2 screenPos = GetScreenPosition(worldPos);
        alert.SetLabelText($"{alertString}", m_currencyBadcolor);
        alert.SetupAlert(screenPos);
        
    }
    
    public void SpawnMaxHealthAlert(int healthValue, Vector3 worldPos)
    {
        string alertString = $"-{healthValue}<sprite name=\"ResourceHealth\"> {m_uiStringData.m_bossDamageType}";
        
        UIAlert alert = Instantiate(m_currencyAlert, transform);
        Vector2 screenPos = GetScreenPosition(worldPos);
        alert.SetLabelText($"{alertString}", m_currencyBadcolor);
        alert.SetupAlert(screenPos);
        
    }

    private Vector2 GetScreenPosition(Vector3 pos)
    {
        m_screenWidth = Screen.width / m_canvas.scaleFactor;
        m_screenHeight = Screen.height / m_canvas.scaleFactor;
        
        Vector3 viewportPos = m_camera.WorldToViewportPoint(pos);
        viewportPos.x = viewportPos.x * m_screenWidth - m_screenWidth * 0.5f;
        viewportPos.y = (viewportPos.y * m_screenHeight - m_screenHeight * 0.5f) - 35f;
        
        return viewportPos;
    }
}
