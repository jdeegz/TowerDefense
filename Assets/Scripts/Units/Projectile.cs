using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform m_target;
    [SerializeField] private float m_projectileSpeed = 4f;
    [SerializeField] private float m_projectileDamage = 1;
    [SerializeField] private Rigidbody m_rb;
    [SerializeField] private AudioClip m_audioImpactSound;

    private void FixedUpdate()
    {
        if (!m_target)
        {
            Destroy(gameObject);
            return;
        }
        Vector3 direction = (m_target.position - transform.position).normalized;

        m_rb.velocity = direction * m_projectileSpeed;
    }

    public void SetTarget(Transform target)
    {
        m_target = target;
    }

    private void OnCollisionEnter(Collision other)
    {
        other.gameObject.GetComponent<UnitEnemy>().OnTakeDamage(m_projectileDamage);
        Destroy(gameObject);
    }
}
