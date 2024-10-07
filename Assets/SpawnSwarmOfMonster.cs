using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

public class SpawnSwarmOfMonster : MonoBehaviour
{
    public GameObject m_graphNodeObj;
    public List<GraphNode> m_graphNodes;

    public GraphNode m_oldGraphNode;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector3 pos = new Vector3();
        foreach (GraphNode node in m_graphNodes)
        {
            for (int i = 0; i < 100; i++)
            {
                pos = new Vector3(i, FlatWithBumps(node.m_baseHealth, i), 0);
                MeshRenderer meshRenderer = Instantiate(m_graphNodeObj, pos, quaternion.identity, transform).GetComponent<MeshRenderer>();
                meshRenderer.material.color = node.m_color;
                node.m_trailRenderer.AddPosition(pos);
            }

            node.m_trailRenderer.gameObject.transform.position = pos;
        }
        
        for (int i = 0; i < 100; i++)
        {
            pos = new Vector3(i, OldCurve(m_oldGraphNode.m_baseHealth, i), 0);
            MeshRenderer meshRenderer = Instantiate(m_graphNodeObj, pos, quaternion.identity, transform).GetComponent<MeshRenderer>();
            meshRenderer.material.color = m_oldGraphNode.m_color;
            m_oldGraphNode.m_trailRenderer.AddPosition(pos);
        }
        m_oldGraphNode.m_trailRenderer.gameObject.transform.position = pos;
        
    }

    private float OldCurve(float baseHealth, float i)
    {
        return baseHealth * Mathf.Pow(1.075f, i);
    }

    [Header("Health Data")]
    public int m_midGameWave = 20;
    public int m_lateGameWave = 40;

    public float m_earlyGameFactor = 0.05f;
    public float m_midGameFactor = 0.05f;
    public float m_lateGameFactor = 0.05f;

    public float m_earlyGameCycleFactor = 0.2f;
    public float m_midGameCycleFactor = 0.1f;
    public float m_lateGameCycleFactor = 0.05f;

    private float FlatWithBumps(float baseHealth, int i)
    {
        Debug.Log($"--- Wave: {i} ---");
        float health = baseHealth;

        float earlyGameHealth;
        float midGameHealth = 0;
        float lateGameHealth = 0;

        // How many health points do we gain from early-game values?
        int earlyWaveNumber = Math.Min(i, m_midGameWave);
        earlyGameHealth = health * (1 + (m_earlyGameFactor * earlyWaveNumber));

        int numberOfEarlyGameCycles = earlyWaveNumber / 10;
        float earlyGameBonusHealth = health * (1 + (numberOfEarlyGameCycles * m_earlyGameCycleFactor)) - health;
        Debug.Log($"Early Base Health: {earlyGameHealth}, Early Bonus Health {earlyGameBonusHealth}, Early Cycles: {numberOfEarlyGameCycles}");

        earlyGameHealth += earlyGameBonusHealth;
        
        // How many health points do we gain from mid-game values?
        int numberOfMidGameCycles = 0;
        float midGameBonusHealth = 0;
        if (i > m_midGameWave)
        {
            int midWaveNumber = Math.Min(i - m_midGameWave, m_lateGameWave - m_midGameWave);
            midGameHealth = health * (1 + (m_midGameFactor * midWaveNumber)) - health;

            numberOfMidGameCycles = midWaveNumber / 10;
            midGameBonusHealth = health * (1 + (numberOfMidGameCycles * m_midGameCycleFactor)) - health;
            Debug.Log($"Mid Base Health: {midGameHealth}, Mid Bonus Health {midGameBonusHealth}, Mid Cycles: {numberOfMidGameCycles}");

            midGameHealth += midGameBonusHealth;
        }

        // How many health points do we gain from late-game values?
        int numberOfLateGameCycles = 0;
        float lateGameBonusHealth = 0;
        if (i > m_lateGameWave)
        {
            int lateWaveNumber = i - m_lateGameWave;
            lateGameHealth = health * (1 + (m_lateGameFactor * lateWaveNumber)) - health;

            numberOfLateGameCycles = lateWaveNumber / 10;
            lateGameBonusHealth = health * (1 + (numberOfLateGameCycles * m_lateGameCycleFactor)) - health;
            Debug.Log($"Late Base Health: {lateGameHealth}, Late Bonus Health {lateGameBonusHealth}, Late Cycles: {numberOfLateGameCycles}");

            lateGameHealth += lateGameBonusHealth;
        }
        
        float cumHealth = (earlyGameHealth + midGameHealth + lateGameHealth);
        
        Debug.Log($"Wave: {i}, Total Health {cumHealth}");

        return cumHealth;
    }
}

[Serializable]
public class GraphNode
{
    public float m_baseHealth;
    public Color m_color;
    public TrailRenderer m_trailRenderer;
}