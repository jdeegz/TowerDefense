using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayAudioData", menuName = "ScriptableObjects/GameplayAudioData")]
public class GameplayAudioData : ScriptableObject
{
    [Header("Wave Begun Spawning")]
    //Wave Begun Spawning
    public AudioClip m_waveStartClip;

    [Header("All enemies defeated")]
    //All enemies defeated (Wave End)
    public List<AudioClip> m_audioWaveEndClips;
    
    [Header("Cannot place tower")]
    //Cannot place tower
    public AudioClip m_cannotPlaceClip;
    
    [Header("End of game")]
    //End of game
    public AudioClip m_victoryClip;
    public AudioClip m_defeatClip;
    public AudioClip m_endlessModeStartedClip;
    
    [Header("Playback controls")]
    //Playback controls
    public AudioClip m_play;
    public AudioClip m_pause;
    public AudioClip m_ffwOn;
    public AudioClip m_ffwOff;
}
