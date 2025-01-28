using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TowersToPlace : MonoBehaviour
{
    public List<Tower> m_towersToPlace;
    public bool m_automaticPlacement = true;

    public ResourceNode m_node;

    private void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState != GameplayManager.GameplayState.PlaceObstacles) return;

        if (m_automaticPlacement == false) return;
        
        foreach (Tower tower in m_towersToPlace)
        {
            if (tower.gameObject.activeSelf == false) continue;
            tower.SetupTower();
        }
    }
    
    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;

        // Disable all the towers if we're going to be placing them manually.
        if (m_automaticPlacement == false)
        {
            foreach (Tower tower in m_towersToPlace)
            {
                tower.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            StartCoroutine(PlaceTowers());
        }
        
        if (Input.GetKeyDown(KeyCode.X))
        {
            SetResources();
        }
    }

    private void SetResources()
    {
        m_node.RequestResource(1);
    }
    
    private void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    IEnumerator PlaceTowers()
    {
        for (int i = 0; i < m_towersToPlace.Count; ++i)
        {
            m_towersToPlace[i].gameObject.SetActive(true);
            m_towersToPlace[i].SetupTower();

            yield return new WaitForSeconds(Random.Range(.33f, .665f));
        }
    }
}