using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class SprinterCutscene : MonoBehaviour
{
    public Transform m_spawnTransform;
    public GameObject m_sprinterPrefab;
    public float m_spawnInterval = 2f;
    public float m_spawnDelay;

    private float m_timeElapsed;
    [SerializeField] private List<SprinterCreated> m_sprinters;
    private List<SprinterCreated> m_sprintersToRemove;

    void Start()
    {
        m_timeElapsed = 0;
        m_timeElapsed -= m_spawnDelay;
        m_sprinters = new List<SprinterCreated>();
        m_sprintersToRemove = new List<SprinterCreated>();
    }

    // Update is called once per frame
    void Update()
    {
        m_timeElapsed += Time.unscaledDeltaTime;

        if (m_timeElapsed > m_spawnInterval)
        {
            SprinterCreated sprinter = new SprinterCreated();
            sprinter.m_sprinter = Instantiate(m_sprinterPrefab, Vector3.zero, m_spawnTransform.rotation, m_spawnTransform);
            sprinter.Setup();
            m_sprinters.Add(sprinter);

            m_timeElapsed = 0;
        }

        List<SprinterCreated> tempSprinters = new List<SprinterCreated>(m_sprinters);
        foreach (SprinterCreated oldSprinter in m_sprintersToRemove)
        {
            tempSprinters.Remove(oldSprinter);
        }

        m_sprintersToRemove = new List<SprinterCreated>();
        m_sprinters = new List<SprinterCreated>(tempSprinters);
        
        foreach (SprinterCreated sprinter in m_sprinters)
        {
            sprinter.m_timeElapsed += Time.unscaledDeltaTime;

            if (sprinter.m_sprinter.activeSelf && sprinter.m_timeElapsed < sprinter.m_lifeTime) //if we're alive, translate forward.
            {
                float speed = 12 * Time.unscaledDeltaTime;
                sprinter.m_sprinter.transform.localPosition += Vector3.forward * speed;
            }

            if (sprinter.m_sprinter.activeSelf == false && m_timeElapsed > sprinter.m_delay)
            {
                sprinter.m_sprinter.SetActive(true);
                sprinter.m_sprinter.transform.position = m_spawnTransform.position;
                m_timeElapsed = 0;
            }

            if (sprinter.m_timeElapsed > sprinter.m_lifeTime)
            {
                Destroy(sprinter.m_sprinter);
                m_sprintersToRemove.Add(sprinter);
            }
        }
    }
}
[System.Serializable]
public class SprinterCreated
{
    public GameObject m_sprinter;
    public float m_delay = 0.1f;
    public float m_timeElapsed;
    public float m_lifeTime = 20f;

    public void Setup()
    {
        m_sprinter.SetActive(false);
        m_timeElapsed = 0;
    }
}