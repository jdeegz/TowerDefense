using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameplayAudioData", menuName = "ScriptableObjects/GameplayAudioData")]
public class GameplayAudioData : ScriptableObject
{
    //Wave Begun Spawning
    public AudioClip m_waveStartClip;

    //All enemies defeated (Wave End)
    public List<AudioClip> m_audioWaveEndClips;
    
    //Cannot place tower
    public AudioClip m_cannotPlaceClip;
    
    //End of game
    public AudioClip m_victoryClip;
    public AudioClip m_defeatClip;
    public AudioClip m_endlessModeStartedClip;
    
    //Playback controls
    public AudioClip m_play;
    public AudioClip m_pause;
    public AudioClip m_ffwOn;
    public AudioClip m_ffwOff;
}
