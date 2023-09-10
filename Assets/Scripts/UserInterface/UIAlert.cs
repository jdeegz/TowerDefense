using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIAlert : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_label;

    [SerializeField] private float m_lifeTime;

    private float m_age;
    
    // Start is called before the first frame update
    void Start()
    {
            
    }

    // Update is called once per frame
    void Update()
    {
        m_age += Time.deltaTime;
        if (m_age >= m_lifeTime)
        {
            Destroy(gameObject);
        }
    }

    public void SetLabelText(string text, Color color)
    {
        m_label.SetText(text);
        m_label.color = color;
    }
}
