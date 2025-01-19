using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class CellOoze : MonoBehaviour
{
    public Vector3 m_goalPos;
    public Collider m_collider;
    public VisualEffect m_oozeProjectileVFX;
    public VisualEffect m_towerDisableVFX;
    public VisualEffect m_cellOozeVFX;
    public AnimationCurve animationCurve;
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private List<AudioClip> m_audioCreatedClips;
    [SerializeField] private List<AudioClip> m_audioImpactClips;

    private Cell m_cell;
    private float m_curDissolve = 1;

    void Awake()
    {
        m_collider.enabled = false;
    }
    
    void OnEnable()
    {
        m_curDissolve = 1f;
        m_cellOozeVFX.SetFloat("Dissolve", m_curDissolve);
        m_towerDisableVFX.SetFloat("Dissolve", m_curDissolve);
        GameplayManager.OnGameplayStateChanged += RemoveOoze;
    }

    public void SetOozeCell(Cell cell)
    {
        m_cell = cell;
        m_goalPos = new Vector3(cell.m_cellPos.x, 0, cell.m_cellPos.y);
        m_oozeProjectileVFX.Play();
        
        RequestPlayAudio(m_audioCreatedClips, m_audioSource);
        gameObject.transform.DOJump(m_goalPos, 4, 1, 2f)
            .SetEase(animationCurve)
            .OnComplete(() =>
            {
                SetupOoze();
                m_oozeProjectileVFX.Stop();
                 RequestPlayAudio(m_audioImpactClips, m_audioSource);
            });
    }
    
    void SetupOoze()
    {
        //What Cell am I on?
        m_cell.UpdateBuildRestrictedValue(true);
        
        //Enable the collider
        m_collider.enabled = true;
        m_cellOozeVFX.Play();
    }

    void RemoveOoze(GameplayManager.GameplayState newState)
    {
        if (newState != GameplayManager.GameplayState.Build) return;
        
        m_cell.UpdateBuildRestrictedValue(false);
        m_collider.enabled = false;
        DOTween.To(() => m_curDissolve, x => m_curDissolve = x, 0f, 0.9f)
            .OnUpdate(() =>
            {
                m_cellOozeVFX.SetFloat("Dissolve", m_curDissolve);
                m_towerDisableVFX.SetFloat("Dissolve", m_curDissolve);
            });
        m_cellOozeVFX.Stop();
        m_towerDisableVFX.Stop();
        
        GameplayManager.OnGameplayStateChanged -= RemoveOoze;
        ObjectPoolManager.OrphanObject(gameObject, 1f, ObjectPoolManager.PoolType.Enemy);
    }
    
    public void RequestPlayAudio(List<AudioClip> clips, AudioSource audioSource = null)
    {
        if (clips[0] == null) return;
        
        if (audioSource == null) audioSource = m_audioSource;
        int i = Random.Range(0, clips.Count);
        audioSource.PlayOneShot(clips[i]);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Tower tower = other.gameObject.GetComponent<Tower>();
        if (tower)
        {
            tower.RequestTowerDisable();
            m_towerDisableVFX.Play();
        }

        ResourceNode node = other.gameObject.GetComponent<ResourceNode>();
        if (node)
        {
            node.OnDepletion(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Tower tower = other.gameObject.GetComponent<Tower>();
        if (tower)
        {
            m_towerDisableVFX.Stop();
            tower.RequestTowerEnable();
        }
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= RemoveOoze;
    }
}
