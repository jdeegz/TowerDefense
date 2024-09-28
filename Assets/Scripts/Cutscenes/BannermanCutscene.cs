using UnityEngine;

public class BannermanCutscene : MonoBehaviour
{
    public GameObject m_healEffect;
    public Transform m_healTransform;
    public float m_healPeriod = 2f;
    private float m_nextHealTime;
    private float m_timeElapsed;

    private void Start()
    {
        m_nextHealTime = 3f;
    }
    
    private void Update()
    {
        if (m_nextHealTime <= m_timeElapsed)
        {
            m_nextHealTime += m_healPeriod;
            Heal();
        }

        m_timeElapsed += Time.unscaledDeltaTime;
    }

    private void Heal()
    {
        Instantiate(m_healEffect.gameObject, m_healTransform.position, Quaternion.identity);
    }
}
