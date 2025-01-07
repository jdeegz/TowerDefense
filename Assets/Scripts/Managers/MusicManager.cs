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
    private AudioClip m_lastPlayedClip;
    private bool m_isPlayingA = true;
    private bool m_isBossWave;
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
                    StartCoroutine(PlayAndCrossfade(m_bossMusicTracks));
                }
                else
                {
                    StartCoroutine(PlayAndCrossfade(m_defaultMusicTracks));
                }
            }
        }
    }

    private bool m_testBossWave;
    public bool TestBossWave
    {
        get { return m_testBossWave; }
        set
        {
            if (value != m_testBossWave)
            {
                m_testBossWave = value;
                StopCoroutine(m_curCoroutine);
                if (m_testBossWave)
                {
                    StartCoroutine(PlayAndCrossfade(m_bossMusicTracks));
                }
                else
                {
                    StartCoroutine(PlayAndCrossfade(m_defaultMusicTracks));
                }
            }
        }
    }


    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayStateChanged;
        m_audioSourceA.loop = false;
        m_audioSourceB.loop = false;

        // Start playback
        m_curCoroutine = StartCoroutine(PlayAndCrossfade(m_defaultMusicTracks));
    }

    private IEnumerator PlayAndCrossfade(List<AudioClip> clips)
    {
        while (true)
        {
            // Determine current and next AudioSource
            AudioSource currentSource = m_isPlayingA ? m_audioSourceA : m_audioSourceB;
            AudioSource nextSource = m_isPlayingA ? m_audioSourceB : m_audioSourceA;

            // Pick the next clip randomly, ensuring it's not the same as the last played clip
            AudioClip nextClip = PickRandomClip(clips);
            nextSource.clip = nextClip;
            nextSource.volume = 0;
            nextSource.Play();

            //Debug.Log($"Now Playing: {nextClip.name}.");

            // Crossfade between sources
            float elapsed = 0f;
            while (elapsed < m_crossFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaledDeltaTime to respect pause
                currentSource.volume = Mathf.Lerp(1, 0, elapsed / m_crossFadeDuration);
                nextSource.volume = Mathf.Lerp(0, 1, elapsed / m_crossFadeDuration);
                yield return null;
            }

            currentSource.Stop();
            currentSource.volume = 0; // Ensure current source is fully faded out

            // Update state
            m_isPlayingA = !m_isPlayingA;
            m_lastPlayedClip = nextClip;

            // Wait for the next clip to finish before crossfading again
            yield return new WaitForSecondsRealtime(nextSource.clip.length - m_crossFadeDuration);
        }
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
    }
}