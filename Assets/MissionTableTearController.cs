using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class MissionTableTearController : MonoBehaviour
{
    public GameObject m_missionTableTearObj;
    public List<Transform> m_spawnPositions;
    public Transform m_centerTransform;
    public Transform m_rotationTransform;
    
    private List<GameObject> m_validmissionButtons;
    private int m_unlockedMissionCount;
    private int m_defeatedMissionCount;
    
    private bool m_isSpawning;
    
    private float m_spawnTimeElapsed = 2;
    private float m_spawnRate = 3;

    void Start()
    {
        SetValidMissions();
    }
    
    public void SetValidMissions()
    {
        m_isSpawning = false;
        
        m_unlockedMissionCount = 0;
        m_defeatedMissionCount = 0;
        
        m_spawnTimeElapsed = 0;
        
        List<MissionButtonInteractable> missionButtons = MissionTableController.Instance.MissionButtonList;
        m_validmissionButtons = new List<GameObject>();

        foreach (MissionButtonInteractable missionButton in missionButtons)
        {
            if (missionButton.ButtonDisplayState == MissionButtonInteractable.DisplayState.Locked ||
                missionButton.ButtonDisplayState == MissionButtonInteractable.DisplayState.Unlocked)
            {
                m_validmissionButtons.Add(missionButton.gameObject);
            }
            
            if (missionButton.ButtonDisplayState == MissionButtonInteractable.DisplayState.Unlocked ||
                missionButton.ButtonDisplayState == MissionButtonInteractable.DisplayState.Defeated)
            {
                ++m_unlockedMissionCount;
            }
            
            if (missionButton.ButtonDisplayState == MissionButtonInteractable.DisplayState.Defeated)
            {
                ++m_defeatedMissionCount;
            }
        }
        
        if (m_validmissionButtons.Count == 0) // Player has powered all spires. Gossamer is healed.
        {
            enabled = false;
        }
        else
        {
            float percentMissionsDefeated = (float)m_defeatedMissionCount / missionButtons.Count;
            m_spawnRate = Mathf.Lerp(6f, 12f, percentMissionsDefeated);
            m_isSpawning = true;
        }
    }

    void Update()
    {
        if (!m_isSpawning) return;

        m_spawnTimeElapsed += Time.deltaTime;

        if (m_spawnTimeElapsed > m_spawnRate)
        {
            m_spawnTimeElapsed = 0;
            SpawnTear();
        }
    }

    void SpawnTear()
    {
        //Target:
        GameObject targetObj = Util.GetRandomElement(m_validmissionButtons);
        Transform targetTransform = targetObj.transform;
        
        //Position to spawn:
        Vector3 pos = Util.GetRandomElement(m_spawnPositions).position;
        GameObject obj = ObjectPoolManager.SpawnObject(m_missionTableTearObj, pos, quaternion.identity, transform, ObjectPoolManager.PoolType.GameObject);
        MissionTableTear tear = obj.GetComponent<MissionTableTear>();
        bool b = Random.Range(0, 2) == 0;
        tear.SpawnTear(m_centerTransform, targetTransform, m_rotationTransform, b, m_unlockedMissionCount);
    }
}
