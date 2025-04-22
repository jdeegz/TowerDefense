using TMPro;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private TextMeshProUGUI m_fpsLabel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_fpsLabel = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        float fps = 1f / Time.deltaTime;
        m_fpsLabel.SetText("FPS: " + Mathf.Round(fps));
    }
}
