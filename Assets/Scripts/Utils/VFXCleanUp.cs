using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXCleanUp : MonoBehaviour
{
    private float m_longestDuration = 0f;
    private ParticleSystem m_tempChild;
    // Start is called before the first frame update
    void Start()
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

        Destroy(gameObject, m_longestDuration);
    }
}