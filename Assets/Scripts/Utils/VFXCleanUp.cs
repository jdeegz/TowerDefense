using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class VFXCleanUp : PooledObject
{
    public bool m_returnToPool;
    private float m_longestDuration = 0f;
    private float m_elapsedTime;
    private ParticleSystem m_tempChild; // TO REMOVE
    private List<ParticleSystem> m_particleSystems; // TO REMOVE
    private List<VisualEffect> m_storedVFXSystems;
    private List<VisualEffect> m_vfxSystems;
    public List<bool> m_vfxSystemHasPlayed;
    public bool m_vfxSystemsComplete;
    private bool m_hasReturnedToPool = false;

    void Start()
    {
        //
        //Get particle system times
        m_particleSystems = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>());
        foreach (ParticleSystem child in m_particleSystems)
        {
            Debug.Log($"{gameObject.name} contains Old Particle Systems.");
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

        //
        //Get Material Over Time
        var m_animatedMaterials = GetComponentsInChildren<MaterialOverTime>();
        foreach (MaterialOverTime child in m_animatedMaterials)
        {
            {
                Debug.Log($"{gameObject.name} contains Old Material Over Time Systems.");
                float totalDuration = child.GetDissolveDuration();
                if (totalDuration > m_longestDuration)
                {
                    m_longestDuration = totalDuration;
                }
            }
        }
    }

    void Update()
    {
        if (m_hasReturnedToPool) return;

        //If we have vfx systems in here, we need to check to see if each one has played and if it's completed, if so set a bool to true.
        if (!m_vfxSystemsComplete)
        {
            m_elapsedTime += Time.deltaTime;

            for (int i = 0; i < m_vfxSystems.Count; i++)
            {
                //Check if this particle system has alive particles and has played (accounts for a delay)
                if (m_vfxSystems[i].aliveParticleCount == 0 && m_vfxSystemHasPlayed[i])
                {
                    //This system is complete. Remove it from the list.
                    m_vfxSystems.RemoveAt(i);
                    m_vfxSystemHasPlayed.RemoveAt(i);
                    --i;
                    continue;
                }

                //Check if the system has an alive particle, indicating it has played.
                if (m_vfxSystems[i].aliveParticleCount > 0)
                {
                    m_vfxSystemHasPlayed[i] = true;
                }
            }

            if (m_vfxSystems.Count == 0) m_vfxSystemsComplete = true;
        }


        if (m_elapsedTime >= m_longestDuration && m_vfxSystemsComplete && !m_hasReturnedToPool)
        {
            if (m_returnToPool)
            {
                ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.ParticleSystem);
                m_hasReturnedToPool = true;
            }
            else
            {
                //
            }

            m_elapsedTime = 0f;
        }
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        
        // On enable, if there are Store VFX systems, we want to reset the VFX system info.
        m_hasReturnedToPool = false;
        
        // Create list that i manipulate in Update.
        m_vfxSystems = new List<VisualEffect>(m_vfx);

        // Check if we have any systems, if we do, make a list of bools.
        if (m_vfxSystems.Count == 0)
        {
            m_vfxSystemsComplete = true;
        }
        else
        {
            m_vfxSystemsComplete = false;

            if (m_vfxSystemHasPlayed == null)
            {
                m_vfxSystemHasPlayed = new List<bool>();
            }
            else
            {
                m_vfxSystemHasPlayed.Clear();
            }

            for (int i = 0; i < m_vfxSystems.Count; i++)
            {
                m_vfxSystemHasPlayed.Add(false);
            }
        }
    }
}