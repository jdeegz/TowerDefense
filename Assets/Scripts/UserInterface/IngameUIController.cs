using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngameUIController : MonoBehaviour
{

    public static IngameUIController Instance;

    public UITowerSelectHUD m_towerSelectHUD;
    public UIHealthMeter m_healthMeter;
    
    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
