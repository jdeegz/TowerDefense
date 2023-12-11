using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIntroductionController : MonoBehaviour
{
    public event Action OnIntroductionComplete;

    public void BossIntroductionCompleted()
    {
        OnIntroductionComplete?.Invoke();
    }
}
