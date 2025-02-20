using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CheatManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    
    void Update()
    {
        //Give Wood
        if (!Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.O))
        {
            GiveWood();
            GiveStone();
        }
        
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.O))
        {
            SetWood();
            SetStone();
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
        
        //Increment Wave
        if (Input.GetKeyDown(KeyCode.RightBracket) && Input.GetKey(KeyCode.LeftShift))
        {
            ModifyWave(10);
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
        
        //Kill all enemies
        if (Input.GetKeyDown(KeyCode.U))
        {
            TriggerKillAll();
        }
    }

    void TriggerKillAll()
    {
        GameplayManager.Instance.KillAllEnemies();
    }

    void GiveWood()
    {
        ResourceManager.Instance.UpdateWoodAmount(500);
    }
    
    void SetWood()
    {
        ResourceManager.Instance.SetWoodAmount(Random.Range(5,20));
    }
    
    void GiveStone()
    {
        ResourceManager.Instance.UpdateStoneAmount(500);
    }
    
    void SetStone()
    {
        ResourceManager.Instance.SetStoneAmount(Random.Range(0, 8));
    }

    void GiveMaxHealth()
    {
        GameplayManager.Instance.m_castleController.CheatCastleHealth();
    }

    void ModifyWave(int i)
    {
        GameplayManager.Instance.Wave += i;
        Debug.Log($"Wave adjusted to: {GameplayManager.Instance.Wave}");
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
            obelisk.IncreaseObeliskCharge(Random.Range(340, 345));
        }
    }
#endif
}
