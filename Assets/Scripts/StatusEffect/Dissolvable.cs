using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Dissolvable : MonoBehaviour
{
    private List<Material> m_materials;
    private float m_dissolveDuration = 0.66f;

    void Awake()
    {
        CollectMaterials(transform);
    }

    void OnEnable()
    {
        if (m_materials == null) return; // No list created yet. Must be a new unit.
        
        if (m_materials.Count == 0) return; // This unit has no materials.

        foreach (Material material in m_materials)
        {
            material.SetFloat("_AlphaClipThreshold", 0);
        }
    }

    protected void ResetDissolve() // Used for Swarm Members because they are not sent to and removed from the pool. (Never trigger On Enable)
    {
        if (m_materials == null) return; // No list created yet. Must be a new unit.
        
        if (m_materials.Count == 0) return;

        foreach (Material material in m_materials)
        {
            material.SetFloat("_AlphaClipThreshold", 0);
        }
    }

    protected void StartDissolve(Action onDissolveComplete)
    {
        StartCoroutine(DoDissolve(onDissolveComplete));
    }

    protected IEnumerator DoDissolve(Action onDissolveComplete)
    {
        if (m_materials.Count > 0)
        {
            float counter = 0;
            float curValue = 0;

            while (curValue < 1)
            {
                counter += Time.deltaTime;
                curValue = Mathf.Lerp(0f, 1f, counter / m_dissolveDuration);

                for (int i = 0; i < m_materials.Count; ++i)
                {
                    m_materials[i].SetFloat("_AlphaClipThreshold", curValue);
                }

                yield return new WaitForEndOfFrame();
            }
        }

        onDissolveComplete?.Invoke();
    }

    public void CollectMaterials(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        Renderer Renderer = parent.GetComponent<Renderer>();
        if (Renderer != null && !(Renderer is TrailRenderer) && !(Renderer is VFXRenderer))
        {
            if (m_materials == null)
            {
                m_materials = new List<Material>();
            }

            foreach (Material material in Renderer.materials)
            {
                m_materials.Add(material);
            }
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            //Recursivly add meshrenderers by calling this function again with the child as the transform.
            Transform child = parent.GetChild(i);
            CollectMaterials(child);
        }
    }
}