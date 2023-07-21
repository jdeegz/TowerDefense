using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SelectionVariables", menuName = "Global/SelectionVariables")]
public class SelectionColors : ScriptableObject
{
    
    public Color m_outlineBaseColor;
    public Color m_outlineRestrictedColor;
    public float m_outlineWidth;
    
}
