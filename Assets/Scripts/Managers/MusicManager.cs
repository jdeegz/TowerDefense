using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    private AudioSource m_audioSource;
    public AudioClip m_defaultMusic;
    public AudioClip m_bossMusic;

    public float m_crossFadeDuration = 1f;

    void Awake()
    {
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.clip = m_defaultMusic;
        m_audioSource.Play();
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        
    }

    private void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.BossWave)
        {
            StartCoroutine(CrossfadeRoutine(m_bossMusic));
        }
        else if (m_audioSource.clip != m_defaultMusic)
        {
            StartCoroutine(CrossfadeRoutine(m_defaultMusic));
        }
    }
    
    IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        float currentTime = 0;
        float startVolume = m_audioSource.volume;

        while (currentTime < m_crossFadeDuration)
        {
            currentTime += Time.deltaTime;
            m_audioSource.volume = Mathf.Lerp(startVolume, 0, currentTime / m_crossFadeDuration);
            yield return null;
        }

        m_audioSource.clip = newClip;
        m_audioSource.Play();

        currentTime = 0;

        while (currentTime < m_crossFadeDuration)
        {
            currentTime += Time.deltaTime;
            m_audioSource.volume = Mathf.Lerp(0, startVolume, currentTime / m_crossFadeDuration);
            yield return null;
        }
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
    }
}
