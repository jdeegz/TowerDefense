using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class CastleController : MonoBehaviour
{
    public CastleData m_castleData;

    [SerializeField] public List<GameObject> m_castleEntrancePoints;
    [SerializeField] private List<Renderer> m_spireRenderers;
    [SerializeField] private Renderer m_spireTopRenderer;
    [SerializeField] private GameObject m_castleModelRoot;
    public VisualEffect m_spireBeamVFX;
    public VisualEffect m_spireEndlessVFX;
    private List<Color> m_allOrigColors;
    private Coroutine m_hitFlashCoroutine;
    private AudioSource m_audioSource;

    private int m_maxHealth;
    private int m_curHealth;
    private int m_repairHealthAmount;
    private int m_repairFrequency;
    private float m_repairElapsedTime;
    private int m_healthRepaired = 0;
    private bool m_isInvulnerable;
    private List<Material> m_spireMaterials;
    private Material m_spireTopMaterial;
    private float m_spireTopStartDissolve;
    
    public event Action<int> UpdateHealth;
    public event Action<int> UpdateMaxHealth;
    public event Action DestroyCastle;

    // Start is called before the first frame update
    void Awake()
    {
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

        m_allOrigColors = new List<Color>();
        m_spireMaterials = new List<Material>();
        foreach (Renderer renderer in m_spireRenderers)
        {
            m_spireMaterials.Add(renderer.material);
            m_allOrigColors.Add(renderer.material.GetColor("_EmissionColor"));
        }

        m_spireTopMaterial = m_spireTopRenderer.material;
        m_spireTopStartDissolve = m_spireTopMaterial.GetFloat("_DissolveValue");
    }

    void OnBankUpdated(int amount, int total)
    {
        //
    }

    public void SetCastleInvulnerable(bool value)
    {
        if (m_isInvulnerable == value) return;

        m_isInvulnerable = value;
    }

    public int GetCurrentMaxHealth()
    {
        return m_maxHealth;
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
    public event Action<bool> OnIsRepairingUpdated;

    private bool IsRepairing
    {
        get { return m_isRepairing; }
        set
        {
            if (value != m_isRepairing)
            {
                m_isRepairing = value;
                OnIsRepairingUpdated?.Invoke(m_isRepairing);
            }
        }
    }

    private Coroutine m_curRepairCoroutine;

    public void RepairCastle()
    {
        if (m_curHealth >= m_maxHealth) return;
        m_curRepairCoroutine = StartCoroutine(RepairCoroutine());
    }

    private bool m_spireBeamVFXIsPlaying;
    private float m_curBeamWidth = 1;

    public void HandleSpireBeamVFX(bool value)
    {
        if (value == m_spireBeamVFXIsPlaying) return;

        m_spireBeamVFXIsPlaying = value;

        if (m_spireBeamVFXIsPlaying)
        {
            RequestPlayAudio(m_castleData.m_spireBeamExplosionClip);
            m_spireBeamVFX.Play();
        }
        else // Triggered when going Endless.
        {
            m_spireBeamVFX.Reinit();
            m_spireBeamVFX.Stop();
            /*DOTween.To(() => m_curBeamWidth, x => m_curBeamWidth = x, 0f, .33f)
                .OnUpdate(() => m_spireBeamVFX.SetFloat("BeamWidth", m_curBeamWidth))
                .SetUpdate(true);*/
        }
    }

    private float m_curSpireDestroyedDissolve;
    private float m_spireDestroyDuration = 5f;
    public void HandleSpireDestroyedVFX()
    {
        gameObject.GetComponent<Collider>().enabled = false;
        
        ObjectPoolManager.SpawnObject(m_castleData.m_destoyedTearVFX, transform.position, quaternion.identity, null, ObjectPoolManager.PoolType.ParticleSystem);
        
        //Dissolve Spire
        DOTween.To(() => 0f, x => SetDissolveValue(x), 1f, (m_spireDestroyDuration - 0.5f) * 0.8f)
            .SetDelay(0.5f)
            .SetUpdate(true)
            .SetEase(Ease.InSine);
        
        RequestPlayAudio(m_castleData.m_destroyedClip);
        
        ObjectPoolManager.SpawnObject(m_castleData.m_destoyedVFX, transform.position, quaternion.identity, m_castleModelRoot.transform, ObjectPoolManager.PoolType.ParticleSystem);
        
        //Lift Castle object
        m_castleModelRoot.transform.DOLocalMove(new Vector3(0, 2, 0), m_spireDestroyDuration).SetUpdate(true).SetEase(Ease.InSine);
        m_castleModelRoot.transform.DOLocalRotate(new Vector3(0, Random.Range(-45, 45), Random.Range(-50, 50)), m_spireDestroyDuration).SetUpdate(true).SetEase(Ease.InSine);
    }

    private float m_curSpireTopDissolveValue;
    void SetDissolveValue(float value)
    {
        foreach (Material material in m_spireMaterials)
        {
            material.SetFloat("_AlphaClipThreshold", value);
        }

        m_curSpireTopDissolveValue = Mathf.Lerp(m_spireTopStartDissolve, 1, value);
        m_spireTopMaterial.SetFloat("_DissolveValue", m_curSpireTopDissolveValue);
    }

    private bool m_spireEndlessVFXPlaying;

    public void HandleSpireEndlessVFX(bool value)
    {
        if (value == m_spireEndlessVFXPlaying) return;

        m_spireEndlessVFXPlaying = value;

        if (m_spireEndlessVFXPlaying)
        {
            m_spireEndlessVFX.Play();
        }
        else
        {
            m_spireEndlessVFX.Stop();
        }
    }


    private IEnumerator RepairCoroutine()
    {
        int amountToRepair = Math.Min(m_maxHealth - m_curHealth, m_repairFrequency);
        IsRepairing = true;
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
        IsRepairing = false;
        m_curRepairCoroutine = null;
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
        ++GameplayManager.Instance.DamageTakenThisWave;
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
        ++GameplayManager.Instance.DamageTakenThisWave;
    }

    void OnUpdateHealth(int i)
    {
        if (m_isInvulnerable) return; 
        
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
            //Exit out a repair if we've hit 0 (Survival Mode)
            if (m_curRepairCoroutine != null)
            {
                StopCoroutine(m_curRepairCoroutine);
                IsRepairing = false;
            }

            DestroyCastle?.Invoke();
        }
    }

    void OnUpdateMaxHealth(int i)
    {
        if (m_isInvulnerable) return;
        
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
        for (int i = 0; i < m_spireRenderers.Count; ++i)
        {
            foreach (Material material in m_spireRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", Color.red);
            }
        }

        yield return new WaitForSeconds(.15f);

        //Return to original colors.
        for (int i = 0; i < m_spireRenderers.Count; ++i)
        {
            foreach (Material material in m_spireRenderers[i].materials)
            {
                material.SetColor("_EmissionColor", m_allOrigColors[i]);
            }
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