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
        }
    }
}
