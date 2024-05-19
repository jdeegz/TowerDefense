using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VFXCleanUp : MonoBehaviour
{
    public bool m_hideOnAwake;
    public bool m_destroyAfterPlay;
    private float m_longestDuration = 0f;
    private float m_elapsedTime;
    private ParticleSystem m_tempChild;
    
    public GameObject m_rootObj;
    
    void Awake()
    {
        var m_particleSystems = GetComponentsInChildren<ParticleSystem>();
        {
            foreach (ParticleSystem child in m_particleSystems)
            {
                float t = child.main.duration;
                if (t > m_longestDuration)
                {
                    m_longestDuration = t;
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
            if (m_destroyAfterPlay)
            {
                ObjectPoolManager.ReturnObjectToPool(gameObject);
            }
        }
    }
}