using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.VFX;

public class ObeliskSoul : MonoBehaviour
{
    public float m_moveDuration = 2f;
    private Vector3 m_endPos;
    private Obelisk m_obelisk;
    private Tween m_tweenToObelisk;
    private int m_soulValue;

    [SerializeField] private List<AudioClip> m_birthAudioClips;
    [SerializeField] private AudioSource m_audioSource;
    
    public void SetupSoul(Vector3 endPos, Obelisk obelisk, int soulValue)
    {
        m_endPos = endPos;
        m_obelisk = obelisk;
        m_soulValue = soulValue;
        HandleMovement();
        RequestPlayAudio(m_birthAudioClips);
    }

    void HandleMovement()
    {
        float moveDuration = Random.Range(m_moveDuration, m_moveDuration * 2.5f);
        float jumpPower = Random.Range(2, 4);
        m_tweenToObelisk = gameObject.transform.DOJump(m_endPos, jumpPower, 1, moveDuration).OnComplete(RequestObeliskCharge).SetEase(Ease.OutQuint);
        m_tweenToObelisk.Play();
    }

    void RequestObeliskCharge()
    {
        m_obelisk.IncreaseObeliskCharge(m_soulValue);
        ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.ParticleSystem); //1.2f is the max life duration of the vfx particle.
    }

    public void RequestPlayAudio(List<AudioClip> clips)
    {
        int i = Random.Range(0, clips.Count);
        m_audioSource.PlayOneShot(clips[i]);
    }
}
