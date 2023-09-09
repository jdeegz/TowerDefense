using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Transform m_target;
    [SerializeField] private float m_projectileSpeed = 4f;
    [SerializeField] private int m_projectileDamage = 1;
    [SerializeField] private Rigidbody m_rb;

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
        //Debug.Log(gameObject.name + " has hit : " + other.gameObject.name);
        other.gameObject.GetComponent<UnitEnemy>().TakeDamage(m_projectileDamage);
        Destroy(gameObject);
    }
}
