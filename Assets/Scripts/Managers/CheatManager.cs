using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        
        //Give Obelisk progress
        if (Input.GetKeyDown(KeyCode.I))
        {
            GiveObeliskProgress();
        }
        
        //Decrement Wave
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            ModifyWave(-1);
        }
        
        //Increment Wave
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            ModifyWave(1);
        }
        
        //Trigger Victory
        if (Input.GetKeyDown(KeyCode.K))
        {
            TriggerGameStateChange(GameplayManager.GameplayState.Victory);
        }
        
        //Trigger Defeat
        if (Input.GetKeyDown(KeyCode.L))
        {
            TriggerGameStateChange(GameplayManager.GameplayState.Defeat);
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

    void ModifyWave(int i)
    {
        GameplayManager.Instance.m_wave += i;
        Debug.Log($"Wave adjusted to: {GameplayManager.Instance.m_wave}");
    }
    
    void TriggerGameStateChange(GameplayManager.GameplayState newState)
    {
        GameplayManager.Instance.UpdateGameplayState(newState);
    }

    void GiveObeliskProgress()
    {
        foreach (Obelisk obelisk in GameplayManager.Instance.m_obelisksInMission)
        {
            if (obelisk.m_obeliskState == Obelisk.ObeliskState.Charged) continue;
            obelisk.IncreaseObeliskCharge(Random.Range(0, 30));
        }
    }
}
#endif