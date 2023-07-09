using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform m_target;
    [SerializeField] private float m_projectileSpeed = 4f;
    [SerializeField] private Rigidbody m_rb;

    private void FixedUpdate()
    {
        if (!m_target) { return; }
        Vector3 direction = (m_target.position - transform.position).normalized;

        m_rb.velocity = direction * m_projectileSpeed;
    }

    public void SetTarget(Transform target)
    {
        m_target = target;
    }

    private void OnCollisionEnter(Collision other)
    {
        other.gameObject.GetComponent<UnitEnemy>().TakeDamage(1);
        Destroy(gameObject);
    }
}
