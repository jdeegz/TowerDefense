using System.Collections.Generic;
using UnityEngine;

public class VanguardShieldManager : MonoBehaviour
{
    public List<GameObject> m_vanguardShields;

    private int m_activeShieldCount;
    private int m_maxShieldCount = 99;

    void OnEnable()
    {
        if (m_vanguardShields.Count <= 1) return; //Dont need to run if we only have 1 shield.

        foreach (GameObject obj in m_vanguardShields)
        {
            obj.SetActive(false);
        }

        //Determine how many shields we want to keep active. Minimum of 1 shield.
        if (GameplayManager.Instance)
        {
            m_maxShieldCount = GameplayManager.Instance.m_wave / 15; //Every N waves, increase the maximum shield count by 1.
        }

        m_activeShieldCount = Random.Range(1, Mathf.Min(m_vanguardShields.Count, m_maxShieldCount) + 1);

        //Build a list of shields we want to have active for this unit.
        List<GameObject> shieldList = new List<GameObject>(m_vanguardShields);

        List<GameObject> shieldsToActivate = new List<GameObject>();
        for (int i = 0;
             i < m_activeShieldCount;
             i++)
        {
            int index = Random.Range(0, shieldList.Count);
            GameObject shieldToActivate = shieldList[index];

            shieldsToActivate.Add(shieldToActivate); //At to list to operate on.
            shieldList.RemoveAt(index); //Remove from list to pull next shield from.
        }

        foreach (GameObject obj in shieldsToActivate)
        {
            obj.SetActive(true);
        }
    }
}