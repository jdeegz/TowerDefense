using System.Collections.Generic;
using UnityEngine;

public class TreeProp : MonoBehaviour
{
    [SerializeField] private List<GameObject> m_objectsToToggle;

    public void ToggleObjects(bool value)
    {
        foreach (GameObject obj in m_objectsToToggle)
        {
            obj.SetActive(!value);
        }
    }
}
