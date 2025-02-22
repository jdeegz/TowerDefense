using System;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class CellOoze : MonoBehaviour
{
    public Vector3 m_goalPos;
    public Collider m_collider;

    public float m_oozeLifetime = 20f;
    public float m_oozeTravelTime = 2f;
    public GameObject m_oozeProjectileObj;
    private GameObject m_activeOozeProjectileObj;

    public GameObject m_towerDisableObj;
    private GameObject m_activeTowerDisableObj;

    public GameObject m_cellOozeObj;
    private GameObject m_activeCellOozeObj;

    public AnimationCurve animationCurve;

    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private List<AudioClip> m_audioCreatedClips;
    [SerializeField] private List<AudioClip> m_audioImpactClips;

    private Cell m_cell;
    private float m_curDissolve = 1;
    private bool m_isActive = false;
    private float m_timeElapsed;

    public void OnEnable()
    {
        m_collider.enabled = false;
        m_isActive = true;
        m_timeElapsed = 0;
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
    }

    private void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.Build && m_isActive)
        {
            RemoveOoze();
        }
    }

    private void Update()
    {
        if (m_isActive)
        {
            HandleOozeTimer();
        }
    }

    private void HandleOozeTimer()
    {
        m_timeElapsed += Time.deltaTime;

        if (m_timeElapsed >= m_oozeLifetime + m_oozeTravelTime) RemoveOoze();
    }

    private Tween m_spawnTween;

    public void SetOozeCell(Cell cell)
    {
        m_cell = cell;
        m_goalPos = new Vector3(cell.m_cellPos.x, 0, cell.m_cellPos.y);
        m_activeOozeProjectileObj = ObjectPoolManager.SpawnObject(m_oozeProjectileObj, transform.position, quaternion.identity, transform, ObjectPoolManager.PoolType.ParticleSystem);

        RequestPlayAudio(m_audioCreatedClips, m_audioSource);

        m_spawnTween = gameObject.transform.DOJump(m_goalPos, 4, 1, m_oozeTravelTime)
            .SetEase(animationCurve)
            .OnComplete(() => { SetupOoze(); });
    }

    void SetupOoze()
    {
        if (!m_isActive) return;

        ObjectPoolManager.ReturnObjectToPool(m_activeOozeProjectileObj);
        m_activeOozeProjectileObj = null;

        RequestPlayAudio(m_audioImpactClips, m_audioSource);

        //What Cell am I on?
        m_cell.UpdateBuildRestrictedValue(true);

        //Enable the collider
        m_collider.enabled = true;

        m_activeCellOozeObj = ObjectPoolManager.SpawnObject(m_cellOozeObj, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
    }

    void RemoveOoze()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
        m_isActive = false;
        m_spawnTween.Kill(false);
        m_cell.UpdateBuildRestrictedValue(false);
        m_collider.enabled = false;

        if (m_activeOozeProjectileObj) ObjectPoolManager.ReturnObjectToPool(m_activeOozeProjectileObj);
        if (m_activeCellOozeObj) ObjectPoolManager.ReturnObjectToPool(m_activeCellOozeObj);
        if (m_activeTowerDisableObj) ObjectPoolManager.ReturnObjectToPool(m_activeTowerDisableObj);
        if (m_disabledTower != null) m_disabledTower.RequestTowerEnable();

        m_activeOozeProjectileObj = null;
        m_activeCellOozeObj = null;
        m_activeTowerDisableObj = null;
        m_disabledTower = null;

        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }

    public void RequestPlayAudio(List<AudioClip> clips, AudioSource audioSource = null)
    {
        if (clips[0] == null) return;

        if (audioSource == null) audioSource = m_audioSource;
        int i = Random.Range(0, clips.Count);
        audioSource.PlayOneShot(clips[i]);
    }

    private Tower m_disabledTower = null;

    private void OnTriggerEnter(Collider other)
    {
        ResourceNode node = other.gameObject.GetComponent<ResourceNode>();
        if (node)
        {
            node.OnDepletion(false);
            return;
        }

        // Do we already have a tower? we shouldn't!
        if (m_disabledTower != null)
        {
            return;
        }

        m_disabledTower = other.gameObject.GetComponent<Tower>();

        if (m_disabledTower != null && m_disabledTower.GetTowerData().m_buildingSize == Vector2Int.one)
        {
            // We found a tower, request to disable it.
            m_disabledTower.RequestTowerDisable();
            m_activeTowerDisableObj = ObjectPoolManager.SpawnObject(m_towerDisableObj, transform.position, Quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
        }
    }

    private void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
    }
}