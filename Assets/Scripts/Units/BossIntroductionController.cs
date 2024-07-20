using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIntroductionController : MonoBehaviour //IS THIS USED AT ALL?
{
    public event Action OnIntroductionComplete;

    public void BossIntroductionCompleted()
    {
        Debug.Log($"BossIntroductionController: Introduction Completed.");
        OnIntroductionComplete?.Invoke();
    }
}
