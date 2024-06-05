using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class VFXCleanUp : MonoBehaviour
{
    public bool m_hideOnAwake;
    public bool m_returnToPool;
    private float m_longestDuration = 0f;
    private float m_elapsedTime;
    private ParticleSystem m_tempChild;

    public GameObject m_rootObj;

    void Start()
    {
        //Get VFX times
        var m_particleSystems = GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem child in m_particleSystems)
        {
            if (child.main.loop)
            {
                //Skip looping vfx
            }
            else
            {
                float mainDuration = child.main.duration;
                float maxParticleLifetime = child.main.startLifetime.constantMax;

                float totalDuration = mainDuration + maxParticleLifetime;
                if (totalDuration > m_longestDuration)
                {
                    m_longestDuration = totalDuration;
                }
            }
        }

        //Get Material Over Time
        var m_animatedMaterials = GetComponentsInChildren<MaterialOverTime>();
        foreach (MaterialOverTime child in m_animatedMaterials)
        {
            {
                float totalDuration = child.GetDissolveDuration();
                if (totalDuration > m_longestDuration)
                {
                    m_longestDuration = totalDuration;
                }
            }
        }
        
        m_rootObj.SetActive(!m_hideOnAwake);
    }

    void Update()
    {
        m_elapsedTime += Time.deltaTime;
        if (m_elapsedTime >= m_longestDuration && m_rootObj.activeSelf)
        {
            m_rootObj.SetActive(false);
            m_elapsedTime = 0f;
            if (m_returnToPool)
            {
                ObjectPoolManager.ReturnObjectToPool(gameObject);
            }
        }
    }
}