using System.Collections.Generic;
using UnityEngine;

public class TreeProp : MonoBehaviour
{
    [SerializeField] private List<GameObject> m_objectsToToggle;

    public void ToggleObjects(bool value)
    {
        Debug.Log($"toggle object value: {value}.");
        foreach (GameObject obj in m_objectsToToggle)
        {
            Debug.Log($"Obj state is :{obj.activeSelf}, setting to {!value}.");
            obj.SetActive(!value);
        }
    }
}
