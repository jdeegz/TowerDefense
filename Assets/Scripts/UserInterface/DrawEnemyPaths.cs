using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class DrawEnemyPaths : MonoBehaviour
{
    
    public LineRenderer m_lineRenderer;
    private Vector2Int m_startVector2Int;
    private Vector2Int m_endVector2Int;
    private Vector3 m_startPos;
    private Vector3 m_endPos;
    
    void Start()
    {
        /*startPoint = GameplayManager.Instance.m_unitSpawners[0].transform;
        endPoint = GameplayManager.Instance.m_enemyGoal;*/
    }

    void UpdateLineRenderer()
    {
        
        m_startPos = GameplayManager.Instance.m_unitSpawners[0].GetSpawnPointTransform().position;
        m_endPos = GameplayManager.Instance.m_enemyGoal.position;
        NavMeshPath path = new NavMeshPath();
        
        //Using the Navmesh instead of the grid & astar.
        if (NavMesh.CalculatePath(m_startPos, m_endPos, NavMesh.AllAreas, path))
        {
            m_lineRenderer.positionCount = path.corners.Length;
            
            //Convert corner positions to grid positions.
            //m_lineRenderer.SetPositions(path.corners);
            for (int i = 0; i < path.corners.Length; ++i)
            {
                var pos = path.corners[i];
                Vector3 cellPos = Util.RoundVectorToInt(pos);
                m_lineRenderer.SetPosition(i, cellPos);
            }
        }
    }
    
    private void GameplayManagerStateChanged(GameplayManager.GameplayState state)
    {
        if (state != GameplayManager.GameplayState.Setup)
        {
            UpdateLineRenderer();
        }
    }
    
    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }
}
