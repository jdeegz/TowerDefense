using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CellObject", menuName = "Cell/CellObject")]
public class CellObject : ScriptableObject
{

    public string cellType;
    public bool isBlocker;

}