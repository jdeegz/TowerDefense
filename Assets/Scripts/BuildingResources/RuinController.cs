using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RuinController : MonoBehaviour
{
    public int m_ruinWeight = 1;
    public RuinState m_ruinState;

    public enum RuinState
    {
        Idle,
        Hidden, // On Awake
        Indicated, // By Resource Manager
        Discovered, // By Harvesting
        Activated, // Differs per type
    }

    private RuinIndicator m_ruinIndicator;
    private ProgressionKeyData m_progressionKey;

    public ProgressionKeyData ProgressionKey => m_progressionKey;

    private void UpdateRuinState(RuinState newState)
    {
        m_ruinState = newState;

        switch (newState)
        {
            case RuinState.Idle:
                break;
            case RuinState.Hidden:
                break;
            case RuinState.Indicated:
                m_ruinIndicator.ToggleRuinRelic(true);
                break;
            case RuinState.Discovered:
                m_ruinIndicator.ToggleRuinRelic(false);
                break;
            case RuinState.Activated:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    public void IndicateThisRuin(ProgressionKeyData key, RuinIndicator ruinIndicator)
    {
        GameObject ruinObj = ruinIndicator.gameObject;
        GameObject ruinIndicatorObj = ObjectPoolManager.SpawnObject(ruinObj, gameObject.transform.position, Quaternion.identity, transform, ObjectPoolManager.PoolType.GameObject);
        GridCellOccupantUtil.SetOccupant(ruinIndicatorObj, true, 1, 1);

        // Check the state of the key, to determine if this is discovered previously.
        m_progressionKey = key;
        m_progressionKey.KeyChanged += OnKeyChanged;
        
        m_ruinIndicator = ruinIndicatorObj.GetComponent<RuinIndicator>();
        m_ruinIndicator.SetUpRuinIndicator(this);

        // Update ruin controller state.
        if (m_progressionKey.ProgressionKeyEnabled)
        {
            UpdateRuinState(RuinState.Discovered);
        }
        else
        {
            UpdateRuinState(RuinState.Indicated);
        }
        
    }

    public void GathererDiscoveredRuin()
    {
        // The node was harvested, but we're not indicated so we're not discovered.
        if (m_ruinState != RuinState.Indicated) return;

        // We've discovered the ruin.
        PlayerDataManager.Instance.RequestUnlockKey(m_progressionKey);

        // The Unlockable Progress this key belongs to and display an alert based on progress.
        ProgressionUnlockableData unlockableData = PlayerDataManager.Instance.m_progressionTable.GetUnlockableFromKey(m_progressionKey);
        UnlockProgress unlockProgress = unlockableData.GetProgress();

        if (unlockProgress.m_isUnlocked)
        {
            TowerData rewardData = unlockableData.GetRewardData().GetReward();
            UIPopupManager.Instance.ShowPopup<UITowerUnlockedPopup>("TowerUnlocked", rewardData);
        }
        else
        {
            Vector3 pos = transform.position;
            pos.y += 2f; // Vertical offset to spawn alert at.
            IngameUIController.Instance.SpawnRuinDiscoveredAlert(pos, unlockableData.name, unlockProgress.m_requirementTotal, unlockProgress.m_requirementsMet);
        }
    }

    void OnKeyChanged(bool value)
    {
        if (value)
        {
            UpdateRuinState(RuinState.Discovered);
        }
        else
        {
            UpdateRuinState(RuinState.Indicated);
        }
    }

    void OnDestroy()
    {
        if (m_ruinIndicator != null)
        {
            m_progressionKey.KeyChanged -= OnKeyChanged;
        }
    }
}