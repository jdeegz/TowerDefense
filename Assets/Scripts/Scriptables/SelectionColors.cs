using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SelectionVariables", menuName = "ScriptableObjects/SelectionVariables")]
public class SelectionColors : ScriptableObject
{
    [Header("Object Selection Outline")]
    public Color m_outlineBaseColor;
    public Color m_outlineRestrictedColor;
    public float m_outlineWidth;

    [Header("Unit Path Colors")]
    public Color m_unitPathColorOn;
    public Color m_unitPathColorOff;
    
}
