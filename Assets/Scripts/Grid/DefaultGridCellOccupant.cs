using UnityEngine;

public class DefaultGridCellOccupant : MonoBehaviour
{
    void Awake()
    {
        GameplayManager.OnGameplayStateChanged += GameplayManagerStateChanged;
    }

    void OnDestroy()
    {
        GameplayManager.OnGameplayStateChanged -= GameplayManagerStateChanged;
    }

    void GameplayManagerStateChanged(GameplayManager.GameplayState newState)
    {
        if (newState == GameplayManager.GameplayState.PlaceObstacles)
        {
            GridCellOccupantUtil.SetOccupant(gameObject, true, 1, 1);
            /*Cell cell = Util.GetCellFrom3DPos(transform.position);
            GridManager.Instance.ToggleTileMap(new Vector3Int(cell.m_cellPos.x, cell.m_cellPos.y, 0), true);*/
        }
    }
}
