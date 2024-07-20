using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHealthMeter : MonoBehaviour
{
    [SerializeField] private Track3dObject m_track3dObject;
    [SerializeField] private Image m_lifeImage;
    [SerializeField] private RectTransform m_rootRectTransform;
    [SerializeField] private CanvasGroup m_canvasGroup;
    [SerializeField] private TextMeshProUGUI m_bossNameLabel;

    private float m_maxHealth;
    private float m_curHealth;
    private int m_newHealth;
    private float m_originalWidth;

    //Health needs to be abstracted out so a Castle and Enemy can use this same script.
    private EnemyController m_enemy;

    private void Awake()
    {
        m_originalWidth = m_rootRectTransform.rect.width;
    }

    public void SetEnemy(EnemyController enemy, float health, float yOffset, float xScale)
    {
        m_enemy = enemy;
        m_maxHealth = health;
        m_curHealth = health;
        m_rootRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_originalWidth * xScale);
        m_track3dObject.SetupTracking(m_enemy.gameObject, GetComponent<RectTransform>(), yOffset);
        m_enemy.UpdateHealth += OnUpdateHealth;
        m_enemy.DestroyEnemy += OnEnemyDestroyed;
    }

    public void SetBoss(EnemyController enemy, float health)
    {
        m_enemy = enemy;
        m_maxHealth = health;
        m_curHealth = health;
        m_bossNameLabel.SetText(m_enemy.m_enemyData.m_enemyName);
        m_enemy.UpdateHealth += OnUpdateHealth;
        m_enemy.DestroyEnemy += OnEnemyDestroyed;
        HandleFadeIn();
    }

    void HandleFadeIn()
    {
        m_canvasGroup.DOFade(1, 1f);
    }

    void OnUpdateHealth(float i)
    {
        m_curHealth += i;
    }

    void Update()
    {
        m_lifeImage.fillAmount = Mathf.Lerp(m_lifeImage.fillAmount, m_curHealth / m_maxHealth, 10 * Time.deltaTime);
    }

    void OnEnemyDestroyed(Vector3 pos)
    {
        m_enemy.UpdateHealth -= OnUpdateHealth;
        m_enemy.DestroyEnemy -= OnEnemyDestroyed;
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}