using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Manages the lifecycle of a GameObject with an attached VisualEffect component,
/// returning it to an object pool when the effect is no longer active.
/// </summary>
[RequireComponent(typeof(VisualEffect))]
public class VFXPoolHandler : MonoBehaviour
{
    // Reference to the VisualEffect component on this GameObject.
    private VisualEffect visualEffect;

    // Flag to track whether the visual effect has been played.
    private bool hasPlayed;

    /// <summary>
    /// Initialize the script by obtaining the VisualEffect component.
    /// </summary>
    void Awake()
    {
        // Get the attached VisualEffect component.
        visualEffect = GetComponent<VisualEffect>();
    }

    /// <summary>
    /// Check each frame to see if the visual effect has stopped playing, and if so, return it to the pool.
    /// </summary>
    void Update()
    {
        // Check if there are no alive particles and the effect has previously played.
        if (visualEffect.aliveParticleCount == 0 && hasPlayed)
        {
            // Return the GameObject to the object pool.
            ReturnToPool();

            // Reset the hasPlayed flag to prepare for the next time the effect is played.
            hasPlayed = false;
            return;
        }
       
        // If there are alive particles, mark the effect as having been played.
        if (visualEffect.aliveParticleCount > 0)
        {
            hasPlayed = true;
        }
    }

    /// <summary>
    /// Deactivates the GameObject and triggers the return to object pool process.
    /// </summary>
    public void ReturnToPool()
    {
        // Call to the ObjectPoolManager to handle the actual return-to-pool logic.
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}