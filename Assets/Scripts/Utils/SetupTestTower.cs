using UnityEngine;

public class SetupTestTower : MonoBehaviour
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
            Tower[] towers = transform.GetComponentsInChildren<Tower>();

            foreach (Tower tower in towers)
            {
                tower.SetupTower();
            }
        }
    }
}
