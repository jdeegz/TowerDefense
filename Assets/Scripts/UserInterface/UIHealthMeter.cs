using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthMeter : MonoBehaviour
{
    [SerializeField] private Track3dObject m_track3dObject;
    [SerializeField] private Image m_lifeImage;

    private int m_maxHealth;
    private int m_curHealth;
    private int m_newHealth;

    //Health needs to be abstracted out so a Castle and Enemy can use this same script.
    private UnitEnemy m_enemy;

    public void SetEnemy(UnitEnemy enemy)
    {
        m_enemy = enemy;
        m_maxHealth = m_enemy.m_maxHealth;
        m_curHealth = m_enemy.m_maxHealth;
        m_track3dObject.SetupTracking(m_enemy.gameObject, GetComponent<RectTransform>());
        m_enemy.UpdateHealth += OnUpdateHealth;
        m_enemy.DestroyEnemy += OnEnemyDestroyed;
    }

    void OnUpdateHealth(int i)
    {
        m_curHealth += i;
    }

    void Update()
    {
        m_lifeImage.fillAmount = Mathf.Lerp(m_lifeImage.fillAmount, (float)m_curHealth / m_maxHealth, 10 * Time.deltaTime);
    }

    void OnEnemyDestroyed()
    {
        Destroy(gameObject);
    }
}