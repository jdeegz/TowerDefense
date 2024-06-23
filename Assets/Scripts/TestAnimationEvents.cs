using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAnimationEvents : MonoBehaviour
{
    public GameObject m_fireVFX;
    public float m_longestFireVFXDuration;
    public GameObject m_muzzleObj;

    private float m_elapsedTimeFireVFX;
    
    // Start is called before the first frame update
    void Awake()
    {
        //Get the length of the particle systems
        var m_particleSystems = GetComponentsInChildren<ParticleSystem>();
        {
            foreach (ParticleSystem child in m_particleSystems)
            {
                float t = child.main.duration;
                if (t > m_longestFireVFXDuration)
                {
                    m_longestFireVFXDuration = t;
                }
            }
        }
        m_fireVFX.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        m_elapsedTimeFireVFX += Time.deltaTime;
        if (m_elapsedTimeFireVFX >= m_longestFireVFXDuration && m_fireVFX.activeSelf)
        {
            m_fireVFX.SetActive(false);
        }
    }

    void FireVFX()
    {
        m_fireVFX.SetActive(true);
        m_elapsedTimeFireVFX = 0;
        Debug.Log($"FIRE!");
    }
}
