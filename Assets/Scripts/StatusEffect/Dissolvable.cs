using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Dissolvable : MonoBehaviour
{
    public VisualEffect m_deathVFX;
    private List<Material> m_materials;
    private float m_dissolveDuration = 0.3f;

    void Start()
    {
        CollectMaterials(transform);
    }

    void OnEnable()
    {
        //Reset the properties when the object is pulled from the pool.
        if (m_materials != null)
        {
            foreach (Material material in m_materials)
            {
                material.SetFloat("_AlphaClipThreshold", 0);
            }
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
            
            while (m_materials[0].GetFloat("_AlphaClipThreshold") < 1)
            {
                counter += Time.deltaTime;
                float curValue = Mathf.Lerp(0f, 1f, counter / m_dissolveDuration);
                
                for (int i = 0; i < m_materials.Count; ++i)
                {
                    m_materials[i].SetFloat("_AlphaClipThreshold", curValue);
                }

                yield return new WaitForEndOfFrame();
            }

            onDissolveComplete?.Invoke();
            Debug.Log($"dissolve complete.");
        }
    }
    
    public void CollectMaterials(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        Renderer Renderer = parent.GetComponent<Renderer>();
        if (Renderer != null)
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
