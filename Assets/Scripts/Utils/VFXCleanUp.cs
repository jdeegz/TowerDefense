using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class VFXCleanUp : MonoBehaviour
{
    public bool m_hideOnAwake;
    public bool m_returnToPool;
    private float m_longestDuration = 0f;
    private float m_elapsedTime;
    private ParticleSystem m_tempChild;
    private List<ParticleSystem> m_particleSystems;
    private List<VisualEffect> m_storedVFXSystems;
    public List<VisualEffect> m_vfxSystems;
    public List<bool> m_vfxSystemHasPlayed;
    public bool m_vfxSystemsComplete;
    public bool m_hasVFXSystemsInChildren;


    public GameObject m_rootObj;

    void Start()
    {
        //
        //Get VFX times
        //Manipulated list.
        m_vfxSystems = new List<VisualEffect>(GetComponentsInChildren<VisualEffect>());
        
        //Stashed list so I dont do Get Component each time we activate.
        m_storedVFXSystems = new List<VisualEffect>(m_vfxSystems);
        
        //Check if we have any systems, if we do, make a list of bools.
        if (m_vfxSystems.Count == 0)
        {
            m_vfxSystemsComplete = true;
        }
        else
        {
            m_hasVFXSystemsInChildren = true;
            
            m_vfxSystemHasPlayed = new List<bool>();
            
            for (int i = 0; i < m_vfxSystems.Count; i++)
            {
                m_vfxSystemHasPlayed.Add(false);
            }
        }
        
        //
        //Get particle system times
        m_particleSystems = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>());
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
        
        //
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
        //If we have vfx systems in here, we need to check to see if each one has played and if it's completed, if so set a bool to true.
        if (!m_vfxSystemsComplete)
        {
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

        m_elapsedTime += Time.deltaTime;
        if (m_elapsedTime >= m_longestDuration && m_rootObj.activeSelf && m_vfxSystemsComplete)
        {
            if (m_returnToPool)
            {
                ObjectPoolManager.ReturnObjectToPool(gameObject, ObjectPoolManager.PoolType.ParticleSystem);
            }
            else
            {
                m_rootObj.SetActive(false);
            }

            m_elapsedTime = 0f;
        }
    }

    void OnEnable()
    {
        //On enable, if there are Store VFX systems, we want to reset the VFX system info.
        if (m_hasVFXSystemsInChildren)
        {
            m_vfxSystemsComplete = false;
            
            m_vfxSystems = new List<VisualEffect>(m_storedVFXSystems);
            
            m_vfxSystemHasPlayed = new List<bool>();
            
            for (int i = 0; i < m_vfxSystems.Count; i++)
            {
                m_vfxSystemHasPlayed.Add(false);
            }
        }
    }
    
}