using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CastleController : MonoBehaviour
{
    public int m_maxHealth = 20;

    [SerializeField] private int m_repairHealthAmount;
    [SerializeField] private float m_repairHealthInterval;
    [SerializeField] private List<GameObject> m_castleCorners;
    [SerializeField] public List<GameObject> m_castleEntrancePoints;
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip m_audioHealthGained;
    [SerializeField] private AudioClip m_audioHealthLost;
    [SerializeField] private AudioClip m_audioResourceGained;
    [SerializeField] private AudioClip m_audioResourceLost;
    [SerializeField] private AudioClip m_audioWaveStart;
    [SerializeField] private AudioClip m_audioWaveEnd;

    private List<MeshRenderer> m_allMeshRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;
    private int m_curHealth;
    private float m_repairElapsedTime;

    public event Action<int> UpdateHealth;
    public event Action DestroyCastle;

    // Start is called before the first frame update
    void Awake()
    {
        CollectMeshRenderers(transform);

        m_curHealth = m_maxHealth;
        UpdateHealth += OnUpdateHealth;
        DestroyCastle += OnCastleDestroyed;

        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        ResourceManager.UpdateStoneBank += OnBankUpdated;
        ResourceManager.UpdateWoodBank += OnBankUpdated;
        m_audioSource = GetComponent<AudioSource>();
    }

    void OnBankUpdated(int amount, int total)
    {
        if (amount > 0)
        {
            PlayAudio(m_audioResourceGained);
        }
        else
        {
            PlayAudio(m_audioResourceLost);
        }
    }

    public void PlayAudio(AudioClip audioClip)
    {
        m_audioSource.PlayOneShot(audioClip);
    }

    void OnDestroy()
    {
        UpdateHealth -= OnUpdateHealth;
        DestroyCastle -= OnCastleDestroyed;

        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
        ResourceManager.UpdateStoneBank -= OnBankUpdated;
        ResourceManager.UpdateWoodBank -= OnBankUpdated;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        switch (newState)
        {
            case GameplayManager.GameplayState.BuildGrid:
                break;
            case GameplayManager.GameplayState.PlaceObstacles:
                foreach (GameObject obj in m_castleCorners)
                {
                    GridCellOccupantUtil.SetOccupant(obj, true, 1, 1);
                }
                break;
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                PlayAudio(m_audioWaveStart);
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                PlayAudio(m_audioWaveEnd);
                break;
            case GameplayManager.GameplayState.Paused:
                break;
            case GameplayManager.GameplayState.Victory:
                break;
            case GameplayManager.GameplayState.Defeat:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameplayManager.Instance.m_gameplayState == GameplayManager.GameplayState.Build &&
            m_curHealth < m_maxHealth)
        {
            m_repairElapsedTime += Time.deltaTime;
            if (m_repairElapsedTime >= m_repairHealthInterval)
            {
                m_repairElapsedTime = 0;
                UpdateHealth?.Invoke(m_repairHealthAmount);
                //Debug.Log("Castle Repaired.");
            }
        }
    }

    public float RepairProgress()
    {
        return m_repairElapsedTime / m_repairHealthInterval;
    }

    public void TakeDamage(int dmg)
    {
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());
        IngameUIController.Instance.SpawnHealthAlert(1, transform.position);
        UpdateHealth?.Invoke(-dmg);
    }

    void OnUpdateHealth(int i)
    {
        m_curHealth += i;

        if (i > 0)
        {
            PlayAudio(m_audioHealthGained);
        }
        else
        {
            PlayAudio(m_audioHealthLost);
        }

        if (m_curHealth <= 0)
        {
            DestroyCastle?.Invoke();
        }
    }

    void OnCastleDestroyed()
    {
        GameplayManager.Instance.UpdateGameplayState(GameplayManager.GameplayState.Defeat);
        //Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        //Set the color
        for (int i = 0; i < m_allMeshRenderers.Count; ++i)
        {
            m_allMeshRenderers[i].material.SetColor("_EmissionColor", Color.red);
        }

        yield return new WaitForSeconds(.15f);

        //Return to original colors.
        for (int i = 0; i < m_allMeshRenderers.Count; ++i)
        {
            m_allMeshRenderers[i].material.SetColor("_EmissionColor", m_allOrigColors[i]);
        }
    }

    private void CollectMeshRenderers(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        MeshRenderer meshRenderer = parent.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (m_allMeshRenderers == null)
            {
                m_allMeshRenderers = new List<MeshRenderer>();
            }

            if (m_allOrigColors == null)
            {
                m_allOrigColors = new List<Color>();
            }

            m_allMeshRenderers.Add(meshRenderer);
            m_allOrigColors.Add(meshRenderer.material.GetColor("_EmissionColor"));
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            //Recursivly add meshrenderers by calling this function again with the child as the transform.
            Transform child = parent.GetChild(i);
            CollectMeshRenderers(child);
        }
    }
}