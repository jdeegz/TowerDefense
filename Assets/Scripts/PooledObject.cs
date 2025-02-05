using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public abstract class PooledObject : MonoBehaviour
{
    [Header("Auto-Managed Components (Optional)")]
    [SerializeField] protected List<Renderer> m_renderers = new();
    [SerializeField] protected List<Rigidbody> m_rigidBodies = new();
    [SerializeField] protected List<Collider> m_colliders = new();
    [SerializeField] protected List<VisualEffect> m_vfx = new();

    // Called when object is retrieved from the pool
    public virtual void OnSpawn()
    {
        // Enable Renderers
        foreach (var renderer in m_renderers)
            renderer.enabled = true;

        // Enable Colliders
        foreach (var col in m_colliders)
            col.enabled = true;

        // Reset and Enable Rigidbodies
        foreach (var rb in m_rigidBodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false; // Enable physics if necessary
        }

        // Play Visual Effects
        foreach (var vfx in m_vfx)
        {
            vfx.Stop();
            vfx.Play();
        }
    }

    // Called when object is returned to the pool
    public virtual void OnDespawn()
    {
        // Disable Renderers
        foreach (var renderer in m_renderers)
            renderer.enabled = false;

        // Disable Colliders
        foreach (var col in m_colliders)
            col.enabled = false;

        // Stop and Reset Rigidbodies
        foreach (var rb in m_rigidBodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Prevent unintended movement
        }

        // Stop Visual Effects
        foreach (var vfx in m_vfx)
        {
            vfx.Stop();
        }
    }
}