using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class CastleController : MonoBehaviour
{
    public CastleData m_castleData;

    [SerializeField] public List<GameObject> m_castleEntrancePoints;
    [SerializeField] private Transform m_modelsParent;
    private List<Renderer> m_allRenderers;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;
    private AudioSource m_audioSource;

    private int m_maxHealth;
    private int m_curHealth;
    private int m_repairHealthAmount;
    private int m_repairFrequency;
    private float m_repairElapsedTime;
    private int m_healthRepaired = 0;

    public event Action<int> UpdateHealth;
    public event Action<int> UpdateMaxHealth;
    public event Action DestroyCastle;

    // Start is called before the first frame update
    void Awake()
    {
        CollectMeshRenderers(m_modelsParent);

        m_maxHealth = m_castleData.m_maxHealth;
        m_curHealth = m_castleData.m_maxHealth;
        m_repairHealthAmount = m_castleData.m_repairHealthAmount;
        m_repairFrequency = m_castleData.m_repairFrequency;

        UpdateHealth += OnUpdateHealth;
        UpdateMaxHealth += OnUpdateMaxHealth;
        DestroyCastle += OnCastleDestroyed;

        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
        ResourceManager.UpdateStoneBank += OnBankUpdated;
        ResourceManager.UpdateWoodBank += OnBankUpdated;
        m_audioSource = GetComponent<AudioSource>();
    }

    void OnBankUpdated(int amount, int total)
    {
        //
    }

    void OnDestroy()
    {
        UpdateHealth -= OnUpdateHealth;
        UpdateMaxHealth -= OnUpdateMaxHealth;
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
                //Set whole block to occupied.
                GridCellOccupantUtil.SetOccupant(gameObject, true, 3, 3);

                //Carve out exits.
                foreach (GameObject obj in m_castleEntrancePoints)
                {
                    GridCellOccupantUtil.SetOccupant(obj, false, 1, 1);
                }

                //Carve out goal
                GridCellOccupantUtil.SetOccupant(gameObject, false, 1, 1);

                Cell cell = Util.GetCellFrom3DPos(transform.position);
                cell.UpdateGoalDisplay(true);
                break;
            case GameplayManager.GameplayState.FloodFillGrid:
                break;
            case GameplayManager.GameplayState.CreatePaths:
                break;
            case GameplayManager.GameplayState.Setup:
                break;
            case GameplayManager.GameplayState.SpawnEnemies:
                break;
            case GameplayManager.GameplayState.BossWave:
                break;
            case GameplayManager.GameplayState.Combat:
                break;
            case GameplayManager.GameplayState.Build:
                RepairCastle();
                break;
            case GameplayManager.GameplayState.CutScene:
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
        //
    }

    private bool m_isRepairing;

    void RepairCastle()
    {
        if (m_curHealth >= m_maxHealth) return;
        StartCoroutine(RepairCoroutine());
    }


    private IEnumerator RepairCoroutine()
    {
        int amountToRepair = Math.Min(m_maxHealth - m_curHealth, m_repairFrequency);
        m_isRepairing = true;
        while (m_healthRepaired < amountToRepair)
        {
            PlayScheduledClip(m_castleData.m_repairingClip);
            m_repairElapsedTime = 0;
            
            while (m_repairElapsedTime < GameplayManager.Instance.m_gameplayData.m_buildDuration / m_repairFrequency)
            {
                m_repairElapsedTime += Time.deltaTime;
                yield return null;
            }
            
            ++m_healthRepaired;
            UpdateHealth?.Invoke(m_repairHealthAmount);
            StopScheduledClip();
        }

        m_healthRepaired = 0;
        m_isRepairing = false;
    }

    public int GetCastleMaxHealth()
    {
        return m_maxHealth;
    }

    public int GetCastleCurHealth()
    {
        return m_curHealth;
    }

    public void CheatCastleHealth()
    {
        m_maxHealth = 999;
        UpdateHealth?.Invoke(m_maxHealth - m_curHealth);
        m_repairElapsedTime = 0;
    }

    public float RepairProgress()
    {
        return m_repairElapsedTime / (GameplayManager.Instance.m_gameplayData.m_buildDuration / m_repairFrequency);
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

    public void TakeBossDamage(int dmg)
    {
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());
        IngameUIController.Instance.SpawnMaxHealthAlert(1, transform.position);

        UpdateMaxHealth?.Invoke(-dmg);
    }

    void OnUpdateHealth(int i)
    {
        m_curHealth += i;

        if (i > 0)
        {
            RequestPlayAudio(m_castleData.m_healthGainedClip);
        }
        else
        {
            RequestPlayAudio(m_castleData.m_healthLostClips);
        }

        if (m_curHealth <= 0)
        {
            DestroyCastle?.Invoke();
        }
    }

    void OnUpdateMaxHealth(int i)
    {
        m_maxHealth += i;

        m_curHealth += i;

        if (i > 0)
        {
            RequestPlayAudio(m_castleData.m_healthGainedClip);
        }
        else
        {
            RequestPlayAudio(m_castleData.m_healthLostClips);
        }

        if (m_curHealth <= 0)
        {
            DestroyCastle?.Invoke();
        }
    }

    void OnCastleDestroyed()
    {
        GameplayManager.Instance.CastleControllerDestroyed();
    }

    private IEnumerator HitFlash()
    {
        //Set the color
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            foreach (Material material in m_allRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", Color.red);
            }
        }

        yield return new WaitForSeconds(.15f);

        //Return to original colors.
        for (int i = 0; i < m_allRenderers.Count; ++i)
        {
            foreach (Material material in m_allRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", m_allOrigColors[i]);
            }
        }
    }


    void CollectMeshRenderers(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        Renderer Renderer = parent.GetComponent<Renderer>();

        if (Renderer != null && !(Renderer is TrailRenderer) && !(Renderer is VFXRenderer))
        {
            if (m_allRenderers == null)
            {
                m_allRenderers = new List<Renderer>();
            }

            if (m_allOrigColors == null)
            {
                m_allOrigColors = new List<Color>();
            }

            m_allRenderers.Add(Renderer);
            m_allOrigColors.Add(Renderer.material.GetColor("_EmissionColor"));
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            //Recursivly add meshrenderers by calling this function again with the child as the transform.
            Transform child = parent.GetChild(i);
            CollectMeshRenderers(child);
        }
    }

    public void RequestPlayAudio(AudioClip clip)
    {
        m_audioSource.PlayOneShot(clip);
    }

    public void RequestPlayAudio(List<AudioClip> clips)
    {
        int i = Random.Range(0, clips.Count);
        m_audioSource.PlayOneShot(clips[i]);
    }

    public void PlayScheduledClip(AudioClip clip)
    {
        m_audioSource.clip = clip;
        m_audioSource.Play();
    }

    public void StopScheduledClip()
    {
        if (m_audioSource.isPlaying)
        {
            m_audioSource.Stop();
        }
    }

    public CastleTooltipData GetTooltipData()
    {
        CastleTooltipData data = new CastleTooltipData();
        data.m_castleName = m_castleData.m_castleName;
        data.m_castleDescription = m_castleData.m_castleDescription;
        data.m_maxHealth = m_maxHealth;
        data.m_curHealth = m_curHealth;
        data.m_repairHealthAmount = m_repairHealthAmount;
        data.m_repairFrequency = m_repairFrequency;
        return data;
    }
}

public class CastleTooltipData
{
    public string m_castleName;
    public string m_castleDescription;
    public int m_maxHealth;
    public int m_curHealth;
    public int m_repairHealthAmount;
    public int m_repairFrequency;
}