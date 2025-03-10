using UnityEngine;

public class CutSceneController : MonoBehaviour
{
    private bool m_isCutSceneComplete;

    void Start()
    {
        if (m_isCutSceneComplete)
        {
            m_isCutSceneComplete = false;
        }

    }

    void Update()
    {
        if (!m_isCutSceneComplete) return;

        if (Input.GetMouseButtonUp(0))
        {
            //We're cut scene complete, and mouse pressed. 
            GameManager.Instance.RequestAdditiveSceneUnload(gameObject.scene.name);
        }
    }

    public void CutSceneCompleted()
    {
        m_isCutSceneComplete = true;
    }
}
