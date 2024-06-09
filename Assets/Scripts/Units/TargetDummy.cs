using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetDummy : EnemyController
{
    // Start is called before the first frame update
    void Start()
    {
        SetEnemyData(m_enemyData);
    }
    
    public override void HandleMovement()
    {
        
    }
}
