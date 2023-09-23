using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class UnitTargetDummy : MonoBehaviour
{
    public Transform m_targetPoint;
    private Coroutine m_hitFlashCoroutine;

    private AudioSource m_audioSource;
    private List<MeshRenderer> m_allMeshRenderers;
    private List<Color> m_allOrigColors;

    public event Action<int> UpdateHealth;
    public int m_maxHealth;
    private int m_curHealth;

    // Start is called before the first frame update
    void Start()
    {
        CollectMeshRenderers(transform);
        UpdateHealth += OnUpdateHealth;
        m_curHealth = m_maxHealth;

        //UIHealthMeter lifeMeter = Instantiate(IngameUIController.Instance.m_healthMeter, IngameUIController.Instance.transform);
        //lifeMeter.SetEnemy(this);
    }
    
    public void TakeDamage(int dmg)
    {
        if (m_hitFlashCoroutine != null)
        {
            StopCoroutine(m_hitFlashCoroutine);
        }

        m_hitFlashCoroutine = StartCoroutine(HitFlash());
        UpdateHealth?.Invoke(-dmg);
    }

    void OnUpdateHealth(int i)
    {
        m_curHealth += i;

        Debug.Log($"{gameObject.name}'s health Remaining: {m_curHealth}");
        if (m_curHealth <= 0)
        {
            //DestroyEnemy?.Invoke();
        }
    }

    private IEnumerator HitFlash()
    {
        //Set the color
        for (int i = 0; i < m_allMeshRenderers.Count; ++i)
        {
            m_allMeshRenderers[i].material.SetColor("_EmissionColor", Color.red);
        }

        yield return new WaitForSeconds(.15f);

        //Return to original colors.
        for (int i = 0; i < m_allMeshRenderers.Count; ++i)
        {
            m_allMeshRenderers[i].material.SetColor("_EmissionColor", m_allOrigColors[i]);
        }
    }

    private void CollectMeshRenderers(Transform parent)
    {
        //Get Parent Mesh Renderer if there is one.
        MeshRenderer meshRenderer = parent.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (m_allMeshRenderers == null)
            {
                m_allMeshRenderers = new List<MeshRenderer>();
            }

            if (m_allOrigColors == null)
            {
                m_allOrigColors = new List<Color>();
            }

            m_allMeshRenderers.Add(meshRenderer);
            m_allOrigColors.Add(meshRenderer.material.GetColor("_EmissionColor"));
        }

        for (int i = 0; i < parent.childCount; ++i)
        {
            //Recursivly add meshrenderers by calling this function again with the child as the transform.
            Transform child = parent.GetChild(i);
            CollectMeshRenderers(child);
        }
    }
}