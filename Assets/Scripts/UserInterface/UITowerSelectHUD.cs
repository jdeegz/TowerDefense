using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UITowerSelectHUD : MonoBehaviour
{
    [SerializeField] private Track3dObject m_track3dObject;
    [SerializeField] private Button m_sellButton;
    [SerializeField] private TextMeshProUGUI m_sellButtonlabel;
    [SerializeField] private int m_stoneValue;
    [SerializeField] private int m_woodValue;
    
    private TowerController m_curTower;


    void Awake()
    {
    }
    
    void Start()
    {
        ToggleTowerSelectHUD(false);
    }

    void Update()
    {
    }

    public void ToggleTowerSelectHUD(bool b)
    {
        gameObject.SetActive(b);
    }

    public void SelectTower(GameObject obj)
    {
        m_curTower = obj.GetComponent<TowerController>();
        m_track3dObject.SetupTracking(obj, GetComponent<RectTransform>());
        
        //Set assets
        ValueTuple<int, int> vars = m_curTower.GetTowerSellCost();
        m_stoneValue = vars.Item1;
        m_woodValue = vars.Item2;
        
        //Sell Button Action
        m_sellButton.onClick.RemoveAllListeners();
        m_sellButton.onClick.AddListener(RequestSellTower);
        
        //Sell Button Text
        string sellText;
        if (vars.Item1 > 0)
        {
            sellText = $"{m_stoneValue}<sprite name=\"ResourceStone\"><br>{m_woodValue}<sprite name=\"ResourceWood\">";
        }
        else
        {
            sellText = $"{m_woodValue}<sprite name=\"ResourceWood\">";
        }
        m_sellButtonlabel.SetText(sellText);
        
        ToggleTowerSelectHUD(true);
    }

    private void RequestSellTower()
    {
        GameplayManager.Instance.SellTower(m_curTower, m_stoneValue, m_woodValue);
        DeselectTower();
    }

    public void DeselectTower()
    {
        m_curTower = null;
        m_track3dObject.StopTracking();
        ToggleTowerSelectHUD(false);
    }
}