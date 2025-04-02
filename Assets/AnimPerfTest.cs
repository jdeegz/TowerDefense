using TMPro;
using UnityEngine;

public class AnimPerfTest : MonoBehaviour
{
    public GameObject towerPrefab;
    public int towerCount = 100;
    public Vector3 gridSize = new Vector3(10, 0, 10);

    void Start()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int z = 0; z < gridSize.z; z++)
            {
                Vector3 position = new Vector3(x * 2, 0, z * 2);
                Instantiate(towerPrefab, position, Quaternion.identity);
            }
        }
    }
    
    public TextMeshProUGUI fpsText;
    private float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = $"FPS: {Mathf.Ceil(fps)}";
    }
}
