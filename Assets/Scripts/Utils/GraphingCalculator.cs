using Unity.Mathematics;
using UnityEngine;

public class GraphingCalculator : MonoBehaviour
{
    public GameObject m_obj;
    public float m_base;
    public float m_damagePower;
    public float m_speedPower;
    public float m_steps;
    public float m_damageCap;
    public float m_speedCap;

    // Start is called before the first frame update
    void Start()
    {
        for (int x = 0; x < m_steps; ++x)
        {
            float yPos = m_base * Mathf.Pow(m_damagePower, x);
            if (yPos > m_damageCap) yPos = m_damageCap;

            float attackSpeed = 1 * Mathf.Pow(m_speedPower, x);
            if (attackSpeed > m_speedCap) attackSpeed = m_speedCap;

            Vector3 pos = new Vector3(x * attackSpeed, yPos, 0);
            GameObject obj = Instantiate(m_obj, pos, quaternion.identity);
            obj.name = x.ToString();
            Debug.Log($"x:{x} speed:{attackSpeed} damage:{yPos}");
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}