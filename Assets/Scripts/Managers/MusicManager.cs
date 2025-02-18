using System.Collections;
using System.Collections.Generic;
using GameUtil;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSourceA;
    [SerializeField] private AudioSource m_audioSourceB;
    [SerializeField] private List<AudioClip> m_defaultMusicTracks;
    [SerializeField] private List<AudioClip> m_bossMusicTracks;
    public float m_crossFadeDuration = 1f;

    private Coroutine m_curCoroutine;
    private Coroutine m_curFadeCoroutine;
    private AudioClip m_lastPlayedClip;
    private bool m_isPlayingA = true;
    private bool m_isBossWave;
    private float m_volumeMultiplier = 1; // Used to control the volume separate from the crossfade.
    private float m_fadeOutDuration = 1f;
    private float m_fadeInDuration = 3f;

    private bool IsBossWave
    {
        get { return m_isBossWave; }
        set
        {
            if (value != m_isBossWave)
            {
                m_isBossWave = value;
                StopCoroutine(m_curCoroutine);
                if (m_isBossWave)
                {
                    m_curCoroutine = StartCoroutine(PlayAndCrossfade(m_bossMusicTracks));
                }
                else
                {
                    m_curCoroutine = StartCoroutine(PlayAndCrossfade(m_defaultMusicTracks));
                }
            }
        }
    }

    private bool m_testBossWave;


    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        GameplayManager.OnGossamerHealed += MissionEnded;
        GameplayManager.OnSpireDestroyed += MissionEnded;
        m_audioSourceA.loop = false;
        m_audioSourceB.loop = false;

        // Start playback
        m_curCoroutine = StartCoroutine(PlayAndCrossfade(m_defaultMusicTracks));
    }

    private AudioSource m_currentSource;
    private float m_currentVolume;

    private float CurrentVolume
    {
        get { return m_currentVolume; }
        set
        {
            if (value != m_currentVolume)
            {
                m_currentVolume = value;
                SetAudioSourceVolume(m_currentSource, m_currentVolume);
            }
        }
    }
    
    private AudioSource m_nextSource;
    private float m_nextVolume;

    private float NextVolume
    {
        get { return m_nextVolume; }
        set
        {
            if (value != m_nextVolume)
            {
                m_nextVolume = value;
                SetAudioSourceVolume(m_nextSource, m_nextVolume);
            }
        }
    }
    
    
    private IEnumerator PlayAndCrossfade(List<AudioClip> clips)
    {
        while (true)
        {
            // Determine current and next AudioSource
            m_currentSource = m_isPlayingA ? m_audioSourceA : m_audioSourceB;
            m_nextSource = m_isPlayingA ? m_audioSourceB : m_audioSourceA;

            // Pick the next clip randomly, ensuring it's not the same as the last played clip
            AudioClip nextClip = PickRandomClip(clips);
            m_nextSource.clip = nextClip;
            m_nextSource.volume = 0;
            m_nextSource.Play();

            //Debug.Log($"Now Playing: {nextClip.name}.");

            // Crossfade between sources
            float elapsed = 0f;
            while (elapsed < m_crossFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaledDeltaTime to respect pause
                
                CurrentVolume = Mathf.Lerp(1, 0, elapsed / m_crossFadeDuration);
                
                NextVolume = Mathf.Lerp(0, 1, elapsed / m_crossFadeDuration);
                
                yield return null;
            }

            m_currentSource.Stop();
            m_currentSource.volume = 0; // Ensure current source is fully faded out

            // Update state
            m_isPlayingA = !m_isPlayingA;
            m_lastPlayedClip = nextClip;

            // Wait for the next clip to finish before crossfading again
            yield return new WaitForSecondsRealtime(m_nextSource.clip.length - m_crossFadeDuration);
        }
    }
    
    private IEnumerator FadeMusic(float targetMultiplier, float duration)
    {
        float startMultiplier = m_volumeMultiplier;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_volumeMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, elapsed / duration);
        
            // Apply updated volume to both audio sources
            SetAudioSourceVolume(m_currentSource, CurrentVolume);
            SetAudioSourceVolume(m_nextSource, NextVolume);
        
            yield return null;
        }

        m_volumeMultiplier = targetMultiplier;
    }

    private void SetAudioSourceVolume(AudioSource audioSource, float volume)
    {
        audioSource.volume = volume * m_volumeMultiplier;
    }
    
    private void MissionEnded(bool value)
    {
        //If True, fade out music, then stop. Else Start music.
        if (value)
        {
            // Fade Music Out.
            FadeOutMusic(m_fadeOutDuration);
        }
        else
        {
            // Fade Music In.
            FadeInMusic(m_fadeInDuration);
        }
    }
    
    private void FadeOutMusic(float duration)
    {
        if (m_curFadeCoroutine != null)
            StopCoroutine(m_curFadeCoroutine);
    
        m_curFadeCoroutine = StartCoroutine(FadeMusic(0f, duration));
    }
    
    private void FadeInMusic(float duration)
    {
        if (m_curFadeCoroutine != null)
            StopCoroutine(m_curFadeCoroutine);
    
        m_curFadeCoroutine = StartCoroutine(FadeMusic(1f, duration));
    }


    private AudioClip PickRandomClip(List<AudioClip> clips)
    {
        if (clips.Count == 1) return clips[0]; // Only one clip available

        AudioClip randomClip;
        do
        {
            randomClip = clips[Random.Range(0, clips.Count)];
        } while (randomClip == m_lastPlayedClip); // Avoid repeating the last played clip

        return randomClip;
    }

    private void GameplayStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.BossWave)
        {
            IsBossWave = true;
        }
        else
        {
            IsBossWave = false;
        }
    }
    
    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayStateChanged;
        GameplayManager.OnGossamerHealed -= MissionEnded;
        GameplayManager.OnSpireDestroyed -= MissionEnded;
    }
}