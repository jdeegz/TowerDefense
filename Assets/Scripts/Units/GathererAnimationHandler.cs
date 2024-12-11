using UnityEngine;

public class GathererAnimationHandler : MonoBehaviour
{
    private GathererController controller;

    void Awake()
    {
        controller = GetComponentInParent<GathererController>();
    }
    
    public void Contact()
    {
        // Find the GathererController script on the parent GameObject
       
        if (controller != null)
        {
            controller.Contact();
        }
        else
        {
            Debug.LogWarning("No GathererController found on parent!");
        }
    }
}
