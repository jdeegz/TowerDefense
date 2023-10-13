using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIStrings", menuName = "ScriptableObjects/StringContainer")]
public class UIStringData : ScriptableObject
{
    [Header("Alert Strings")]
    public string m_cannotAfford;
    public string m_cannotPlace;
}
