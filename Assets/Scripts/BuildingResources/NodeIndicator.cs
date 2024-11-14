using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class NodeIndicator : MonoBehaviour
{
    private List<Material> m_materials;

    void Awake()
    {
        CollectMaterials(transform);
    }

    public void TintMaterials(Color color)
    {
        if (m_materials == null) return;
        
        foreach (Material material in m_materials)
        {
            material.SetColor("_BaseColor", color);
            //material.SetColor("_EmissionColor", color);
        }
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
