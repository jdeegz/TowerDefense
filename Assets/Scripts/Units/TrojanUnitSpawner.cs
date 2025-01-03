using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrojanUnitSpawner : MonoBehaviour
{
    //The Trojan spawner is created at the X,Y position of the Enemy Trojan when it dies.
    //It needs to be added to the gameplay manager's active spawner list.
    //It needs to spawn creeps from each creep wave based on their timing data.
    [SerializeField] private TearData m_data;
    [SerializeField] private List<CreepWave> m_creepWaves;
    [SerializeField] private AudioSource m_audioSource;

    private List<CreepSpawner> m_activeCreepSpawners;
    private List<Creep> m_activeCreepWave;
    private bool m_isSpawnerActive;
    
    private void OnEnable()
    {
        m_isSpawnerActive = false;
        
        //Add this spawner to the list of active spawners to keep the current wave active while it spawns.
        GameplayManager.Instance.ActivateSpawner();
        RequestPlayAudio(m_data.m_audioSpawnerCreated, m_audioSource);
        StartCoroutine(CreateSpawner());
    }

    private IEnumerator CreateSpawner()
    {
        yield return new WaitForSeconds(0.5f);
        StartSpawning();
    }
    
    private void StartSpawning()
    {
        RequestPlayAudioLoop(m_data.m_audioSpawnerActiveLoops, m_audioSource);
        
        //Calculate which CreepWave to spawn based on mission's wave number.
        int creepWaveIndex = GameplayManager.Instance.m_wave % m_creepWaves.Count;
        
        //Assure each creep has a point to spawn to.
        m_activeCreepSpawners = new List<CreepSpawner>();
        for (int i = 0; i < m_creepWaves[creepWaveIndex].m_creeps.Count; ++i)
        {
            //Build list of Active Creep Spawners.
            CreepSpawner creepSpawner = new CreepSpawner(m_creepWaves[creepWaveIndex].m_creeps[i], transform);
            m_activeCreepSpawners.Add(creepSpawner);
        }

        m_isSpawnerActive = true;
    }
    
    private void Update()
    {
        if (m_isSpawnerActive)
        {
            for (int i = 0; i < m_activeCreepSpawners.Count; ++i)
            {
                if (m_activeCreepSpawners[i].IsCreepSpawning())
                {
                    m_activeCreepSpawners[i].UpdateCreep();
                }
                else
                {
                    //If the creep is NOT spawning, remove it from the active creep spawner list.
                    m_activeCreepSpawners.RemoveAt(i);
                    --i;
                }
            }

            //If we have NO active creep spawners, disable this spawner.
            if (m_activeCreepSpawners.Count == 0)
            {
                RequestStopAudioLoop(m_audioSource);
                m_isSpawnerActive = false;
                GameplayManager.Instance.DisableSpawner();
                RemoveTrojanUnitSpawner();
            }
        }
    }
    
    public void RequestPlayAudioLoop(List<AudioClip> clips, AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;
        
        int i = Random.Range(0, clips.Count);
        audioSource.volume = 0;
        audioSource.loop = true;
        audioSource.clip = clips[i];
        audioSource.Play();
        
        if(m_curCoroutine != null) StopCoroutine(m_curCoroutine);
        m_curCoroutine = StartCoroutine(FadeInAudio(1f, audioSource));

    }

    private Coroutine m_curCoroutine;
    private IEnumerator FadeInAudio(float duration, AudioSource audioSource)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Clamp01(startVolume + (elapsedTime / duration));
            yield return null;
        }

        audioSource.volume = 1f;
    }
    
    private IEnumerator FadeOutAudio(float duration, AudioSource audioSource)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Clamp01(startVolume - (elapsedTime / duration));
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }
    
    public void RequestStopAudioLoop(AudioSource audioSource = null)
    {
        if (audioSource == null) audioSource = m_audioSource;
        if(m_curCoroutine != null) StopCoroutine(m_curCoroutine);
        m_curCoroutine = StartCoroutine(FadeOutAudio(0.5f, audioSource));
    }
    
    public void RequestPlayAudio(AudioClip clip, AudioSource audioSource = null)
    {
        if (clip == null) return;
        
        if (audioSource == null) audioSource = m_audioSource;
        audioSource.PlayOneShot(clip);
    }

    private void RemoveTrojanUnitSpawner()
    {
        //Trigger animation to remove spawners
        
        //Remove Obj from scene
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.Enemy);
    }
    
    public TearTooltipData GetTooltipData()
    {
        TearTooltipData data = new TearTooltipData();
        data.m_tearName = m_data.m_tearName;
        data.m_tearDescription = m_data.m_tearDescription;
        data.m_tearDetails = m_data.m_tearDetails;
        return data;
    }
}
