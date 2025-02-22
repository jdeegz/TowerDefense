using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class Obelisk : MonoBehaviour
{
    public ObeliskData m_obeliskData;
    public VisualEffect m_chargedVFXGroup;
    public VisualEffect m_spireBeamVFX;
    public LineRenderer m_obeliskRangeCircle;
    public GameObject m_targetPoint;
    public GameObject m_blockObj;
    public Renderer m_meterRenderer;

    private Material m_meterMaterial;
    private string m_meterScrollParameter = "_DissolveValue";
    private float m_obeliskRadius;
    private int m_curChargeCount;
    private int m_maxChargeCount;
    private List<Cell> m_cellsInRange;
    private Cell m_cell;
    private AudioSource m_audioSource;
    private bool m_spireBeamVFXIsPlaying;

    public enum ObeliskState
    {
        Charging,
        Charged
    }

    public ObeliskState m_obeliskState;

    public event Action<ObeliskState> OnObeliskStateChanged;
    public event Action<int> OnObeliskChargeChanged;

    public void UpdateObeliskState(ObeliskState newState)
    {
        m_obeliskState = newState;

        switch (newState)
        {
            case ObeliskState.Charging:
                m_chargedVFXGroup.Stop();
                break;
            case ObeliskState.Charged:
                m_chargedVFXGroup.Play();
                RequestPlayAudio(m_obeliskData.m_obeliskChargedClip);
                GameplayManager.Instance.CheckObeliskStatus();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }

        OnObeliskStateChanged?.Invoke(newState);
    }

    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        OnObeliskChargeChanged += ObeliskChargeChanged;

        //Setup data
        m_maxChargeCount = m_obeliskData.m_maxChargeCount;
        m_curChargeCount = 0;
        m_obeliskRadius = m_obeliskData.m_obeliskRange;

        m_audioSource = GetComponent<AudioSource>();
        m_meterMaterial = m_meterRenderer.material;
        m_meterMaterial.SetFloat(m_meterScrollParameter, 1);
    }

    private float m_curBeamDissolve;
    public void HandleSpireBeamVFX(bool value)
    {
        if (value == m_spireBeamVFXIsPlaying) return;

        m_spireBeamVFXIsPlaying = value;

        if (m_spireBeamVFXIsPlaying)
        {
            m_spireBeamVFX.Play();
            
            m_curBeamDissolve = 1;
            m_spireBeamVFX.SetFloat("Dissolve", m_curBeamDissolve);
            //Start Effects
            DOTween.To(() => m_curBeamDissolve, x => m_curBeamDissolve = x, 0f, .66f)
                .OnUpdate(() => m_spireBeamVFX.SetFloat("Dissolve", m_curBeamDissolve))
                .SetDelay(.33f)
                .SetUpdate(true);
        }
        else
        {
            //Dissolve Effect. Effects
            DOTween.To(() => m_curBeamDissolve, x => m_curBeamDissolve = x, 1f, .33f)
                .OnUpdate(() => m_spireBeamVFX.SetFloat("Dissolve", m_curBeamDissolve))
                .SetUpdate(true)
                .OnComplete(() => m_spireBeamVFX.Stop());
        }
    }


    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        OnObeliskChargeChanged -= ObeliskChargeChanged;
    }

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            //Set occupancy
            GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
            if (m_blockObj != null) GridCellOccupantUtil.SetBuildRestricted(m_blockObj, true, 1, 1);
            //GridCellOccupantUtil.SetBuildRestricted(gameObject, true, 1, 3);

            SetupRangeCircle(48, m_obeliskRadius);

            GameplayManager.Instance.AddObeliskToList(this);
        }
    }

    public void PlayAudio(AudioClip audioClip)
    {
        m_audioSource.PlayOneShot(audioClip);
    }

    public void IncreaseObeliskCharge(int i)
    {
        switch (m_obeliskState)
        {
            case ObeliskState.Charging:
                if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Defeat) return; // Don't increment if the game is over. (Edge case handling)
                if (m_curChargeCount >= m_maxChargeCount) return; // Dont increment if we're capped.

                m_curChargeCount += i;
                
                // METER
                float progress = (float)m_curChargeCount / m_maxChargeCount;
                m_meterMaterial.SetFloat(m_meterScrollParameter, 1 - progress);

                // AUDIO
                RequestPlayAudio(m_obeliskData.m_soulCollectedClips);

                // CHARGED CHECK
                if (m_curChargeCount >= m_maxChargeCount)
                {
                    UpdateObeliskState(ObeliskState.Charged);
                }

                OnObeliskChargeChanged?.Invoke(m_curChargeCount);
                break;
            case ObeliskState.Charged:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public float GetObeliskProgress()
    {
        return (float)m_curChargeCount / m_maxChargeCount;
    }

    public int GetObeliskChargeCount()
    {
        return m_curChargeCount;
    }
    
    public int GetObeliskMaxChargeCount()
    {
        return m_maxChargeCount;
    }

    private void ObeliskChargeChanged(int i)
    {
        //This is not in use at the moment.
        //Debug.Log($"Obelisk {gameObject.name} charge changed.");
    }

    void SetupRangeCircle(int segments, float radius)
    {
        m_obeliskRangeCircle.positionCount = segments;
        m_obeliskRangeCircle.startWidth = 0.06f;
        m_obeliskRangeCircle.endWidth = 0.06f;
        for (int i = 0; i < segments; ++i)
        {
            float circumferenceProgress = (float)i / segments;
            float currentRadian = circumferenceProgress * 2 * Mathf.PI;
            float xScaled = Mathf.Cos(currentRadian);
            float yScaled = Mathf.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            Vector3 currentPosition = new Vector3(x, 0.01f, y);

            m_obeliskRangeCircle.SetPosition(i, currentPosition);
        }
    }

    public void SetCharge(int i)
    {
        m_curChargeCount = i;
    }

    public ObeliskTooltipData GetTooltipData()
    {
        ObeliskTooltipData data = new ObeliskTooltipData();
        data.m_obeliskName = m_obeliskData.m_obeliskName;
        data.m_obeliskDescription = m_obeliskData.m_obeliskDescription;
        data.m_obeliskCurCharge = m_curChargeCount;
        data.m_obeliskMaxCharge = m_maxChargeCount;
        return data;
    }

    public void RequestPlayAudio(AudioClip clip)
    {
        //source.Stop();
        m_audioSource.PlayOneShot(clip);
    }

    public void RequestPlayAudio(List<AudioClip> clips)
    {
        int i = Random.Range(0, clips.Count);
        m_audioSource.PlayOneShot(clips[i]);
    }
}

public class ObeliskTooltipData
{
    public string m_obeliskName;
    public string m_obeliskDescription;
    public int m_obeliskCurCharge;
    public int m_obeliskMaxCharge;
}