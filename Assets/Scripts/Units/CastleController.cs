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
    }

    void OnDestroy()
    {
        UpdateHealth -= OnUpdateHealth;
        DestroyCastle -= OnCastleDestroyed;

        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            foreach (GameObject obj in m_castleCorners)
            {
                GridCellOccupantUtil.SetOccupant(obj, true, 1, 1);
            }
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