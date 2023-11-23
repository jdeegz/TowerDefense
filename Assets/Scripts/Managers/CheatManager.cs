using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if CHEATS_ENABLED
public class CheatManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //Give Wood
        if (Input.GetKeyDown(KeyCode.O))
        {
            GiveWood();
        }
        
        //Set Max Life & Give life
        if (Input.GetKeyDown(KeyCode.P))
        {
            GiveMaxHealth();
        }
    }

    void GiveWood()
    {
        ResourceManager.Instance.UpdateWoodAmount(100);
    }

    void GiveMaxHealth()
    {
        GameplayManager.Instance.m_castleController.CheatCastleHealth();
    }
}
#endif