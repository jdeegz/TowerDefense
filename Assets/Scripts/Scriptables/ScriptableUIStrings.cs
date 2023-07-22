using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIStrings", menuName = "UI/StringContainer")]
public class ScriptableUIStrings : ScriptableObject
{
    [Header("Alert Strings")]
    public string m_cannotAfford;
    public string m_cannotPlace;
}
