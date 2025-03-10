using System;
using UnityEngine;

public class HealthGraphing : MonoBehaviour
{
    [SerializeField] private MissionGameplayData m_gameplayData;
    [SerializeField] private int m_wavesToGraph = 100;
    [SerializeField] private Gradient m_colorGradient;

    private GameObject m_newParent;
    
    
    
    private void Start()
    {
        m_newParent = new GameObject("New");
        
        for (int wave = 1; wave < m_wavesToGraph; ++wave)
        {
            float y = m_gameplayData.CalculateHealth(10, wave);
            Color color = m_colorGradient.Evaluate((wave % 10) / 10f);
            CreateMarker(wave, y, color, m_newParent.transform);
        }
    }

    private void CreateMarker(int x, float y, Color color, Transform parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(parent);
        sphere.transform.position = new Vector3(x, y, 0);
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material.color = color;
    }
}
